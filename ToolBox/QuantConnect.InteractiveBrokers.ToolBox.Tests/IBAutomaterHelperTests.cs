using FluentAssertions;
using QuantConnect.IBAutomater;
using QuantConnect.InteractiveBrokers.ToolBox;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

public class IBAutomaterHelperTests
{
    private readonly Dictionary<string, string> _baseConfig;

    public IBAutomaterHelperTests()
    {
        _baseConfig = new Dictionary<string, string>
        {
            ["IB_USERNAME"] = "user",
            ["IB_PASSWORD"] = "pass",
            ["IB_ACCOUNT"] = "U1234567",
            ["IB_GATEWAY_DIR"] = Directory.GetCurrentDirectory()
        };
    }

    [Fact]
    public async Task StartGatewayIfNeeded_ReturnsFalse_WhenFlagDisabled()
    {
        var logger = new TestLogger();
        var helper = CreateHelper(logger, automaterFactory: _ => throw new InvalidOperationException("Factory should not be invoked"));

        var result = await helper.StartGatewayIfNeeded(CreateRequest(useAutomater: false), CancellationToken.None);

        result.Should().BeFalse();
        logger.Messages.Should().Contain(m => m.StartsWith("DEBUG") && m.Contains("not requested"));
    }

    [Fact]
    public async Task StartGatewayIfNeeded_ReturnsFalse_InCiEnvironment()
    {
        var logger = new TestLogger();
        Environment.SetEnvironmentVariable("CI", "true");

        try
        {
            var helper = CreateHelper(logger, automaterFactory: _ => throw new InvalidOperationException("Factory should not be invoked"));
            var result = await helper.StartGatewayIfNeeded(CreateRequest(), CancellationToken.None);

            result.Should().BeFalse();
            logger.Messages.Should().Contain(m => m.StartsWith("WARN") && m.Contains("CI environment"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CI", null);
        }
    }

    [Fact]
    public async Task StartGatewayIfNeeded_ReturnsFalse_WhenGatewayAlreadyListening()
    {
        var logger = new TestLogger();
        var helper = CreateHelper(
            logger,
            automaterFactory: _ => throw new InvalidOperationException("Factory should not be invoked"),
            gatewayStatusProbe: (_, _, _) => Task.FromResult(true));

        var result = await helper.StartGatewayIfNeeded(CreateRequest(), CancellationToken.None);

        result.Should().BeFalse();
        logger.Messages.Should().Contain(m => m.StartsWith("INFO") && m.Contains("already listening"));
    }

    [Fact]
    public async Task StartGatewayIfNeeded_ReturnsFalse_ForRemoteHost()
    {
        var logger = new TestLogger();
        var helper = CreateHelper(logger, automaterFactory: _ => throw new InvalidOperationException("Factory should not be invoked"));

        var result = await helper.StartGatewayIfNeeded(CreateRequest(host: "192.168.0.10"), CancellationToken.None);

        result.Should().BeFalse();
        logger.Messages.Should().Contain(m => m.StartsWith("WARN") && m.Contains("not local"));
    }

    [Fact]
    public async Task StartGatewayIfNeeded_StartsGateway_WhenAvailable()
    {
        var logger = new TestLogger();
        var automater = new FakeAutomater();
        var helper = CreateHelper(
            logger,
            automaterFactory: _ => automater,
            gatewayStatusProbe: (_, _, _) => Task.FromResult(false),
            gatewayWaiter: (_, _, _) => Task.CompletedTask);

        var result = await helper.StartGatewayIfNeeded(CreateRequest(), CancellationToken.None);

        result.Should().BeTrue();
        automater.StartCalled.Should().BeTrue();
        automater.StopCalled.Should().BeFalse();
        automater.DisposeCalled.Should().BeFalse();

        await helper.StopGatewayIfStarted();

        automater.StopCalled.Should().BeTrue();
        automater.DisposeCalled.Should().BeTrue();
    }

    [Fact]
    public async Task StartGatewayIfNeeded_Throws_WhenAutomaterReportsError()
    {
        var logger = new TestLogger();
        var automater = new FakeAutomater
        {
            StartResultToReturn = new StartResult(ErrorCode.LoginFailed, "bad credentials")
        };

        var helper = CreateHelper(
            logger,
            automaterFactory: _ => automater,
            gatewayStatusProbe: (_, _, _) => Task.FromResult(false),
            gatewayWaiter: (_, _, _) => Task.CompletedTask);

        var act = () => helper.StartGatewayIfNeeded(CreateRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*LoginFailed*");

        automater.StartCalled.Should().BeTrue();
        automater.DisposeCalled.Should().BeTrue();
    }

    private IBAutomaterHelper CreateHelper(
        TestLogger logger,
        Func<IBAutomaterHelper.AutomaterSettings, IBAutomaterHelper.IIBAutomaterController>? automaterFactory = null,
        Func<string, int, CancellationToken, Task<bool>>? gatewayStatusProbe = null,
        Func<string, int, CancellationToken, Task>? gatewayWaiter = null,
        Dictionary<string, string>? config = null)
    {
        var effectiveConfig = config != null
            ? new Dictionary<string, string>(config)
            : new Dictionary<string, string>(_baseConfig);

        return new IBAutomaterHelper(
            effectiveConfig,
            logger,
            automaterFactory,
            gatewayStatusProbe,
            gatewayWaiter);
    }

    private static DownloadRequest CreateRequest(bool useAutomater = true, string host = "127.0.0.1", int port = 4001)
    {
        return new DownloadRequest
        {
            Symbol = "AAPL",
            SecurityType = "equity",
            Resolution = "minute",
            From = DateTime.Today.AddDays(-5),
            To = DateTime.Today.AddDays(-1),
            DataDir = Path.GetTempPath(),
            UseIbAutomater = useAutomater,
            GatewayHost = host,
            GatewayPort = port
        };
    }

    private sealed class TestLogger : ILogger
    {
        public List<string> Messages { get; } = new();

        public void LogTrace(string message, object? context = null) => Add("TRACE", message);
        public void LogDebug(string message, object? context = null) => Add("DEBUG", message);
        public void LogInfo(string message, object? context = null) => Add("INFO", message);
        public void LogWarning(string message, object? context = null) => Add("WARN", message);
        public void LogError(string message, object? context = null, Exception? exception = null) => Add("ERROR", message);

        private void Add(string level, string message) => Messages.Add($"{level}: {message}");
    }

    private sealed class FakeAutomater : IBAutomaterHelper.IIBAutomaterController
    {
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public bool DisposeCalled { get; private set; }
        public StartResult StartResultToReturn { get; set; } = new(ErrorCode.None, string.Empty);

        public event EventHandler<OutputDataReceivedEventArgs>? OutputDataReceived
        {
            add { }
            remove { }
        }

        public event EventHandler<ErrorDataReceivedEventArgs>? ErrorDataReceived
        {
            add { }
            remove { }
        }

        public event EventHandler<ExitedEventArgs>? Exited
        {
            add { }
            remove { }
        }

        public event EventHandler? Restarted
        {
            add { }
            remove { }
        }

        public StartResult Start(bool waitForExit)
        {
            StartCalled = true;
            return StartResultToReturn;
        }

        public void Stop()
        {
            StopCalled = true;
        }

        public bool IsRunning() => StartCalled && !StopCalled;

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }
}
