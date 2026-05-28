using System.CommandLine;
using FluentAssertions;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

public class CliParsingTests
{
    [Fact]
    public void ParseArguments_WithRequiredOptions_ShouldSucceed()
    {
        // Arrange
        var args = new[]
        {
            "--symbol", "AAPL",
            "--security-type", "Equity",
            "--resolution", "Minute",
            "--from", "2024-01-01",
            "--to", "2024-01-31",
            "--data-dir", "./data"
        };

        // Act & Assert - This will fail until Program.cs is implemented
        var result = Program.ParseArguments(args);
        result.Should().NotBeNull("CLI parsing should succeed with all required options");
    }

    [Fact]
    public void ParseArguments_WithOptionalOptions_ShouldSucceed()
    {
        // Arrange
        var args = new[]
        {
            "--symbol", "AAPL",
            "--security-type", "Equity",
            "--resolution", "Minute",
            "--from", "2024-01-01",
            "--to", "2024-01-31",
            "--data-dir", "./data",
            "--exchange", "SMART",
            "--currency", "USD",
            "--config", "config.json",
            "--log-level", "info"
        };

        // Act & Assert - This will fail until Program.cs is implemented
        var result = Program.ParseArguments(args);
        result.Should().NotBeNull("CLI parsing should succeed with optional options");
        result.Exchange.Should().Be("SMART");
        result.Currency.Should().Be("USD");
        result.ConfigPath.Should().Be("config.json");
        result.LogLevel.Should().Be("info");
    }

    [Fact]
    public void ParseArguments_WithMissingRequiredOptions_ShouldFail()
    {
        // Arrange
        var args = new[]
        {
            "--symbol", "AAPL"
            // Missing required options
        };

        // Act & Assert - This will fail until Program.cs is implemented
        var exception = Assert.Throws<ArgumentException>(() => Program.ParseArguments(args));
        exception.Message.Should().Contain("required", "Should indicate missing required arguments");
    }

    [Fact]
    public void ParseArguments_WithHelpFlag_ShouldPrintUsageAndExitZero()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act & Assert - This will fail until Program.cs is implemented
        var exitCode = Program.RunWithArgs(args);
        exitCode.Should().Be(0, "Help flag should exit with success code");
    }

    [Fact]
    public void ParseArguments_WithInvalidDateRange_ShouldFail()
    {
        // Arrange
        var args = new[]
        {
            "--symbol", "AAPL",
            "--security-type", "Equity",
            "--resolution", "Minute",
            "--from", "2024-01-31",
            "--to", "2024-01-01", // to < from
            "--data-dir", "./data"
        };

        // Act & Assert - This will fail until validation is implemented
        var exception = Assert.Throws<ArgumentException>(() => Program.ParseArguments(args));
        exception.Message.Should().Contain("date range", "Should validate date ordering");
    }
}