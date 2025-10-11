using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.InteractiveBrokers.ToolBox.Services;

public class JobStore
{
    private readonly string _storePath;
    public JobStore(string? storePath = null)
    {
        _storePath = storePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuantConnect", "InteractiveBrokers", "jobs.json");
    }

    public async Task SaveJobsAsync(IEnumerable<JobInfo> jobs, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
        var json = JsonSerializer.Serialize(jobs);
        await File.WriteAllTextAsync(_storePath, json, ct);
    }

    public async Task<IReadOnlyList<JobInfo>> LoadJobsAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_storePath))
            return new List<JobInfo>();
        var json = await File.ReadAllTextAsync(_storePath, ct);
        return JsonSerializer.Deserialize<List<JobInfo>>(json) ?? new List<JobInfo>();
    }
}
