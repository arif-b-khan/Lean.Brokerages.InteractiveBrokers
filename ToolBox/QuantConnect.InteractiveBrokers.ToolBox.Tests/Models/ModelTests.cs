using System.Text.Json;
using FluentAssertions;
using QuantConnect.InteractiveBrokers.ToolBox.Models;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests.Models;

public class ModelTests
{
    [Fact]
    public void BrokerageConfiguration_ValidationFlagsMissingFields()
    {
        var config = new BrokerageConfiguration
        {
            Username = "",
            Account = "",
            DataDirectory = ""
        };

        var errors = config.Validate();
        errors.Should().Contain("Username (IB_USERNAME) is required.");
        errors.Should().Contain("Account (IB_ACCOUNT) is required.");
        errors.Should().Contain("DataDirectory (DATA_DIR) is required.");
    }

    [Fact]
    public void BrokerageConfiguration_ToEnvironmentVariablesIncludesValues()
    {
        var config = new BrokerageConfiguration
        {
            Username = "user",
            Password = "password",
            Account = "account",
            GatewayHost = "localhost",
            GatewayPort = 4001,
            GatewayDirectory = "/ib",
            GatewayVersion = "latest",
            TradingMode = "live",
            AutomaterExportLogs = true,
            DataDirectory = "/data"
        };

        var env = config.ToEnvironmentVariables();
        env.Should().Contain(new KeyValuePair<string, string>("IB_USERNAME", "user"));
        env.Should().Contain(new KeyValuePair<string, string>("IB_PASSWORD", "password"));
        env.Should().Contain(new KeyValuePair<string, string>("GATEWAY_PORT", "4001"));
    }

    [Fact]
    public void BrokerageConfiguration_SerializesAndDeserializes()
    {
        var config = new BrokerageConfiguration
        {
            Username = "user",
            Password = "secret",
            Account = "account",
            DataDirectory = "/data"
        };

        var json = JsonSerializer.Serialize(config);
        var roundTrip = JsonSerializer.Deserialize<BrokerageConfiguration>(json);

        roundTrip.Should().NotBeNull();
        roundTrip!.Username.Should().Be("user");
        roundTrip.Password.Should().Be("secret");
    }

    [Fact]
    public void BarRecord_ToLeanCsvMatchesExpectedFormat()
    {
        var bar = new BarRecord(
            Timestamp: new DateTime(2024, 1, 15, 9, 30, 0, DateTimeKind.Utc),
            Open: 150.5m,
            High: 151m,
            Low: 149.8m,
            Close: 150.9m,
            Volume: 100000,
            SourceFile: "aapl.csv");

        var csv = bar.ToLeanCsv();
        csv.Should().Be("20240115 09:30:00,150.5,151,149.8,150.9,100000");
    }

    [Fact]
    public void BarRecord_FromLeanCsvParsesValues()
    {
        var csv = "20240115 09:30:00,150.25,151.00,149.75,150.80,1000000";
        var record = BarRecord.FromLeanCsv(csv, "file.csv");

        record.Open.Should().Be(150.25m);
        record.Volume.Should().Be(1_000_000);
        record.SourceFile.Should().Be("file.csv");
    }

    [Fact]
    public void LeanDataSnapshot_PaginatesRecords()
    {
        var records = Enumerable.Range(0, 50)
            .Select(i => new BarRecord(DateTime.UtcNow.AddMinutes(i), 1, 1, 1, 1, i, "file"))
            .ToList();

        var snapshot = new LeanDataSnapshot(
            Guid.NewGuid(),
            "AAPL",
            "Minute",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.Today),
            new[] { "file" },
            DateTime.UtcNow,
            records);

        var page = snapshot.Page(2, 10);
        page.Records.Should().HaveCount(10);
        page.Records.First().Volume.Should().Be(10);
    }

    [Fact]
    public void LeanDataSnapshot_ContainsChecksDateRange()
    {
        var snapshot = new LeanDataSnapshot(
            Guid.NewGuid(),
            "AAPL",
            "Minute",
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31),
            Array.Empty<string>(),
            DateTime.UtcNow,
            Array.Empty<BarRecord>());

        snapshot.Contains(new DateOnly(2024, 1, 15)).Should().BeTrue();
        snapshot.Contains(new DateOnly(2023, 12, 31)).Should().BeFalse();
    }
}
