namespace QuantConnect.InteractiveBrokers.ToolBox;

/// <summary>
/// Interface for downloading historical data from a brokerage
/// </summary>
public interface IDataDownloader
{
    /// <summary>
    /// Fetch historical bars for the given request
    /// </summary>
    Task<IEnumerable<IBar>> FetchBars(DownloadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test connection to the data source
    /// </summary>
    Task<bool> TestConnection(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interactive Brokers implementation of the data downloader
/// This is a stub implementation that will be expanded to use actual IB client
/// </summary>
public class InteractiveBrokersDownloader : IDataDownloader
{
    private readonly BackoffPolicy _backoffPolicy;
    private readonly ILogger _logger;
    private readonly Dictionary<string, string> _config;

    public InteractiveBrokersDownloader(
        Dictionary<string, string> config,
        BackoffPolicy backoffPolicy,
        ILogger logger)
    {
        _config = config;
        _backoffPolicy = backoffPolicy;
        _logger = logger;
    }

    /// <summary>
    /// Fetch historical bars from Interactive Brokers
    /// </summary>
    public Task<IEnumerable<IBar>> FetchBars(DownloadRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInfo($"Fetching bars for {request.Symbol} from {request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd}");
        
        // TODO: Implement actual IB API integration
        // For now, return sample data for development/testing across the requested date range
        var allBars = new List<IBar>();

        var date = request.From.Date;
        var endDate = request.To.Date;

        while (date <= endDate)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsMarketDay(date))
            {
                var dayBars = GenerateSampleBars(request.Symbol, date, request.Resolution);
                if (dayBars?.Count > 0)
                {
                    allBars.AddRange(dayBars);
                }
            }

            date = date.AddDays(1);
        }

        _logger.LogInfo($"Generated {allBars.Count} sample bars for {request.Symbol} spanning {request.From:yyyy-MM-dd} -> {request.To:yyyy-MM-dd}");
        return Task.FromResult<IEnumerable<IBar>>(allBars);
    }

    /// <summary>
    /// Test connection to Interactive Brokers Gateway/TWS
    /// </summary>
    public async Task<bool> TestConnection(CancellationToken cancellationToken = default)
    {
        return await _backoffPolicy.ExecuteIbCall(async () =>
        {
            var host = _config.GetValueOrDefault("GATEWAY_HOST", "127.0.0.1");
            var portStr = _config.GetValueOrDefault("GATEWAY_PORT", "7497");
            
            if (!int.TryParse(portStr, out var port))
            {
                throw new ArgumentException($"Invalid GATEWAY_PORT value: {portStr}. Must be a valid integer.");
            }

            _logger.LogInfo($"Testing connection to IB Gateway at {host}:{port}");
            
            try
            {
                // TODO: Replace with actual IB client connection test
                // For now, test basic TCP connectivity to the gateway
                using var tcpClient = new System.Net.Sockets.TcpClient();
                await tcpClient.ConnectAsync(host, port, cancellationToken);
                
                _logger.LogInfo("Basic TCP connection successful");
                
                // TODO: Add IB client handshake and authentication test
                // This would involve:
                // 1. Create IB client instance
                // 2. Connect with credentials
                // 3. Verify account access
                // 4. Test market data permissions
                
                _logger.LogInfo("Connection test successful (basic TCP connectivity verified)");
                return true;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                var message = GetConnectionErrorMessage(host, port, ex);
                _logger.LogError($"Connection failed: {message}");
                throw new InvalidOperationException(message, ex);
            }
            catch (TimeoutException ex)
            {
                var message = $"Connection to {host}:{port} timed out. Ensure the IB Gateway/TWS is running and accepting connections.";
                _logger.LogError($"Connection failed: {message}");
                throw new InvalidOperationException(message, ex);
            }
            
        }, cancellationToken);
    }

    /// <summary>
    /// Get a user-friendly error message for connection failures
    /// </summary>
    private static string GetConnectionErrorMessage(string host, int port, System.Net.Sockets.SocketException ex)
    {
        return ex.SocketErrorCode switch
        {
            System.Net.Sockets.SocketError.ConnectionRefused => 
                $"Connection refused to {host}:{port}. Ensure IB Gateway/TWS is running and configured to accept connections on this port.",
            
            System.Net.Sockets.SocketError.HostNotFound => 
                $"Host {host} not found. Check the GATEWAY_HOST configuration.",
            
            System.Net.Sockets.SocketError.NetworkUnreachable => 
                $"Network unreachable to {host}:{port}. Check your network connection and firewall settings.",
            
            System.Net.Sockets.SocketError.TimedOut => 
                $"Connection to {host}:{port} timed out. The gateway may not be responding.",
            
            _ => 
                $"Failed to connect to {host}:{port}: {ex.Message}. Check that IB Gateway/TWS is running and accessible."
        };
    }

    /// <summary>
    /// Check if a date is a market trading day (basic implementation)
    /// </summary>
    private static bool IsMarketDay(DateTime date)
    {
        var dayOfWeek = date.DayOfWeek;
        return dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday;
    }

    /// <summary>
    /// Generate sample bar data for testing purposes
    /// </summary>
    private List<Bar> GenerateSampleBars(string symbol, DateTime date, string resolution)
    {
        var bars = new List<Bar>();
        var random = new Random(date.GetHashCode() + symbol.GetHashCode());
        
        var basePrice = 100.0m + (decimal)(random.NextDouble() * 50); // Random base price 100-150
        
        if (resolution.ToLowerInvariant() == "daily")
        {
            // Generate one bar per day
            var open = basePrice;
            var high = open + (decimal)(random.NextDouble() * 5);
            var low = open - (decimal)(random.NextDouble() * 5);
            var close = low + (decimal)(random.NextDouble() * (double)(high - low));
            var volume = (long)(1000000 + random.NextDouble() * 5000000); // 1M-6M volume
            
            bars.Add(new Bar(date, open, high, low, close, volume));
        }
        else if (resolution.ToLowerInvariant() == "minute")
        {
            // Generate bars for market hours (9:30 AM - 4:00 PM EST)
            var marketOpen = date.Date.AddHours(9).AddMinutes(30);
            var marketClose = date.Date.AddHours(16);
            
            var currentTime = marketOpen;
            var currentPrice = basePrice;
            
            while (currentTime < marketClose)
            {
                var priceChange = (decimal)((random.NextDouble() - 0.5) * 0.5); // ±$0.25 max change
                currentPrice = Math.Max(1, currentPrice + priceChange);
                
                var open = currentPrice;
                var high = open + (decimal)(random.NextDouble() * 0.2);
                var low = open - (decimal)(random.NextDouble() * 0.2);
                var close = low + (decimal)(random.NextDouble() * (double)(high - low));
                var volume = (long)(1000 + random.NextDouble() * 9000); // 1K-10K volume per minute
                
                bars.Add(new Bar(currentTime, open, high, low, close, volume));
                
                currentTime = currentTime.AddMinutes(1);
                currentPrice = close;
            }
        }
        
        return bars;
    }
}

