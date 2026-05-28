using System;
using FluentAssertions;
using Xunit;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Services;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Api;
using QuantConnect.InteractiveBrokers.ToolBox.Models;
using QuantConnect.InteractiveBrokers.ToolBox.Services;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

public class GuiServiceTests
{
    [Fact]
    public async Task StartDownloadJob_ShouldCreateJobAndComplete()
    {
        var request = new DownloadRequest
        {
            Symbol = "TEST",
            Resolution = "daily",
            From = DateTime.UtcNow.Date.AddDays(-2),
            To = DateTime.UtcNow.Date,
            DataDir = Path.GetTempPath()
        };

        var logger = new StructuredLogger("debug", Guid.NewGuid().ToString("N")[..8]);
        var outputLayout = new OutputLayout();
        var dataWriter = new DataWriter(outputLayout, logger);

        // Fake downloader that returns a small set of bars
        var downloader = new FakeDownloader();
        var jobManager = new DownloadJobManager();
        var snapshotLoader = new FakeSnapshotLoader();

        var service = (QuantConnect.InteractiveBrokers.ToolBox.UI.Services.GuiService)Activator.CreateInstance(
            typeof(QuantConnect.InteractiveBrokers.ToolBox.UI.Services.GuiService),
            jobManager,
            downloader,
            dataWriter,
            snapshotLoader,
            logger)!;

        var job = await service.StartDownloadJobAsync(request);

        Assert.NotNull(job);
        Assert.Equal("Running", job.Status);

        // Wait a short time for background work to complete in this simple test
        await Task.Delay(500);

        var jobs = await service.GetJobsAsync();
        Assert.Contains(jobs, j => j.JobId == job.JobId);
    }

    [Fact]
    public async Task LoadSnapshotAsync_ShouldDelegateToLoader()
    {
        var request = new SnapshotRequest
        {
            Symbol = "TEST",
            Resolution = "minute",
            SecurityType = "equity",
            DataDirectory = "/tmp",
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 1, 2),
            PageNumber = 2,
            PageSize = 50
        };

        var logger = new StructuredLogger("info", Guid.NewGuid().ToString("N")[..8]);
        var output = new OutputLayout();
        var dataWriter = new DataWriter(output, logger);
        var downloader = new FakeDownloader();
        var jobManager = new DownloadJobManager();
        var snapshotLoader = new FakeSnapshotLoader();

        var service = (QuantConnect.InteractiveBrokers.ToolBox.UI.Services.GuiService)Activator.CreateInstance(
            typeof(QuantConnect.InteractiveBrokers.ToolBox.UI.Services.GuiService),
            jobManager,
            downloader,
            dataWriter,
            snapshotLoader,
            logger)!;

    dynamic api = service;
    SnapshotPage page = await api.LoadSnapshotAsync(request);

        snapshotLoader.LastRequest.Should().Be(request);
        page.Should().BeSameAs(snapshotLoader.ResultToReturn);
    }

    private class FakeDownloader : InteractiveBrokersDownloader
    {
        public FakeDownloader() : base(new Dictionary<string,string>(), new BackoffPolicy(), new StructuredLogger("info", Guid.NewGuid().ToString("N")[..8])) { }

        // Shadow the non-virtual FetchBars with a test implementation
        public new Task<IEnumerable<IBar>> FetchBars(DownloadRequest request, CancellationToken cancellationToken = default)
        {
            var bars = new List<IBar>
            {
                new Bar(request.From.Date, 100, 101, 99, 100.5m, 1000)
            };

            return Task.FromResult<IEnumerable<IBar>>(bars);
        }
    }

    private class FakeSnapshotLoader : ILeanDataSnapshotLoader
    {
        public SnapshotRequest? LastRequest { get; private set; }

        public SnapshotPage ResultToReturn { get; } = new SnapshotPage(
            new LeanDataSnapshot(Guid.NewGuid(), "TEST", "minute", new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2), Array.Empty<string>(), DateTime.UtcNow, Array.Empty<BarRecord>()),
            1,
            100,
            0);

        public Task<SnapshotPage> LoadAsync(SnapshotRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(ResultToReturn);
        }
    }
}
