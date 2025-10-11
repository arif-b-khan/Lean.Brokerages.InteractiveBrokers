using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QuantConnect.InteractiveBrokers.ToolBox;
using QuantConnect.InteractiveBrokers.ToolBox.Models;
using QuantConnect.InteractiveBrokers.ToolBox.Services;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

public class GuiServiceIntegrationTests
{
    [Fact]
    public async Task GuiService_EndToEnd_ShouldWriteFilesAndLoadSnapshots()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ib-gui-int-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var logger = new StructuredLogger("debug", Guid.NewGuid().ToString("N")[..8]);
            var outputLayout = new OutputLayout();
            var dataWriter = new DataWriter(outputLayout, logger);
            var backoff = new BackoffPolicy();
            var downloader = new InteractiveBrokersDownloader(new Dictionary<string, string>(), backoff, logger);
            var jobManager = new DownloadJobManager();
            var snapshotLoader = new LeanDataSnapshotLoader(outputLayout, logger);

            var service = (QuantConnect.InteractiveBrokers.ToolBox.UI.Services.GuiService)Activator.CreateInstance(
                typeof(QuantConnect.InteractiveBrokers.ToolBox.UI.Services.GuiService),
                jobManager,
                downloader,
                dataWriter,
                snapshotLoader,
                logger)!;

            var startDate = DateTime.UtcNow.Date.AddDays(-3);
            var endDate = DateTime.UtcNow.Date.AddDays(-1);

            var request = new DownloadRequest
            {
                Symbol = "SPY",
                SecurityType = "equity",
                Resolution = "daily",
                From = startDate,
                To = endDate,
                DataDir = tempRoot
            };

            var job = await service.StartDownloadJobAsync(request);
            job.Should().NotBeNull();
            job.Status.Should().Be("Running");

            var completedUpdate = await WaitForJobStatusAsync(jobManager, job.JobId, "Completed", TimeSpan.FromSeconds(10));
            completedUpdate.Status.Should().Be("Completed");
            completedUpdate.EndTime.Should().NotBeNull();

            var persistedJobs = await service.GetJobsAsync();
            persistedJobs.Should().Contain(j => j.JobId == job.JobId);

            var zipFiles = Directory.GetFiles(tempRoot, "*.zip", SearchOption.AllDirectories);
            zipFiles.Should().NotBeEmpty();

            var startDateOnly = DateOnly.FromDateTime(startDate);
            var endDateOnly = DateOnly.FromDateTime(endDate);

            var snapshotRequest = new SnapshotRequest
            {
                Symbol = request.Symbol,
                Resolution = request.Resolution,
                SecurityType = request.SecurityType,
                DataDirectory = tempRoot,
                StartDate = startDateOnly,
                EndDate = endDateOnly,
                PageNumber = 1,
                PageSize = 2
            };

            var firstPage = await service.LoadSnapshotAsync(snapshotRequest);

            firstPage.TotalRecords.Should().BeGreaterOrEqualTo(3);
            firstPage.PageNumber.Should().Be(1);
            firstPage.PageSize.Should().Be(2);
            firstPage.TotalPages.Should().BeGreaterOrEqualTo(2);
            firstPage.Snapshot.Records.Should().HaveCount(2);
            firstPage.Snapshot.SourceFiles.Should().NotBeEmpty();
            firstPage.Snapshot.Records.Should().OnlyContain(r =>
                DateOnly.FromDateTime(r.Timestamp) >= startDateOnly &&
                DateOnly.FromDateTime(r.Timestamp) <= endDateOnly);

            var secondPageRequest = new SnapshotRequest
            {
                Symbol = snapshotRequest.Symbol,
                Resolution = snapshotRequest.Resolution,
                SecurityType = snapshotRequest.SecurityType,
                DataDirectory = snapshotRequest.DataDirectory,
                StartDate = snapshotRequest.StartDate,
                EndDate = snapshotRequest.EndDate,
                PageNumber = 2,
                PageSize = snapshotRequest.PageSize
            };
            var secondPage = await service.LoadSnapshotAsync(secondPageRequest);

            var remainingRecordCount = Math.Max(0, firstPage.TotalRecords - firstPage.PageSize);
            var expectedSecondPageCount = Math.Min(firstPage.PageSize, remainingRecordCount);

            secondPage.PageNumber.Should().Be(2);
            secondPage.PageSize.Should().Be(2);
            secondPage.Snapshot.Records.Should().HaveCount(expectedSecondPageCount);
            secondPage.Snapshot.Records.Should().OnlyContain(r =>
                DateOnly.FromDateTime(r.Timestamp) >= startDateOnly &&
                DateOnly.FromDateTime(r.Timestamp) <= endDateOnly);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static async Task<JobInfo> WaitForJobStatusAsync(
        DownloadJobManager jobManager,
        string jobId,
        string expectedStatus,
        TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<JobInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(JobInfo info)
        {
            if (!string.Equals(info.JobId, jobId, StringComparison.Ordinal))
            {
                return;
            }

            if (string.Equals(info.Status, expectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                jobManager.JobUpdated -= Handler;
                tcs.TrySetResult(info);
            }
        }

        jobManager.JobUpdated += Handler;

        try
        {
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            if (completedTask != tcs.Task)
            {
                throw new TimeoutException($"Job '{jobId}' did not reach status '{expectedStatus}' within {timeout}.");
            }

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            jobManager.JobUpdated -= Handler;
        }
    }
}
