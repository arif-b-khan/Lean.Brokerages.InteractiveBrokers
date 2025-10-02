using FluentAssertions;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

public class OutputLayoutTests
{
    [Fact]
    public void GetPath_ForEquityMinute_ShouldReturnCorrectLeanPath()
    {
        // Arrange
        var request = new DownloadRequest
        {
            Symbol = "AAPL",
            SecurityType = "Equity",
            Exchange = "SMART",
            Currency = "USD",
            Resolution = "Minute",
            DataDir = "/data"
        };
        var layout = new OutputLayout();

        // Act & Assert - This will fail until OutputLayout.cs is implemented
        var path = layout.GetPath(request);
        path.Should().Be("/data/equity/usa/minute/a/aapl", 
            "Should follow LEAN equity minute path convention");
    }

    [Fact]
    public void GetPath_ForEquityDaily_ShouldReturnCorrectLeanPath()
    {
        // Arrange
        var request = new DownloadRequest
        {
            Symbol = "TSLA",
            SecurityType = "Equity",
            Exchange = "SMART",
            Currency = "USD",
            Resolution = "Daily",
            DataDir = "/data"
        };
        var layout = new OutputLayout();

        // Act & Assert - This will fail until OutputLayout.cs is implemented
        var path = layout.GetPath(request);
        path.Should().Be("/data/equity/usa/daily/t/tsla",
            "Should follow LEAN equity daily path convention");
    }

    [Fact]
    public void GetFilename_ForMinuteResolution_ShouldIncludeDateAndExtension()
    {
        // Arrange
        var request = new DownloadRequest
        {
            Symbol = "AAPL",
            SecurityType = "Equity",
            Resolution = "Minute"
        };
        var layout = new OutputLayout();
        var date = new DateTime(2024, 1, 15);

        // Act & Assert - This will fail until OutputLayout.cs is implemented
        var filename = layout.GetFilename(request, date);
        filename.Should().Be("20240115_trade.zip",
            "Should follow LEAN minute filename convention with date and extension");
    }

    [Fact]
    public void GetFilename_ForDailyResolution_ShouldUseCorrectFormat()
    {
        // Arrange
        var request = new DownloadRequest
        {
            Symbol = "AAPL",
            SecurityType = "Equity", 
            Resolution = "Daily"
        };
        var layout = new OutputLayout();
        var date = new DateTime(2024, 1, 15);

        // Act & Assert - This will fail until OutputLayout.cs is implemented
        var filename = layout.GetFilename(request, date);
        filename.Should().Be("aapl.zip",
            "Should follow LEAN daily filename convention");
    }

    [Fact]
    public void SerializeBar_ShouldFormatAsLeanCsv()
    {
        // Arrange
        var layout = new OutputLayout();
        var bar = new TestBar
        {
            Time = new DateTime(2024, 1, 15, 9, 30, 0),
            Open = 150.25m,
            High = 151.00m,
            Low = 149.75m,
            Close = 150.80m,
            Volume = 1000000
        };

        // Act & Assert - This will fail until OutputLayout.cs is implemented
        var csvLine = layout.SerializeBar("Minute", bar);
        csvLine.Should().StartWith("20240115 09:30:00,150.25,151.00,149.75,150.80,1000000",
            "Should format bar as LEAN CSV with timestamp and OHLCV");
    }

    [Theory]
    [InlineData("aapl", "a")]
    [InlineData("msft", "m")]
    [InlineData("1234", "1")]
    [InlineData("googl", "g")]
    public void GetFirstLetterFolder_ShouldReturnLowercaseFirstChar(string symbol, string expected)
    {
        // Arrange
        var layout = new OutputLayout();

        // Act & Assert - This will fail until OutputLayout.cs is implemented
        var folder = layout.GetFirstLetterFolder(symbol);
        folder.Should().Be(expected, $"First letter folder for {symbol} should be {expected}");
    }
}

// Test helper class - will be replaced by actual data structures
public class TestBar : IBar
{
    public DateTime Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

