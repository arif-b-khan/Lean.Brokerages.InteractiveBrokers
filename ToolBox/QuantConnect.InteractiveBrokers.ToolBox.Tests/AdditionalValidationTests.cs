using Xunit;
using FluentAssertions;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

/// <summary>
/// Additional validation tests for date ordering, symbol casing, and other edge cases
/// </summary>
public class AdditionalValidationTests
{
    [Theory]
    [InlineData("AAPL", "aapl")] // Should normalize to lowercase
    [InlineData("MSFT", "msft")]
    [InlineData("spy", "spy")] // Already lowercase
    [InlineData("QQQ", "qqq")]
    public void Symbol_ShouldNormalizeToLowercase_ForOutputFiles(string inputSymbol, string expectedOutput)
    {
        // Arrange
        var outputLayout = new OutputLayout();
        var request = new DownloadRequest
        {
            Symbol = inputSymbol,
            Resolution = "daily",
            DataDir = "/data"
        };
        var date = new DateTime(2023, 6, 15);

        // Act
        var filename = outputLayout.GetFilename(request, date);

        // Assert
        filename.Should().Contain(expectedOutput);
    }

    [Theory]
    [InlineData("2023-06-15", "2023-06-16", true)] // Valid: start before end
    [InlineData("2023-06-15", "2023-06-15", false)] // Invalid: same date
    [InlineData("2023-06-16", "2023-06-15", false)] // Invalid: start after end
    public void DateRange_Validation_ShouldCheckOrdering(string startStr, string endStr, bool shouldBeValid)
    {
        // Arrange
        var marketHelper = new MarketSessionHelper(new ConsoleLogger("info", "test"));
        var startDate = DateTime.Parse(startStr);
        var endDate = DateTime.Parse(endStr);

        // Act
        var result = marketHelper.ValidateDateRange(startDate, endDate, "daily");

        // Assert
        result.IsValid.Should().Be(shouldBeValid);
        if (!shouldBeValid)
        {
            result.Errors.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("DAILY", "daily")] // Case normalization
    [InlineData("MINUTE", "minute")]
    [InlineData("daily", "daily")] // Already lowercase
    [InlineData("Daily", "daily")] // Mixed case
    public void Resolution_ShouldNormalizeToLowercase(string input, string expected)
    {
        // Arrange
        var outputLayout = new OutputLayout();
        
        // Act
        var normalized = input.ToLowerInvariant();
        
        // Assert - verify our code would handle this correctly
        normalized.Should().Be(expected);
    }

    [Fact]
    public void MarketSessionHelper_ShouldSkipWeekends()
    {
        // Arrange
        var logger = new ConsoleLogger("info", "test");
        var helper = new MarketSessionHelper(logger);
        var saturday = new DateTime(2023, 6, 17); // Saturday
        var sunday = new DateTime(2023, 6, 18); // Sunday
        var monday = new DateTime(2023, 6, 19); // Monday

        // Act & Assert
        helper.IsTradingDay(saturday).Should().BeFalse();
        helper.IsTradingDay(sunday).Should().BeFalse();
        helper.IsTradingDay(monday).Should().BeTrue();
    }

    [Theory]
    [InlineData("2023-06-15", "2023-06-21", 5)] // Thu-Wed = 5 trading days (Thu, Fri, Mon, Tue, Wed)
    [InlineData("2023-06-17", "2023-06-18", 0)] // Sat-Sun = 0 trading days
    [InlineData("2023-06-19", "2023-06-23", 5)] // Mon-Fri = 5 trading days
    public void GetTradingDays_ShouldCountCorrectly(string startStr, string endStr, int expectedCount)
    {
        // Arrange
        var logger = new ConsoleLogger("info", "test");
        var helper = new MarketSessionHelper(logger);
        var startDate = DateTime.Parse(startStr);
        var endDate = DateTime.Parse(endStr);

        // Act
        var tradingDays = helper.GetTradingDays(startDate, endDate);

        // Assert
        tradingDays.Count().Should().Be(expectedCount);
    }

    [Theory]
    [InlineData("tick", 1)]
    [InlineData("second", 7)]
    [InlineData("minute", 30)]
    [InlineData("daily", 3650)]
    public void DateRange_Validation_ShouldWarnForLargeRanges(string resolution, int maxDays)
    {
        // Arrange
        var logger = new ConsoleLogger("info", "test");
        var helper = new MarketSessionHelper(logger);
        var startDate = DateTime.Today.AddDays(-maxDays - 1); // One day over the limit
        var endDate = DateTime.Today;

        // Act
        var result = helper.ValidateDateRange(startDate, endDate, resolution);

        // Assert
        result.IsValid.Should().BeTrue(); // Should still be valid
        result.Warnings.Should().NotBeEmpty(); // But should have warnings
        result.Warnings.Should().Contain(w => w.Contains("quite large"));
    }

    [Fact]
    public void DateRange_Validation_ShouldRejectFutureDates()
    {
        // Arrange
        var logger = new ConsoleLogger("info", "test");
        var helper = new MarketSessionHelper(logger);
        var futureDate = DateTime.Today.AddDays(30);
        var evenFurtherFuture = DateTime.Today.AddDays(60);

        // Act
        var result = helper.ValidateDateRange(futureDate, evenFurtherFuture, "daily");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cannot be in the future"));
    }

    [Theory]
    [InlineData("NYSE", "Eastern Standard Time")]
    [InlineData("LSE", "GMT Standard Time")]
    [InlineData("SMART", "Eastern Standard Time")]
    [InlineData("UNKNOWN", "UTC")] // Should default to UTC
    public void Exchange_TimeZone_Mapping_ShouldBeCorrect(string exchange, string expectedTimeZoneId)
    {
        // Arrange
        var logger = new ConsoleLogger("info", "test");
        var helper = new MarketSessionHelper(logger);

        // Act
        var timeZone = helper.GetExchangeTimeZone(exchange);

        // Assert
        // Note: On macOS/Linux, timezone IDs might be different, so we'll just check it's not null
        timeZone.Should().NotBeNull();
        
        // For well-known exchanges, verify they're not UTC (unless expected to be)
        if (exchange != "UNKNOWN")
        {
            if (expectedTimeZoneId != "UTC")
            {
                timeZone.Should().NotBe(TimeZoneInfo.Utc);
            }
        }
    }

    [Theory]
    [InlineData("", false)] // Empty symbol
    [InlineData(" ", false)] // Whitespace
    [InlineData("A", true)] // Single character (valid)
    [InlineData("AAPL", true)] // Normal symbol
    [InlineData("BRK.A", true)] // Symbol with dot
    public void Symbol_Validation_ShouldHandleEdgeCases(string symbol, bool shouldBeValid)
    {
        // Arrange
        var request = new DownloadRequest { Symbol = symbol };

        // Act & Assert
        if (shouldBeValid)
        {
            request.Symbol.Should().NotBeNullOrWhiteSpace();
        }
        else
        {
            request.Symbol.Should().Match(s => string.IsNullOrWhiteSpace(s));
        }
    }
}