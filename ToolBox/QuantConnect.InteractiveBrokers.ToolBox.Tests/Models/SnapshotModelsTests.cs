using FluentAssertions;
using QuantConnect.InteractiveBrokers.ToolBox.Models;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests.Models;

public class SnapshotModelsTests
{
    [Fact]
    public void SnapshotRequest_ValidateReturnsErrorsForMissingFields()
    {
        var request = new SnapshotRequest
        {
            Symbol = "",
            Resolution = "",
            SecurityType = "",
            DataDirectory = "",
            StartDate = default,
            EndDate = default,
            PageNumber = 0,
            PageSize = 0
        };

        var errors = request.Validate();

        errors.Should().Contain("Symbol is required.");
        errors.Should().Contain("Resolution is required.");
        errors.Should().Contain("SecurityType is required.");
        errors.Should().Contain("DataDirectory is required.");
        errors.Should().Contain("StartDate must be specified.");
        errors.Should().Contain("EndDate must be specified.");
        errors.Should().Contain("PageNumber must be at least 1.");
        errors.Should().Contain("PageSize must be at least 1.");
    }

    [Fact]
    public void SnapshotRequest_ValidateRejectsUnsupportedResolution()
    {
        var request = new SnapshotRequest
        {
            Symbol = "AAPL",
            Resolution = "weekly",
            SecurityType = "equity",
            DataDirectory = "/tmp",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 31),
            PageNumber = 1,
            PageSize = 100
        };

        var errors = request.Validate();

        errors.Should().Contain(e => e.Contains("Resolution 'weekly' is not supported"));
    }

    [Fact]
    public void SnapshotRequest_ValidateRejectsStartAfterEnd()
    {
        var request = new SnapshotRequest
        {
            Symbol = "AAPL",
            Resolution = "minute",
            SecurityType = "equity",
            DataDirectory = "/tmp",
            StartDate = new DateOnly(2024, 2, 1),
            EndDate = new DateOnly(2024, 1, 1),
            PageNumber = 1,
            PageSize = 100
        };

        var errors = request.Validate();

        errors.Should().Contain("StartDate must be on or before EndDate.");
    }

    [Fact]
    public void SnapshotRequest_ValidateReturnsEmptyForValidRequest()
    {
        var request = new SnapshotRequest
        {
            Symbol = "AAPL",
            Resolution = "minute",
            SecurityType = "equity",
            DataDirectory = "/tmp",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 31),
            PageNumber = 1,
            PageSize = 100
        };

        var errors = request.Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void SnapshotPage_TotalPagesCalculatedCorrectly()
    {
        var snapshot = new LeanDataSnapshot(
            Guid.NewGuid(),
            "AAPL",
            "minute",
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 2),
            Array.Empty<string>(),
            DateTime.UtcNow,
            Array.Empty<BarRecord>());

        var page = new SnapshotPage(snapshot, 1, 100, 250);
        page.TotalPages.Should().Be(3);
    }
}
