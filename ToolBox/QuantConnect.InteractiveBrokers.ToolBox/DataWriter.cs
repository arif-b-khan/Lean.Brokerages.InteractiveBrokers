using System.IO.Compression;
using System.Text;

namespace QuantConnect.InteractiveBrokers.ToolBox;

/// <summary>
/// Data writer that outputs historical bars in LEAN-compatible format
/// </summary>
public class DataWriter
{
    private readonly OutputLayout _outputLayout;
    private readonly ILogger _logger;

    public DataWriter(OutputLayout outputLayout, ILogger logger)
    {
        _outputLayout = outputLayout;
        _logger = logger;
    }

    /// <summary>
    /// Write bars to LEAN-compatible files and return list of created files
    /// </summary>
    public async Task<DownloadResult> WriteBars(
        DownloadRequest request,
        IEnumerable<IBar> bars,
        CancellationToken cancellationToken = default)
    {
        var result = new DownloadResult();
        var barsList = bars.ToList();
        
        try
        {
            _logger.LogInfo($"Writing {barsList.Count} bars to LEAN format");
            
            // Group bars by date for file organization
            var barsByDate = GroupBarsByDate(barsList, request.Resolution);
            
            foreach (var dateGroup in barsByDate)
            {
                var files = await WriteBarGroup(request, dateGroup.Key, dateGroup.Value, cancellationToken);
                result.Files.AddRange(files);
            }
            
            result.Success = true;
            _logger.LogInfo($"Successfully wrote {result.Files.Count} files");
            
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.LogError($"Failed to write bars: {ex.Message}");
        }
        
        return result;
    }

    /// <summary>
    /// Write a group of bars for a specific date
    /// </summary>
    private async Task<List<string>> WriteBarGroup(
        DownloadRequest request,
        DateTime date,
        List<IBar> bars,
        CancellationToken cancellationToken)
    {
        var files = new List<string>();
        
        // Get output directory and filename
        var outputDir = _outputLayout.GetPath(request);
        var filename = _outputLayout.GetFilename(request, date);
        var fullPath = Path.Combine(outputDir, filename);
        
        // Ensure output directory exists
        Directory.CreateDirectory(outputDir);
        
        // Generate CSV content
        var csvContent = GenerateCsvContent(bars, request.Resolution);
        
        // Write to temporary file first (atomic write)
        var tempPath = fullPath + ".tmp";
        
        try
        {
            if (filename.EndsWith(".zip"))
            {
                await WriteCompressedCsv(tempPath, csvContent, GetCsvFilename(request, date), cancellationToken);
            }
            else
            {
                await File.WriteAllTextAsync(tempPath, csvContent, Encoding.UTF8, cancellationToken);
            }
            
            // Atomic move from temp to final location
            File.Move(tempPath, fullPath, overwrite: true);
            
            files.Add(Path.GetRelativePath(request.DataDir, fullPath));
            _logger.LogDebug($"Wrote {bars.Count} bars to {fullPath}");
        }
        finally
        {
            // Clean up temp file if it exists
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        
        return files;
    }

    /// <summary>
    /// Group bars by date based on resolution
    /// </summary>
    private static Dictionary<DateTime, List<IBar>> GroupBarsByDate(List<IBar> bars, string resolution)
    {
        return resolution.ToLowerInvariant() switch
        {
            "daily" => bars.GroupBy(b => b.Time.Date).ToDictionary(g => g.Key, g => g.ToList()),
            "minute" or "second" or "tick" or "hour" => 
                bars.GroupBy(b => b.Time.Date).ToDictionary(g => g.Key, g => g.ToList()),
            _ => throw new ArgumentException($"Unsupported resolution for grouping: {resolution}")
        };
    }

    /// <summary>
    /// Generate CSV content from bars
    /// </summary>
    private string GenerateCsvContent(List<IBar> bars, string resolution)
    {
        var csv = new StringBuilder();
        
        foreach (var bar in bars.OrderBy(b => b.Time))
        {
            var line = _outputLayout.SerializeBar(resolution, bar);
            csv.AppendLine(line);
        }
        
        return csv.ToString();
    }

    /// <summary>
    /// Write CSV content to a compressed ZIP file
    /// </summary>
    private static async Task WriteCompressedCsv(
        string zipPath,
        string csvContent,
        string csvFilename,
        CancellationToken cancellationToken)
    {
        using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
        
        var entry = archive.CreateEntry(csvFilename);
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, Encoding.UTF8);
        
        await writer.WriteAsync(csvContent.AsMemory(), cancellationToken);
    }

    /// <summary>
    /// Get the CSV filename inside ZIP archives
    /// </summary>
    private static string GetCsvFilename(DownloadRequest request, DateTime date)
    {
        var resolution = request.Resolution.ToLowerInvariant();
        
        return resolution switch
        {
            "daily" => $"{request.Symbol.ToLowerInvariant()}.csv",
            "minute" or "second" or "tick" or "hour" => $"{date:yyyyMMdd}_trade.csv",
            _ => throw new ArgumentException($"Unsupported resolution: {request.Resolution}")
        };
    }
}

/// <summary>
/// Result of a data writing operation
/// </summary>
public class DownloadResult
{
    public bool Success { get; set; }
    public List<string> Files { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? Error { get; set; }
}