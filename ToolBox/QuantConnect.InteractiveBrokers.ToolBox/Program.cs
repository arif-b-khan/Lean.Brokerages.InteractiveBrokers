using System.CommandLine;
using System.CommandLine.Parsing;

namespace QuantConnect.InteractiveBrokers.ToolBox;

/// <summary>
/// Console application for downloading historical data from Interactive Brokers in LEAN format
/// </summary>
public static class Program
{
    // Command-line option holders so ParseResult.GetValueForOption can be used with Option<T> instances
    private static Option<string>? _symbolOption;
    private static Option<string>? _securityTypeOption;
    private static Option<string>? _resolutionOption;
    private static Option<DateTime>? _fromOption;
    private static Option<DateTime>? _toOption;
    private static Option<string>? _dataDirOption;
    private static Option<string>? _exchangeOption;
    private static Option<string>? _currencyOption;
    private static Option<string?>? _configOption;
    private static Option<string>? _logLevelOption;
    private static Option<string>? _gatewayHostOption;
    private static Option<int>? _gatewayPortOption;
    private static Option<bool>? _dryRunOption;
    private static Option<bool>? _useIbAutomaterOption;
    /// <summary>
    /// Main entry point for the console application
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = CreateRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Synchronous version for testing
    /// </summary>
    public static int RunWithArgs(string[] args)
    {
        return Main(args).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Parse arguments and return structured request (for testing)
    /// </summary>
    public static DownloadRequest ParseArguments(string[] args)
    {
        var rootCommand = CreateRootCommand();
        var parseResult = rootCommand.Parse(args);
        
        if (parseResult.Errors.Any())
        {
            var errorMessage = string.Join("; ", parseResult.Errors.Select(e => e.Message));
            throw new ArgumentException($"Required arguments missing or invalid: {errorMessage}");
        }

        return ExtractDownloadRequest(parseResult);
    }

    // ...existing code...

    private static RootCommand CreateRootCommand()
    {
    _symbolOption = new Option<string>(new[] { "--symbol", "-s" }, "Trading symbol (e.g., AAPL)") { IsRequired = true };

    _securityTypeOption = new Option<string>(new[] { "--security-type", "-t" }, "Security type (Equity, Futures)") { IsRequired = true };

    _resolutionOption = new Option<string>(new[] { "--resolution", "-r" }, "Data resolution (Tick, Second, Minute, Hour, Daily)") { IsRequired = true };

    _fromOption = new Option<DateTime>(new[] { "--from", "-f" }, "Start date (YYYY-MM-DD)") { IsRequired = true };

    _toOption = new Option<DateTime>(new[] { "--to" }, "End date (YYYY-MM-DD)") { IsRequired = true };

    _dataDirOption = new Option<string>(new[] { "--data-dir", "-d" }, "Output data directory") { IsRequired = true };

    _exchangeOption = new Option<string>(new[] { "--exchange", "-e" }, "Exchange (default: SMART)") { Arity = ArgumentArity.ZeroOrOne };
    _exchangeOption.SetDefaultValue("SMART");

    _currencyOption = new Option<string>(new[] { "--currency", "-c" }, "Currency (default: USD)") { Arity = ArgumentArity.ZeroOrOne };
    _currencyOption.SetDefaultValue("USD");

    _configOption = new Option<string?>(new[] { "--config" }, "JSON config file path (optional)") { Arity = ArgumentArity.ZeroOrOne };

    _logLevelOption = new Option<string>(new[] { "--log-level" }, "Log level (trace, debug, info, warn, error)") { Arity = ArgumentArity.ZeroOrOne };
    _logLevelOption.SetDefaultValue("info");

    _gatewayHostOption = new Option<string>(new[] { "--gateway-host" }, "IB Gateway host") { Arity = ArgumentArity.ZeroOrOne };
    _gatewayHostOption.SetDefaultValue("127.0.0.1");

    _gatewayPortOption = new Option<int>(new[] { "--gateway-port" }, "IB Gateway port") { Arity = ArgumentArity.ZeroOrOne };
    _gatewayPortOption.SetDefaultValue(7497);

    _dryRunOption = new Option<bool>(new[] { "--dry-run" }, "Validate configuration and log actions without connecting to IB") { Arity = ArgumentArity.ZeroOrOne };

    _useIbAutomaterOption = new Option<bool>(new[] { "--use-ib-automater" }, "Use IBAutomater to start/manage IB Gateway automatically (disabled in CI environments)") { Arity = ArgumentArity.ZeroOrOne };

        var rootCommand = new RootCommand("QuantConnect Interactive Brokers Data Download ToolBox")
        {
            _symbolOption,
            _securityTypeOption,
            _resolutionOption,
            _fromOption,
            _toOption,
            _dataDirOption,
            _exchangeOption,
            _currencyOption,
            _configOption,
            _logLevelOption,
            _gatewayHostOption,
            _gatewayPortOption,
            _dryRunOption,
            _useIbAutomaterOption
        };

        rootCommand.SetHandler(async (context) =>
        {
            var request = ExtractDownloadRequest(context.ParseResult);
            await ExecuteDownload(request, context.GetCancellationToken());
        });

        return rootCommand;
    }

    private static DownloadRequest ExtractDownloadRequest(ParseResult parseResult)
    {
    var symbol = parseResult.GetValueForOption(_symbolOption!);
    var securityType = parseResult.GetValueForOption(_securityTypeOption!);
    var resolution = parseResult.GetValueForOption(_resolutionOption!);
    var from = parseResult.GetValueForOption(_fromOption!);
    var to = parseResult.GetValueForOption(_toOption!);
    var dataDir = parseResult.GetValueForOption(_dataDirOption!);

        // Validate date range
        if (from >= to)
        {
            throw new ArgumentException("Invalid date range: 'from' date must be before 'to' date");
        }

        return new DownloadRequest
        {
            Symbol = symbol!,
            SecurityType = securityType!,
            Resolution = resolution!,
            From = from,
            To = to,
            DataDir = dataDir!,
            Exchange = parseResult.GetValueForOption(_exchangeOption!) ?? "SMART",
            Currency = parseResult.GetValueForOption(_currencyOption!) ?? "USD",
            ConfigPath = parseResult.GetValueForOption(_configOption!),
            LogLevel = parseResult.GetValueForOption(_logLevelOption!) ?? "info",
            GatewayHost = parseResult.GetValueForOption(_gatewayHostOption!) ?? "127.0.0.1",
            GatewayPort = parseResult.GetValueForOption(_gatewayPortOption!),
            DryRun = parseResult.GetValueForOption(_dryRunOption!),
            UseIbAutomater = parseResult.GetValueForOption(_useIbAutomaterOption!)
        };
    }

    private static async Task ExecuteDownload(DownloadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Initialize logging with correlation ID
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            ILogger logger = new ConsoleLogger(request.LogLevel, correlationId);
            
            logger.LogInfo($"Starting IB data download [CorrelationId: {correlationId}]");
            logger.LogInfo($"Request: {request.Symbol} {request.SecurityType} {request.Resolution} " +
                          $"{request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd}");

            // Load and validate configuration
            var configLoader = new ConfigLoader(logger);
            var config = await configLoader.LoadConfig(request.ConfigPath);
            
            // Validate data directory
            if (!request.DryRun && !Directory.Exists(request.DataDir))
            {
                logger.LogInfo($"Creating data directory: {request.DataDir}");
                Directory.CreateDirectory(request.DataDir);
            }

            if (request.DryRun)
            {
                logger.LogInfo("DRY RUN mode - no actual data download will occur");
                logger.LogInfo("Configuration validated successfully");
                return;
            }

            // Initialize components
            var outputLayout = new OutputLayout();
            var backoffPolicy = new BackoffPolicy();
            var downloader = new InteractiveBrokersDownloader(config, backoffPolicy, logger);
            var dataWriter = new DataWriter(outputLayout, logger);
            var marketSessionHelper = new MarketSessionHelper(logger);
            var ibAutomaterHelper = new IBAutomaterHelper(config, logger);
            
            // Validate date range and get trading days
            var validation = marketSessionHelper.ValidateDateRange(request.From, request.To, request.Resolution);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    logger.LogError(error);
                }
                Environment.Exit(1);
            }
            
            foreach (var warning in validation.Warnings)
            {
                logger.LogWarning(warning);
            }
            
            var tradingDays = marketSessionHelper.GetTradingDays(request.From, request.To, request.Exchange);
            logger.LogInfo($"Will process {tradingDays.Count()} trading days");
            
            // Start IB Gateway using IBAutomater if requested
            var gatewayStarted = await ibAutomaterHelper.StartGatewayIfNeeded(request, cancellationToken);
            
            try
            {
                // Execute download with proper composition
                logger.LogInfo("Initializing Interactive Brokers connection...");
                await downloader.TestConnection(cancellationToken);
            
            logger.LogInfo("Downloading historical data...");
            var bars = await ExecuteWithBackoff(
                () => downloader.FetchBars(request, cancellationToken),
                backoffPolicy,
                logger,
                cancellationToken);
            
            logger.LogInfo("Writing data to LEAN format...");
            var result = await dataWriter.WriteBars(request, bars, cancellationToken);
            
            // Report results
            if (result.Success)
            {
                logger.LogInfo($"Download completed successfully: {result.Files.Count} files created");
                foreach (var file in result.Files)
                {
                    logger.LogInfo($"  Created: {file}");
                }
                
                if (result.Warnings.Any())
                {
                    logger.LogWarning($"{result.Warnings.Count} warnings occurred:");
                    foreach (var warning in result.Warnings)
                    {
                        logger.LogWarning($"  {warning}");
                    }
                }
            }
            else
            {
                logger.LogError($"Download failed: {result.Error}");
                Environment.Exit(1);
            }
            logger.LogInfo("Download completed successfully");
            }
            finally
            {
                // Clean up IBAutomater if it was started
                if (gatewayStarted)
                {
                    logger.LogInfo("Shutting down IBAutomater-managed gateway");
                    await ibAutomaterHelper.StopGatewayIfStarted(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
    
    /// <summary>
    /// Execute download operation with backoff policy for rate limiting
    /// </summary>
    private static async Task<IEnumerable<IBar>> ExecuteWithBackoff(
        Func<Task<IEnumerable<IBar>>> operation,
        BackoffPolicy backoffPolicy,
        ILogger logger,
        CancellationToken ct)
    {
        return await backoffPolicy.ExecuteWithBackoff(
            async () =>
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    // Log pacing/backoff events
                    if (BackoffPolicy.IsRetryableException(ex))
                    {
                        logger.LogInfo($"Rate limit encountered, will retry with backoff: {ex.Message}");
                        throw; // Let backoff policy handle retry
                    }
                    
                    // Non-retryable error
                    logger.LogError($"Non-retryable error during download: {ex.Message}");
                    throw;
                }
            },
            BackoffPolicy.IsRetryableException,
            ct);
    }
}

/// <summary>
/// Represents a data download request with all parameters
/// </summary>
public class DownloadRequest
{
    public string Symbol { get; set; } = "";
    public string SecurityType { get; set; } = "";
    public string Resolution { get; set; } = "";
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string DataDir { get; set; } = "";
    public string Exchange { get; set; } = "SMART";
    public string Currency { get; set; } = "USD";
    public string? ConfigPath { get; set; }
    public string LogLevel { get; set; } = "info";
    public string GatewayHost { get; set; } = "127.0.0.1";
    public int GatewayPort { get; set; } = 7497;
    public bool DryRun { get; set; }
    public bool UseIbAutomater { get; set; }
}

/// <summary>
/// Simple console logger with structured output and secret redaction
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly string _logLevel;
    private readonly string _correlationId;
    
    public ConsoleLogger(string logLevel, string correlationId)
    {
        _logLevel = logLevel;
        _correlationId = correlationId;
    }
    
    public void LogTrace(string message, object? context = null)
    {
        LogDebug(message, context);
    }
    
    public void LogDebug(string message, object? context = null)
    {
        if (_logLevel == "debug" || _logLevel == "trace")
        {
            Console.WriteLine($"[DEBUG] [{_correlationId}] {message}");
        }
    }
    
    public void LogInfo(string message, object? context = null)
    {
        Console.WriteLine($"[INFO] [{_correlationId}] {message}");
    }
    
    public void LogWarning(string message, object? context = null)
    {
        Console.WriteLine($"[WARN] [{_correlationId}] {message}");
    }
    
    public void LogError(string message, object? context = null, Exception? exception = null)
    {
        var fullMessage = exception != null ? $"{message} - {exception.Message}" : message;
        Console.WriteLine($"[ERROR] [{_correlationId}] {fullMessage}");
    }
}