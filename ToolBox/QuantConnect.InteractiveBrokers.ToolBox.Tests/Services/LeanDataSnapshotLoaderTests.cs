using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using QuantConnect.InteractiveBrokers.ToolBox;
using QuantConnect.InteractiveBrokers.ToolBox.Models;
using QuantConnect.InteractiveBrokers.ToolBox.Services;
using Xunit;

namespace QuantConnect.InteractiveBrokers.ToolBox.Tests.Services;

public class LeanDataSnapshotLoaderTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly OutputLayout _layout = new();
    private readonly TestLogger _logger = new();

    public LeanDataSnapshotLoaderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"ib-toolbox-tests-{Guid.NewGuid():N}");
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmptySnapshot_WhenDirectoryMissing()
    {
        var request = new SnapshotRequest
        {
            Symbol = "AAPL",
            Resolution = "minute",
            SecurityType = "equity",
            DataDirectory = Path.Combine(_tempRoot, "data"),
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 1, 2),
            PageNumber = 1,
            PageSize = 100
        };

        var loader = new LeanDataSnapshotLoader(_layout, _logger);

        var page = await loader.LoadAsync(request);

        page.TotalRecords.Should().Be(0);
        page.Snapshot.RecordCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ReadsZippedMinuteData()
    {
        var dataDir = Path.Combine(_tempRoot, "data");
        var request = new SnapshotRequest
        {
            Symbol = "AAPL",
            Resolution = "minute",
            SecurityType = "equity",
            DataDirectory = dataDir,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 1, 1),
            PageNumber = 1,
            PageSize = 100
        };

        var downloadRequest = new DownloadRequest
        {
            Symbol = request.Symbol,
            SecurityType = request.SecurityType,
            Resolution = request.Resolution,
            DataDir = request.DataDirectory
        };

        var targetDirectory = _layout.GetPath(downloadRequest);
        Directory.CreateDirectory(targetDirectory);

        var date = new DateTime(2025, 1, 1);
        var zipFilename = _layout.GetFilename(downloadRequest, date);
        var zipPath = Path.Combine(targetDirectory, zipFilename);
        CreateZip(zipPath, $"{date:yyyyMMdd} 09:30:00,100,101,99,100.5,1234\n");

        var loader = new LeanDataSnapshotLoader(_layout, _logger);

        var page = await loader.LoadAsync(request);

        page.TotalRecords.Should().Be(1);
        page.Snapshot.RecordCount.Should().Be(1);
        page.Snapshot.Records.Single().Volume.Should().Be(1234);
        page.Snapshot.SourceFiles.Should().ContainSingle(f => f.EndsWith(zipFilename, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LoadAsync_PaginatesRecords()
    {
        var dataDir = Path.Combine(_tempRoot, "data");
        var request = new SnapshotRequest
        {
            Symbol = "MSFT",
            Resolution = "minute",
            SecurityType = "equity",
            DataDirectory = dataDir,
            StartDate = new DateOnly(2025, 2, 3),
            EndDate = new DateOnly(2025, 2, 3),
            PageNumber = 2,
            PageSize = 50
        };

        var downloadRequest = new DownloadRequest
        {
            Symbol = request.Symbol,
            SecurityType = request.SecurityType,
            Resolution = request.Resolution,
            DataDir = request.DataDirectory
        };

        var targetDirectory = _layout.GetPath(downloadRequest);
        Directory.CreateDirectory(targetDirectory);

        var date = new DateTime(2025, 2, 3);
        var zipFilename = _layout.GetFilename(downloadRequest, date);
        var zipPath = Path.Combine(targetDirectory, zipFilename);

        var builder = new System.Text.StringBuilder();
        var startTime = new DateTime(2025, 2, 3, 9, 30, 0);
        for (var i = 0; i < 120; i++)
        {
            var time = startTime.AddMinutes(i);
            builder.AppendLine($"{time:yyyyMMdd HH:mm:ss},100,101,99,100,1000");
        }

        CreateZip(zipPath, builder.ToString());

        var loader = new LeanDataSnapshotLoader(_layout, _logger);

        var page = await loader.LoadAsync(request);

        page.TotalRecords.Should().Be(120);
        page.Snapshot.RecordCount.Should().Be(50);
        var firstTimestamp = page.Snapshot.Records.First().Timestamp;
        var lastTimestamp = page.Snapshot.Records.Last().Timestamp;
        firstTimestamp.Should().Be(startTime.AddMinutes(50));
        lastTimestamp.Should().Be(startTime.AddMinutes(99));
    }

    private static void CreateZip(string zipPath, string content)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entryName = Path.GetFileNameWithoutExtension(zipPath) + ".csv";
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    private sealed class TestLogger : ILogger
    {
        public List<string> Messages { get; } = new();

        public void LogTrace(string message, object? context = null) => Messages.Add($"TRACE: {message}");
        public void LogDebug(string message, object? context = null) => Messages.Add($"DEBUG: {message}");
        public void LogInfo(string message, object? context = null) => Messages.Add($"INFO: {message}");
        public void LogWarning(string message, object? context = null) => Messages.Add($"WARN: {message}");
        public void LogError(string message, object? context = null, Exception? exception = null) => Messages.Add($"ERROR: {message}");
    }
}
