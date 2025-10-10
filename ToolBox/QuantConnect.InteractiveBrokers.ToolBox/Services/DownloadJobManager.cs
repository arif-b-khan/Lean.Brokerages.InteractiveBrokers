using System.Collections.Concurrent;

namespace QuantConnect.InteractiveBrokers.ToolBox.Services;

public class DownloadJobManager
{
    private readonly ConcurrentDictionary<string, JobInfo> _jobs = new();

    public event Action<JobInfo>? JobUpdated;
    
    /// <summary>
    /// Publish an externally produced job update to subscribers.
    /// This method allows other components to notify listeners without invoking the event directly.
    /// </summary>
    public void PublishJobUpdate(JobInfo info)
    {
        JobUpdated?.Invoke(info);
    }

    public Task<JobInfo> StartJobAsync(DownloadRequest request, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var job = new JobInfo(id, request.Symbol, request.Resolution, "Running", DateTime.UtcNow);
        _jobs[id] = job;
        JobUpdated?.Invoke(job);

        // Fire-and-forget background work that will be driven externally by GuiService
        return Task.FromResult(job);
    }

    public Task StopJobAsync(string jobId, CancellationToken ct = default)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            var updated = job with { Status = "Stopped", EndTime = DateTime.UtcNow };
            _jobs[jobId] = updated;
            PublishJobUpdate(updated);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<JobInfo>> GetJobsAsync(CancellationToken ct = default)
    {
        return Task.FromResult((IReadOnlyList<JobInfo>)_jobs.Values.ToList());
    }
}

// Lightweight JobInfo record reused across projects
public record JobInfo(string JobId, string Symbol, string Resolution, string Status, DateTime StartTime, DateTime? EndTime = null);
