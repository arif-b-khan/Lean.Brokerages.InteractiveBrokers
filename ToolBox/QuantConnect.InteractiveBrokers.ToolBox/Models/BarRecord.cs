using System.Globalization;
using QuantConnect.InteractiveBrokers.ToolBox;

namespace QuantConnect.InteractiveBrokers.ToolBox.Models;

/// <summary>
/// Represents a Lean bar entry that can be displayed in the GUI grid.
/// </summary>
public sealed record BarRecord(
    DateTime Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    string SourceFile)
{
    public string ToLeanCsv() => string.Join(",",
        Timestamp.ToString("yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture),
        Open.ToString(CultureInfo.InvariantCulture),
        High.ToString(CultureInfo.InvariantCulture),
        Low.ToString(CultureInfo.InvariantCulture),
        Close.ToString(CultureInfo.InvariantCulture),
        Volume.ToString(CultureInfo.InvariantCulture));

    public static BarRecord FromIBar(IBar bar, string sourceFile)
    {
        ArgumentNullException.ThrowIfNull(bar);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);

        return new BarRecord(bar.Time, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, sourceFile);
    }

    public static BarRecord FromLeanCsv(string csvLine, string sourceFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvLine);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);

        var segments = csvLine.Split(',', StringSplitOptions.TrimEntries);
        if (segments.Length != 6)
        {
            throw new FormatException("Expected 6 CSV columns when parsing LEAN bar record");
        }

        var timestamp = DateTime.ParseExact(segments[0], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        var open = decimal.Parse(segments[1], CultureInfo.InvariantCulture);
        var high = decimal.Parse(segments[2], CultureInfo.InvariantCulture);
        var low = decimal.Parse(segments[3], CultureInfo.InvariantCulture);
        var close = decimal.Parse(segments[4], CultureInfo.InvariantCulture);
        var volume = long.Parse(segments[5], CultureInfo.InvariantCulture);

        return new BarRecord(timestamp, open, high, low, close, volume, sourceFile);
    }
}
