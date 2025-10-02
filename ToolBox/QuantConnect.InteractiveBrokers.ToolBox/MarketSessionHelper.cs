namespace QuantConnect.InteractiveBrokers.ToolBox;

/// <summary>
/// Helper for handling date ranges and market session filtering
/// </summary>
public class MarketSessionHelper
{
    private readonly ILogger _logger;

    public MarketSessionHelper(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get trading days within the specified date range, skipping weekends and known holidays
    /// </summary>
    public IEnumerable<DateTime> GetTradingDays(DateTime startDate, DateTime endDate, string exchange = "SMART")
    {
        var current = startDate.Date;
        var end = endDate.Date;
        var tradingDays = new List<DateTime>();

        while (current <= end)
        {
            if (IsTradingDay(current, exchange))
            {
                tradingDays.Add(current);
            }
            current = current.AddDays(1);
        }

        _logger.LogDebug($"Found {tradingDays.Count} trading days between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd} for {exchange}");
        return tradingDays;
    }

    /// <summary>
    /// Check if a given date is a trading day for the specified exchange
    /// For v1, this implements basic weekday filtering. Future versions can add exchange-specific holidays.
    /// </summary>
    public bool IsTradingDay(DateTime date, string exchange = "SMART")
    {
        var dayOfWeek = date.DayOfWeek;
        
        // Skip weekends for all exchanges
        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        // TODO: Add exchange-specific holiday calendars
        // For now, treat all weekdays as trading days
        // Known major holidays that could be added:
        // - US: New Year's Day, MLK Day, Presidents Day, Good Friday, Memorial Day, 
        //       Independence Day, Labor Day, Thanksgiving, Christmas
        // - International exchanges have their own holiday calendars

        return true;
    }

    /// <summary>
    /// Adjust bar timestamps to align with market hours and exchange time zones
    /// For v1, this is a placeholder that returns bars as-is
    /// </summary>
    public IEnumerable<IBar> AlignBarsToMarketHours(IEnumerable<IBar> bars, string exchange = "SMART", string resolution = "minute")
    {
        // TODO: Implement proper market hours alignment
        // This would include:
        // - Converting bar timestamps to exchange time zone
        // - Filtering bars outside regular trading hours
        // - Handling pre-market and after-hours sessions
        // - Exchange-specific market open/close times
        
        var barsList = bars.ToList();
        _logger.LogDebug($"Market hours alignment not yet implemented - returning {barsList.Count} bars as-is");
        
        return barsList;
    }

    /// <summary>
    /// Get the appropriate time zone for an exchange
    /// </summary>
    public TimeZoneInfo GetExchangeTimeZone(string exchange)
    {
        // TODO: Add comprehensive exchange timezone mapping
        return exchange.ToUpperInvariant() switch
        {
            "NYSE" or "NASDAQ" or "SMART" => TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
            "LSE" => TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"),
            "TSE" => TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"),
            "HKEX" => TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"),
            _ => TimeZoneInfo.Utc // Default to UTC for unknown exchanges
        };
    }

    /// <summary>
    /// Validate that the requested date range is reasonable for data download
    /// </summary>
    public ValidationResult ValidateDateRange(DateTime startDate, DateTime endDate, string resolution)
    {
        var result = new ValidationResult();
        var today = DateTime.Today;
        
        // Check if date range is valid
        if (startDate >= endDate)
        {
            result.IsValid = false;
            result.Errors.Add("Start date must be before end date");
            return result;
        }

        // Check if start date is too far in the future
        if (startDate > today)
        {
            result.IsValid = false;
            result.Errors.Add("Start date cannot be in the future");
            return result;
        }

        // Warn if end date is in the future
        if (endDate > today)
        {
            result.Warnings.Add($"End date {endDate:yyyy-MM-dd} is in the future, will only download data up to {today:yyyy-MM-dd}");
            endDate = today;
        }

        // Check for very large date ranges that might cause issues
        var daySpan = (endDate - startDate).TotalDays;
        var maxDaysForResolution = GetMaxRecommendedDays(resolution);
        
        if (daySpan > maxDaysForResolution)
        {
            result.Warnings.Add($"Date range of {daySpan:F0} days is quite large for {resolution} resolution. " +
                              $"Consider breaking into smaller chunks if you encounter rate limiting issues.");
        }

        result.IsValid = true;
        return result;
    }

    /// <summary>
    /// Get the maximum recommended days for a given resolution to avoid rate limiting
    /// </summary>
    private static int GetMaxRecommendedDays(string resolution)
    {
        return resolution.ToLowerInvariant() switch
        {
            "tick" => 1,        // Tick data is very high volume
            "second" => 7,      // Second data is high volume
            "minute" => 30,     // Minute data is moderate volume
            "hour" => 365,      // Hourly data is lower volume
            "daily" => 3650,    // Daily data can span many years
            _ => 30             // Default to conservative estimate
        };
    }
}

/// <summary>
/// Result of date range validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}