using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuantConnect.InteractiveBrokers.ToolBox.Services;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests;

public class JobStoreTests
{
    [Fact]
    public async Task SaveAndLoadJobs_RoundTrip_Works()
    {
        var store = new JobStore("./test-jobs.json");
        var jobs = new List<JobInfo>
        {
            new JobInfo("id1", "SPY", "daily", "Completed", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
            new JobInfo("id2", "AAPL", "minute", "Running", DateTime.UtcNow, null)
        };
        await store.SaveJobsAsync(jobs);
        var loaded = await store.LoadJobsAsync();
        Assert.Equal(2, loaded.Count);
        Assert.Contains(loaded, j => j.JobId == "id1" && j.Symbol == "SPY");
        Assert.Contains(loaded, j => j.JobId == "id2" && j.Symbol == "AAPL");
    }
}
