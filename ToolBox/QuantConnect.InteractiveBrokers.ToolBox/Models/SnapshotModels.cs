using System;
using System.Collections.Generic;

#nullable enable

namespace QuantConnect.InteractiveBrokers.ToolBox.Models;

/// <summary>
/// Represents the user request to load Lean-formatted data from disk.
/// </summary>
public sealed class SnapshotRequest
{
    private static readonly HashSet<string> SupportedResolutions = new(StringComparer.OrdinalIgnoreCase)
    {
        "tick",
        "second",
        "minute",
        "hour",
        "daily"
    };

    public string Symbol { get; set; } = string.Empty;

    public string Resolution { get; set; } = "minute";

    public string SecurityType { get; set; } = "equity";

    public string DataDirectory { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 100;

    /// <summary>
    /// Validates the request and returns a list of errors. Empty when valid.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Symbol))
        {
            errors.Add("Symbol is required.");
        }

        if (string.IsNullOrWhiteSpace(Resolution))
        {
            errors.Add("Resolution is required.");
        }
        else if (!SupportedResolutions.Contains(Resolution))
        {
            errors.Add($"Resolution '{Resolution}' is not supported. Supported values: {string.Join(", ", SupportedResolutions)}.");
        }

        if (string.IsNullOrWhiteSpace(SecurityType))
        {
            errors.Add("SecurityType is required.");
        }

        if (string.IsNullOrWhiteSpace(DataDirectory))
        {
            errors.Add("DataDirectory is required.");
        }

        if (StartDate == default)
        {
            errors.Add("StartDate must be specified.");
        }

        if (EndDate == default)
        {
            errors.Add("EndDate must be specified.");
        }

        if (StartDate > EndDate)
        {
            errors.Add("StartDate must be on or before EndDate.");
        }

        if (PageNumber < 1)
        {
            errors.Add("PageNumber must be at least 1.");
        }

        if (PageSize < 1)
        {
            errors.Add("PageSize must be at least 1.");
        }

        return errors;
    }
}

/// <summary>
/// Represents a paginated snapshot of Lean data returned to the GUI.
/// </summary>
public sealed record SnapshotPage(
    LeanDataSnapshot Snapshot,
    int PageNumber,
    int PageSize,
    int TotalRecords)
{
    public int TotalPages => TotalRecords == 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
}
