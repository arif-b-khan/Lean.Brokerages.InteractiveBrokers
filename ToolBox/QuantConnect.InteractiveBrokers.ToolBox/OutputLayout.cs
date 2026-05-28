namespace QuantConnect.InteractiveBrokers.ToolBox;

/// <summary>
/// Helper class for generating LEAN-compatible file paths and CSV formatting
/// </summary>
public class OutputLayout
{
    /// <summary>
    /// Get the directory path for a download request following LEAN conventions
    /// </summary>
    public string GetPath(DownloadRequest request)
    {
        var securityType = request.SecurityType.ToLowerInvariant();
        var resolution = request.Resolution.ToLowerInvariant();
        var symbol = request.Symbol.ToLowerInvariant();
        // Default to USA market for equities
        var market = securityType == "equity" ? "usa" : "generic";

        // Place the symbol directory directly under the resolution folder, e.g.
        // {dataDir}/{securityType}/{market}/{resolution}/{symbol}
        return Path.Combine(
            request.DataDir,
            securityType,
            market,
            resolution,
            symbol
        );
    }

    /// <summary>
    /// Get the filename for a specific date and request
    /// </summary>
    public string GetFilename(DownloadRequest request, DateTime date)
    {
        var resolution = request.Resolution.ToLowerInvariant();
        
        return resolution switch
        {
            "minute" or "second" or "tick" => $"{date:yyyyMMdd}_trade.zip",
            "hour" => $"{date:yyyyMMdd}_trade.zip",
            "daily" => $"{request.Symbol.ToLowerInvariant()}.zip",
            _ => throw new ArgumentException($"Unsupported resolution: {request.Resolution}")
        };
    }

    /// <summary>
    /// Get the first letter folder for symbol partitioning
    /// </summary>
    public string GetFirstLetterFolder(string symbol)
    {
        return symbol.ToLowerInvariant()[0].ToString();
    }

    /// <summary>
    /// Serialize a bar to LEAN CSV format
    /// </summary>
    public string SerializeBar(string resolution, IBar bar)
    {
        var timestamp = bar.Time.ToString("yyyyMMdd HH:mm:ss");
        
        return resolution.ToLowerInvariant() switch
        {
            "minute" or "hour" or "daily" => 
                $"{timestamp},{bar.Open},{bar.High},{bar.Low},{bar.Close},{bar.Volume}",
            "second" or "tick" =>
                $"{timestamp},{bar.Open},{bar.High},{bar.Low},{bar.Close},{bar.Volume}",
            _ => throw new ArgumentException($"Unsupported resolution for serialization: {resolution}")
        };
    }
}

/// <summary>
/// Interface representing a price bar with OHLCV data
/// </summary>
public interface IBar
{
    DateTime Time { get; }
    decimal Open { get; }
    decimal High { get; }
    decimal Low { get; }
    decimal Close { get; }
    long Volume { get; }
}

/// <summary>
/// Basic implementation of IBar for testing and data storage
/// </summary>
public class Bar : IBar
{
    public DateTime Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }

    public Bar() { }

    public Bar(DateTime time, decimal open, decimal high, decimal low, decimal close, long volume)
    {
        Time = time;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
    }
}