using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.InteractiveBrokers.ToolBox.Models;
using QuantConnect.InteractiveBrokers.ToolBox.Services;
using QuantConnect.InteractiveBrokers.ToolBox.UI.Api;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Gui.ViewModels;

public class MainViewModel
{
    private readonly IGuiApi _api;
    public DownloadViewModel Downloads { get; }
    public SnapshotViewModel Snapshots { get; }
    public ConnectionViewModel Connection { get; }

    public MainViewModel() : this(new DesignTimeApi()) {}

    public MainViewModel(IGuiApi api)
    {
        _api = api;
        Downloads = new DownloadViewModel(_api);
        Snapshots = new SnapshotViewModel(_api);
        Connection = new ConnectionViewModel();
    }
}

internal sealed class DesignTimeApi : IGuiApi
{
    public Task<IReadOnlyList<JobInfo>> GetJobsAsync(CancellationToken ct = default) =>
        Task.FromResult((IReadOnlyList<JobInfo>)new List<JobInfo>());

    public Task<SnapshotPage> LoadSnapshotAsync(SnapshotRequest request, CancellationToken ct = default) =>
        Task.FromResult(new SnapshotPage(new LeanDataSnapshot(
            Guid.NewGuid(), 
            "SPY", 
            "Daily", 
            DateOnly.FromDateTime(DateTime.Today.AddDays(-30)), 
            DateOnly.FromDateTime(DateTime.Today),
            [], 
            DateTime.UtcNow, 
            []), request.PageNumber, request.PageSize, 0));

    public Task<JobInfo> StartDownloadJobAsync(DownloadRequest request, CancellationToken ct = default) =>
        Task.FromResult(new JobInfo(Guid.NewGuid().ToString("N"), request.Symbol, request.Resolution, "Running", DateTime.UtcNow));

    public Task StopDownloadJobAsync(string jobId, CancellationToken ct = default) => Task.CompletedTask;
}
