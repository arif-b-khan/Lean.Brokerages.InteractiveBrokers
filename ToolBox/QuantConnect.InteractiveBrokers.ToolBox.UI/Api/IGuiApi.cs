using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuantConnect.InteractiveBrokers.ToolBox.Models;
using QuantConnect.InteractiveBrokers.ToolBox.Services;

namespace QuantConnect.InteractiveBrokers.ToolBox.UI.Api;

public interface IGuiApi
{
    Task<JobInfo> StartDownloadJobAsync(DownloadRequest request, CancellationToken ct = default);
    Task StopDownloadJobAsync(string jobId, CancellationToken ct = default);
    Task<IReadOnlyList<JobInfo>> GetJobsAsync(CancellationToken ct = default);
    Task<SnapshotPage> LoadSnapshotAsync(SnapshotRequest request, CancellationToken ct = default);
}
