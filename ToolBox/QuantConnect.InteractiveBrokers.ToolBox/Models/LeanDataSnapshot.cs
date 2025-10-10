using System.Collections.Immutable;
using System.Linq;

namespace QuantConnect.InteractiveBrokers.ToolBox.Models;

/// <summary>
/// Represents a paginated snapshot of Lean data ready to be rendered by the GUI.
/// </summary>
public sealed class LeanDataSnapshot
{
    private readonly ImmutableArray<BarRecord> _records;

    public LeanDataSnapshot(
        Guid id,
        string symbol,
        string resolution,
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<string> sourceFiles,
        DateTime loadedAtUtc,
        IEnumerable<BarRecord> records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolution);
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be on or before end date", nameof(startDate));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Symbol = symbol;
        Resolution = resolution;
        StartDate = startDate;
        EndDate = endDate;
        SourceFiles = sourceFiles?.ToArray() ?? Array.Empty<string>();
        LoadedAtUtc = loadedAtUtc == default ? DateTime.UtcNow : loadedAtUtc;
        _records = (records ?? Array.Empty<BarRecord>()).ToImmutableArray();
        RecordCount = _records.Length;
    }

    public Guid Id { get; }

    public string Symbol { get; }

    public string Resolution { get; }

    public DateOnly StartDate { get; }

    public DateOnly EndDate { get; }

    public IReadOnlyList<string> SourceFiles { get; }

    public int RecordCount { get; }

    public DateTime LoadedAtUtc { get; }

    public IReadOnlyList<BarRecord> Records => _records;

    public LeanDataSnapshot Page(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        var skip = (pageNumber - 1) * pageSize;
        var pageRecords = _records.Skip(skip).Take(pageSize).ToImmutableArray();

        return new LeanDataSnapshot(
            Id,
            Symbol,
            Resolution,
            StartDate,
            EndDate,
            SourceFiles,
            LoadedAtUtc,
            pageRecords);
    }

    public bool Contains(DateOnly date)
    {
        return date >= StartDate && date <= EndDate;
    }
}
