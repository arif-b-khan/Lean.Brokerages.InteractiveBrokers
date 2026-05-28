using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using QuantConnect.InteractiveBrokers.ToolBox.Models;

namespace QuantConnect.InteractiveBrokers.ToolBox.Services;

public interface ILeanDataSnapshotLoader
{
    Task<SnapshotPage> LoadAsync(SnapshotRequest request, CancellationToken cancellationToken = default);
}

public sealed class LeanDataSnapshotLoader : ILeanDataSnapshotLoader
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly OutputLayout _outputLayout;
    private readonly ILogger _logger;

    public LeanDataSnapshotLoader(OutputLayout outputLayout, ILogger logger)
    {
        _outputLayout = outputLayout;
        _logger = logger;
    }

    public async Task<SnapshotPage> LoadAsync(SnapshotRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = request.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(request));
        }

        var downloadRequest = ToDownloadRequest(request);
        var baseDirectory = _outputLayout.GetPath(downloadRequest);

        if (!Directory.Exists(baseDirectory))
        {
            _logger.LogWarning($"Snapshot directory '{baseDirectory}' does not exist. Returning empty snapshot.");
            return CreateEmptyPage(request);
        }

        var allRecords = new List<BarRecord>();
        var sourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in EnumerateExistingFiles(downloadRequest, request, baseDirectory, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = GetRelativePathSafe(request.DataDirectory, candidate.FilePath);
            sourceFiles.Add(relativePath);

            var records = candidate.IsZip
                ? await ReadZipAsync(candidate, request, relativePath, cancellationToken).ConfigureAwait(false)
                : await ReadFileAsync(candidate.FilePath, request, relativePath, cancellationToken).ConfigureAwait(false);

            if (records.Count == 0)
            {
                continue;
            }

            allRecords.AddRange(records);
        }

        allRecords.Sort((left, right) => left.Timestamp.CompareTo(right.Timestamp));

        var snapshot = new LeanDataSnapshot(
            Guid.NewGuid(),
            request.Symbol,
            request.Resolution,
            request.StartDate,
            request.EndDate,
            sourceFiles,
            DateTime.UtcNow,
            allRecords);

        var pagedSnapshot = snapshot.Page(request.PageNumber, request.PageSize);

        return new SnapshotPage(
            pagedSnapshot,
            request.PageNumber,
            request.PageSize,
            snapshot.RecordCount);
    }

    private static SnapshotPage CreateEmptyPage(SnapshotRequest request)
    {
        var emptySnapshot = new LeanDataSnapshot(
            Guid.NewGuid(),
            request.Symbol,
            request.Resolution,
            request.StartDate,
            request.EndDate,
            Array.Empty<string>(),
            DateTime.UtcNow,
            Array.Empty<BarRecord>());

        return new SnapshotPage(emptySnapshot, request.PageNumber, request.PageSize, 0);
    }

    private static DownloadRequest ToDownloadRequest(SnapshotRequest request)
    {
        return new DownloadRequest
        {
            Symbol = request.Symbol,
            SecurityType = request.SecurityType,
            Resolution = request.Resolution,
            DataDir = request.DataDirectory
        };
    }

    private async Task<List<BarRecord>> ReadZipAsync(FileCandidate candidate, SnapshotRequest request, string relativePath, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(candidate.FilePath);

        var entry = TryResolveZipEntry(archive, candidate.EntryName);
        if (entry is null)
        {
            _logger.LogWarning($"Zip entry '{candidate.EntryName}' not found in '{candidate.FilePath}'.");
            return new List<BarRecord>();
        }

        await using var stream = entry.Open();
        return await ReadStreamAsync(stream, request, CombineSource(relativePath, entry.FullName), cancellationToken).ConfigureAwait(false);
    }

    private static ZipArchiveEntry? TryResolveZipEntry(ZipArchive archive, string entryName)
    {
        var direct = archive.GetEntry(entryName);
        if (direct != null)
        {
            return direct;
        }

        return archive.Entries
            .FirstOrDefault(e => string.Equals(e.FullName, entryName, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(Path.GetFileName(e.FullName), entryName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<BarRecord>> ReadFileAsync(string filePath, SnapshotRequest request, string relativePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        return await ReadStreamAsync(stream, request, relativePath, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<BarRecord>> ReadStreamAsync(Stream stream, SnapshotRequest request, string sourceIdentifier, CancellationToken cancellationToken)
    {
        var records = new List<BarRecord>();

        using var reader = new StreamReader(stream, Utf8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false);
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var record = BarRecord.FromLeanCsv(line, sourceIdentifier);
                var recordDate = DateOnly.FromDateTime(record.Timestamp);
                if (recordDate < request.StartDate || recordDate > request.EndDate)
                {
                    continue;
                }

                records.Add(record);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                _logger.LogWarning($"Skipped malformed row in '{sourceIdentifier}': {ex.Message}");
            }
        }

        return records;
    }

    private static string CombineSource(string relativePath, string entryName)
    {
        if (string.IsNullOrEmpty(entryName))
        {
            return relativePath;
        }

        return Path.Combine(relativePath, entryName);
    }

    private static string GetRelativePathSafe(string root, string filePath)
    {
        try
        {
            return Path.GetRelativePath(root, filePath);
        }
        catch (Exception)
        {
            return filePath;
        }
    }

    private IEnumerable<FileCandidate> EnumerateExistingFiles(
        DownloadRequest downloadRequest,
        SnapshotRequest snapshotRequest,
        string baseDirectory,
        CancellationToken cancellationToken)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var date in EnumerateDates(snapshotRequest.StartDate, snapshotRequest.EndDate))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidatePath = Path.Combine(baseDirectory, GetOutputFilename(downloadRequest, date));
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            if (!seen.Add(candidatePath))
            {
                continue;
            }

            var entryName = GetZipEntryName(downloadRequest, date);
            yield return new FileCandidate(candidatePath, entryName, candidatePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        }
    }

    private string GetOutputFilename(DownloadRequest request, DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return _outputLayout.GetFilename(request, dateTime);
    }

    private static string GetZipEntryName(DownloadRequest request, DateOnly date)
    {
        return request.Resolution.ToLowerInvariant() switch
        {
            "minute" or "second" or "tick" or "hour" => $"{date:yyyyMMdd}_trade.csv",
            "daily" => $"{request.Symbol.ToLowerInvariant()}.csv",
            _ => $"{date:yyyyMMdd}.csv"
        };
    }

    private static IEnumerable<DateOnly> EnumerateDates(DateOnly start, DateOnly end)
    {
        var date = start;
        while (date <= end)
        {
            yield return date;
            date = date.AddDays(1);
        }
    }

    private sealed record FileCandidate(string FilePath, string EntryName, bool IsZip);
}