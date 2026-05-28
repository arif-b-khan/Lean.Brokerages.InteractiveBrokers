using System.Collections.Concurrent;

namespace QuantConnect.InteractiveBrokers.ToolBox.Services;

using QuantConnect.InteractiveBrokers.ToolBox.Services;

public class DownloadJobManager
{
    private readonly ConcurrentDictionary<string, JobInfo> _jobs = new();
    private readonly JobStore _jobStore;

    public event Action<JobInfo>? JobUpdated;

    public DownloadJobManager(JobStore? jobStore = null)
    {
        _jobStore = jobStore ?? new JobStore();
        _ = RestoreJobsAsync();
    }

    public void PublishJobUpdate(JobInfo info)
    {
        JobUpdated?.Invoke(info);
        _ = PersistJobsAsync();
    }

    public async Task<JobInfo> StartJobAsync(DownloadRequest request, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var job = new JobInfo(id, request.Symbol, request.Resolution, "Running", DateTime.UtcNow);
        _jobs[id] = job;
        JobUpdated?.Invoke(job);
        await PersistJobsAsync();
        return job;
    }

    public async Task StopJobAsync(string jobId, CancellationToken ct = default)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            var updated = job with { Status = "Stopped", EndTime = DateTime.UtcNow };
            _jobs[jobId] = updated;
            PublishJobUpdate(updated);
            await PersistJobsAsync();
        }
    }

    public Task<IReadOnlyList<JobInfo>> GetJobsAsync(CancellationToken ct = default)
    {
        return Task.FromResult((IReadOnlyList<JobInfo>)_jobs.Values.ToList());
    }

    private async Task PersistJobsAsync()
    {
        await _jobStore.SaveJobsAsync(_jobs.Values, CancellationToken.None);
    }

    private async Task RestoreJobsAsync()
    {
        var jobs = await _jobStore.LoadJobsAsync(CancellationToken.None);
        foreach (var job in jobs)
        {
            _jobs[job.JobId] = job;
            JobUpdated?.Invoke(job);
        }
    }
}

// Lightweight JobInfo record reused across projects
public record JobInfo(string JobId, string Symbol, string Resolution, string Status, DateTime StartTime, DateTime? EndTime = null);
