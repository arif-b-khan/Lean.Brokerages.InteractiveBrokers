using QuantConnect.InteractiveBrokers.ToolBox;
using QuantConnect.InteractiveBrokers.ToolBox.Models;
using QuantConnect.InteractiveBrokers.ToolBox.Services;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Api;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Services;

public class GuiService : IGuiApi
{
    private readonly DownloadJobManager _jobManager;
    private readonly IDataDownloader _downloader;
    private readonly DataWriter _dataWriter;
    private readonly ILeanDataSnapshotLoader _snapshotLoader;
    private readonly ILogger _logger;

    public GuiService(DownloadJobManager jobManager, IDataDownloader downloader, DataWriter dataWriter, ILeanDataSnapshotLoader snapshotLoader, ILogger logger)
    {
        _jobManager = jobManager;
        _downloader = downloader;
        _dataWriter = dataWriter;
        _snapshotLoader = snapshotLoader;
        _logger = logger;
    }

    public async Task<QuantConnect.InteractiveBrokers.ToolBox.Services.JobInfo> StartDownloadJobAsync(DownloadRequest request, CancellationToken ct = default)
    {
        var job = await _jobManager.StartJobAsync(request, ct);

        // Run download in background
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInfo($"[GuiService] Starting download job {job.JobId} for {request.Symbol}");
                var bars = await _downloader.FetchBars(request, ct);
                var result = await _dataWriter.WriteBars(request, bars, ct);

                var status = result.Success ? "Completed" : "Failed";
                var updated = new QuantConnect.InteractiveBrokers.ToolBox.Services.JobInfo(job.JobId, job.Symbol, job.Resolution, status, job.StartTime, DateTime.UtcNow);
                // Update manager state
                await _jobManager.StopJobAsync(job.JobId, ct);
                _jobManager.PublishJobUpdate(updated);
                _logger.LogInfo($"[GuiService] Job {job.JobId} finished with status: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[GuiService] Job {job.JobId} failed: {ex.Message}");
                await _jobManager.StopJobAsync(job.JobId, ct);
                var updated = new QuantConnect.InteractiveBrokers.ToolBox.Services.JobInfo(job.JobId, job.Symbol, job.Resolution, "Failed", job.StartTime, DateTime.UtcNow);
                _jobManager.PublishJobUpdate(updated);
            }
        }, ct);

        return job;
    }

    public Task StopDownloadJobAsync(string jobId, CancellationToken ct = default)
    {
        return _jobManager.StopJobAsync(jobId, ct);
    }

    public Task<IReadOnlyList<QuantConnect.InteractiveBrokers.ToolBox.Services.JobInfo>> GetJobsAsync(CancellationToken ct = default)
    {
        return _jobManager.GetJobsAsync(ct);
    }

    public Task<SnapshotPage> LoadSnapshotAsync(SnapshotRequest request, CancellationToken ct = default)
    {
        _logger.LogInfo($"[GuiService] Loading snapshot for {request.Symbol} ({request.Resolution}) page {request.PageNumber}.");
        return _snapshotLoader.LoadAsync(request, ct);
    }
}
