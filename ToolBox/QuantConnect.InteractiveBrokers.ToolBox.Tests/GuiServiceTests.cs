using Xunit;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Services;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Api;

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
        var jobManager = new QuantConnect.InteractiveBrokers.ToolBox.Services.DownloadJobManager();

        var service = new GuiService(jobManager, downloader, dataWriter, logger);

        var job = await service.StartDownloadJobAsync(request);

        Assert.NotNull(job);
        Assert.Equal("Running", job.Status);

        // Wait a short time for background work to complete in this simple test
        await Task.Delay(500);

        var jobs = await service.GetJobsAsync();
        Assert.Contains(jobs, j => j.JobId == job.JobId);
    }

    private class FakeDownloader : InteractiveBrokersDownloader
    {
        public FakeDownloader() : base(new Dictionary<string,string>(), new BackoffPolicy(), new StructuredLogger("info", Guid.NewGuid().ToString("N")[..8])) { }

        public override Task<IEnumerable<IBar>> FetchBars(DownloadRequest request, CancellationToken cancellationToken = default)
        {
            var bars = new List<IBar>
            {
                new Bar(request.From.Date, 100, 101, 99, 100.5m, 1000)
            };

            return Task.FromResult<IEnumerable<IBar>>(bars);
        }
    }
}
