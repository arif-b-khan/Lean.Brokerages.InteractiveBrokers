using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.IBAutomater;

namespace QuantConnect.InteractiveBrokers.ToolBox;

/// <summary>
/// Helper for managing Interactive Brokers Gateway/TWS using IBAutomater
/// </summary>
public class IBAutomaterHelper
{
    private static readonly string[] CiEnvironmentVariables =
    {
        "CI", "CONTINUOUS_INTEGRATION", "BUILD_NUMBER", "BUILD_ID",
        "GITHUB_ACTIONS", "GITLAB_CI", "JENKINS_URL", "TEAMCITY_VERSION",
        "TF_BUILD", "APPVEYOR", "CIRCLECI", "TRAVIS", "DRONE"
    };

    private readonly ILogger _logger;
    private readonly Dictionary<string, string> _config;
    private readonly Func<AutomaterSettings, IIBAutomaterController> _automaterFactory;
    private readonly Func<string, int, CancellationToken, Task<bool>> _gatewayStatusProbe;
    private readonly Func<string, int, CancellationToken, Task> _gatewayWaiter;

    private readonly object _sync = new();
    private IIBAutomaterController? _automater;
    private bool _startedGateway;

    public IBAutomaterHelper(
        Dictionary<string, string> config,
        ILogger logger,
        Func<AutomaterSettings, IIBAutomaterController>? automaterFactory = null,
        Func<string, int, CancellationToken, Task<bool>>? gatewayStatusProbe = null,
        Func<string, int, CancellationToken, Task>? gatewayWaiter = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _automaterFactory = automaterFactory ?? (settings => new QuantConnectIBAutomaterController(settings));
        _gatewayStatusProbe = gatewayStatusProbe ?? IsGatewayListeningAsync;
        _gatewayWaiter = gatewayWaiter ?? WaitForGatewayReadyAsync;
    }

    /// <summary>
    /// Check if IBAutomater should be used (disabled in CI environments)
    /// </summary>
    public bool ShouldUseIBAutomater(bool requestedByUser)
    {
        if (!requestedByUser)
        {
            return false;
        }

        foreach (var envVar in CiEnvironmentVariables)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
            {
                _logger.LogWarning($"Detected CI environment ({envVar}), disabling IBAutomater integration");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Start IB Gateway using IBAutomater if requested and available
    /// </summary>
    public async Task<bool> StartGatewayIfNeeded(DownloadRequest request, CancellationToken cancellationToken)
    {
        if (!request.UseIbAutomater)
        {
            _logger.LogDebug("IBAutomater not requested");
            return false;
        }

        if (!ShouldUseIBAutomater(true))
        {
            _logger.LogDebug("IBAutomater disabled by environment policy");
            return false;
        }

        if (!IsLocalHost(request.GatewayHost))
        {
            _logger.LogWarning($"IBAutomater can only manage local gateways. Host '{request.GatewayHost}' is not local.");
            return false;
        }

        if (await _gatewayStatusProbe(request.GatewayHost, request.GatewayPort, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInfo($"Detected IB Gateway already listening on {request.GatewayHost}:{request.GatewayPort}. Skipping IBAutomater start.");
            return false;
        }

        var ibConfig = GetIBAutomaterConfig();
        var settings = BuildAutomaterSettings(ibConfig, request);

        _logger.LogInfo($"Starting IB Gateway via IBAutomater (mode: {settings.TradingMode}, version: {settings.IbGatewayVersion})");

        var automater = _automaterFactory(settings);
        AttachEventHandlers(automater);

        try
        {
            var startResult = automater.Start(waitForExit: false);

            if (startResult.HasError)
            {
                var message = BuildStartErrorMessage(startResult);
                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }

            _logger.LogInfo("Waiting for IB Gateway to accept connections...");
            await _gatewayWaiter(request.GatewayHost, settings.Port, cancellationToken).ConfigureAwait(false);

            lock (_sync)
            {
                _automater = automater;
                _startedGateway = true;
            }

            _logger.LogInfo("IB Gateway is ready for connections.");
            return true;
        }
        catch (Exception ex)
        {
            CleanupAutomater(automater);

            if (ex is InvalidOperationException)
            {
                throw;
            }

            _logger.LogError("Failed to start IB Gateway via IBAutomater", exception: ex);
            throw new InvalidOperationException("Failed to launch IB Gateway via IBAutomater. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Stop IB Gateway that was started by IBAutomater
    /// </summary>
    public Task StopGatewayIfStarted(CancellationToken cancellationToken = default)
    {
        IIBAutomaterController? automater;
        bool startedByHelper;

        lock (_sync)
        {
            automater = _automater;
            startedByHelper = _startedGateway;
            _automater = null;
            _startedGateway = false;
        }

        if (automater == null)
        {
            _logger.LogDebug("No IBAutomater instance to stop");
            return Task.CompletedTask;
        }

        try
        {
            if (startedByHelper)
            {
                _logger.LogInfo("Stopping IB Gateway via IBAutomater...");
                automater.Stop();
            }
            else
            {
                _logger.LogDebug("IB Gateway was not started by this helper; skipping stop call");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error while stopping IB Gateway via IBAutomater: {ex.Message}");
        }
        finally
        {
            CleanupAutomater(automater);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Get IBAutomater configuration from config dictionary
    /// </summary>
    private Dictionary<string, string> GetIBAutomaterConfig()
    {
        var ibConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in _config)
        {
            if (kvp.Key.StartsWith("IB_", StringComparison.OrdinalIgnoreCase))
            {
                ibConfig[kvp.Key] = kvp.Value;
            }
        }

        MergeFromEnvironmentIfMissing(ibConfig, "IB_GATEWAY_DIR");
        MergeFromEnvironmentIfMissing(ibConfig, "IB_VERSION");
        MergeFromEnvironmentIfMissing(ibConfig, "IB_TRADING_MODE");
        MergeFromEnvironmentIfMissing(ibConfig, "IB_AUTOMATER_EXPORT_LOGS");

        return ibConfig;
    }

    private AutomaterSettings BuildAutomaterSettings(Dictionary<string, string> ibConfig, DownloadRequest request)
    {
        var gatewayDir = ResolveIbGatewayDirectory(ibConfig);
        if (!Directory.Exists(gatewayDir))
        {
            throw new InvalidOperationException($"IB Gateway directory not found: '{gatewayDir}'. Set IB_GATEWAY_DIR to your IB Gateway installation path.");
        }

        var userName = GetRequiredValue(ibConfig, "IB_USERNAME");
        var password = GetRequiredValue(ibConfig, "IB_PASSWORD");
        var version = ibConfig.TryGetValue("IB_VERSION", out var configuredVersion) && !string.IsNullOrWhiteSpace(configuredVersion)
            ? configuredVersion
            : "latest";

        var tradingMode = ibConfig.TryGetValue("IB_TRADING_MODE", out var configuredMode) && !string.IsNullOrWhiteSpace(configuredMode)
            ? configuredMode.Trim().ToLowerInvariant()
            : "paper";

        if (tradingMode is not ("paper" or "live"))
        {
            _logger.LogWarning($"Unrecognized IB_TRADING_MODE '{tradingMode}'. Defaulting to 'paper'.");
            tradingMode = "paper";
        }

        var exportLogs = ibConfig.TryGetValue("IB_AUTOMATER_EXPORT_LOGS", out var exportValue) && bool.TryParse(exportValue, out var export)
            ? export
            : false;

        return new AutomaterSettings(
            ExpandPath(gatewayDir),
            version,
            userName,
            password,
            tradingMode,
            request.GatewayPort,
            exportLogs
        );
    }

    private static string GetRequiredValue(Dictionary<string, string> config, string key)
    {
        if (config.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Missing required configuration value '{key}'.");
    }

    private static void MergeFromEnvironmentIfMissing(Dictionary<string, string> config, string key)
    {
        if (!config.ContainsKey(key))
        {
            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envValue))
            {
                config[key] = envValue;
            }
        }
    }

    private static string ResolveIbGatewayDirectory(Dictionary<string, string> config)
    {
        if (config.TryGetValue("IB_GATEWAY_DIR", out var configuredDir) && !string.IsNullOrWhiteSpace(configuredDir))
        {
            return ExpandPath(configuredDir);
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return @"C:\\Jts";
        }

        return Path.Combine(home, "Jts");
    }

    private static string ExpandPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return path.StartsWith("~")
            ? Path.Combine(home, path.TrimStart('~', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            : path;
    }

    private static bool IsLocalHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        host = host.Trim();

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("::1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            var addresses = Dns.GetHostAddresses(host);
            return addresses.Any(IPAddress.IsLoopback);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsGatewayListeningAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(3));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, linkedCts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static async Task WaitForGatewayReadyAsync(string host, int port, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMinutes(2);
        var pollInterval = TimeSpan.FromSeconds(2);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await IsGatewayListeningAsync(host, port, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Timed out waiting for IB Gateway to accept connections on {host}:{port}.");
    }

    private static string BuildStartErrorMessage(StartResult result)
    {
        var baseMessage = $"IBAutomater failed to start IB Gateway ({result.ErrorCode}). {result.ErrorMessage}".TrimEnd();

        return result.ErrorCode switch
        {
            ErrorCode.ProcessStartFailed => baseMessage + " Ensure IB_GATEWAY_DIR points to a valid IB Gateway installation (run the installer if needed).",
            ErrorCode.IbGatewayVersionNotInstalled => baseMessage + " Install the requested IB Gateway version or set IB_VERSION to an installed build.",
            ErrorCode.JavaNotFound => baseMessage + " Install a compatible Java Runtime Environment and ensure it is on the PATH.",
            ErrorCode.LoginFailed or ErrorCode.LoginFailedAccountTasksRequired => baseMessage + " Verify your IB credentials and complete any pending compliance tasks in Client Portal.",
            ErrorCode.ExistingSessionDetected => baseMessage + " Another IB Gateway/TWS session is active. Log out of other sessions or disable --use-ib-automater.",
            ErrorCode.SecurityDialogDetected => baseMessage + " Dismiss any outstanding security dialogs in IB Gateway.",
            ErrorCode.TwoFactorConfirmationTimeout => baseMessage + " Approve the 2FA prompt in Client Portal within the allowed time window.",
            ErrorCode.InitializationTimeout => baseMessage + " IB Gateway did not become ready. Check gateway logs for details.",
            _ => baseMessage
        };
    }

    private void AttachEventHandlers(IIBAutomaterController automater)
    {
        automater.OutputDataReceived += OnOutputDataReceived;
        automater.ErrorDataReceived += OnErrorDataReceived;
        automater.Exited += OnAutomaterExited;
        automater.Restarted += OnAutomaterRestarted;
    }

    private void DetachEventHandlers(IIBAutomaterController automater)
    {
        automater.OutputDataReceived -= OnOutputDataReceived;
        automater.ErrorDataReceived -= OnErrorDataReceived;
        automater.Exited -= OnAutomaterExited;
        automater.Restarted -= OnAutomaterRestarted;
    }

    private void CleanupAutomater(IIBAutomaterController automater)
    {
        try
        {
            DetachEventHandlers(automater);
        }
        catch
        {
            // ignore cleanup errors
        }

        automater.Dispose();
    }

    private void OnOutputDataReceived(object? sender, OutputDataReceivedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            _logger.LogDebug($"IBAutomater: {e.Data}");
        }
    }

    private void OnErrorDataReceived(object? sender, ErrorDataReceivedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            _logger.LogWarning($"IBAutomater error: {e.Data}");
        }
    }

    private void OnAutomaterExited(object? sender, ExitedEventArgs e)
    {
        _logger.LogInfo($"IBAutomater exited with code {e.ExitCode}");
    }

    private void OnAutomaterRestarted(object? sender, EventArgs e)
    {
        _logger.LogInfo("IBAutomater triggered an automatic restart");
    }

    public record AutomaterSettings(
        string IbGatewayDirectory,
        string IbGatewayVersion,
        string UserName,
        string Password,
        string TradingMode,
        int Port,
        bool ExportGatewayLogs);

    public interface IIBAutomaterController : IDisposable
    {
        event EventHandler<OutputDataReceivedEventArgs> OutputDataReceived;
        event EventHandler<ErrorDataReceivedEventArgs> ErrorDataReceived;
        event EventHandler<ExitedEventArgs> Exited;
        event EventHandler? Restarted;

        StartResult Start(bool waitForExit);
        void Stop();
        bool IsRunning();
    }

    private sealed class QuantConnectIBAutomaterController : IIBAutomaterController
    {
        private readonly QuantConnect.IBAutomater.IBAutomater _inner;

        public QuantConnectIBAutomaterController(AutomaterSettings settings)
        {
            _inner = new QuantConnect.IBAutomater.IBAutomater(
                settings.IbGatewayDirectory,
                settings.IbGatewayVersion,
                settings.UserName,
                settings.Password,
                settings.TradingMode,
                settings.Port,
                settings.ExportGatewayLogs);
        }

        public event EventHandler<OutputDataReceivedEventArgs> OutputDataReceived
        {
            add => _inner.OutputDataReceived += value;
            remove => _inner.OutputDataReceived -= value;
        }

        public event EventHandler<ErrorDataReceivedEventArgs> ErrorDataReceived
        {
            add => _inner.ErrorDataReceived += value;
            remove => _inner.ErrorDataReceived -= value;
        }

        public event EventHandler<ExitedEventArgs> Exited
        {
            add => _inner.Exited += value;
            remove => _inner.Exited -= value;
        }

        public event EventHandler? Restarted
        {
            add => _inner.Restarted += value;
            remove => _inner.Restarted -= value;
        }

        public StartResult Start(bool waitForExit) => _inner.Start(waitForExit);

        public void Stop() => _inner.Stop();

        public bool IsRunning() => _inner.IsRunning();

        public void Dispose() => _inner.Dispose();
    }
}