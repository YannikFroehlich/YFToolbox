namespace YFToolbox.Core.Models;

public sealed record HistoryEntry(
    string ToolId,
    DateTimeOffset Timestamp,
    string Result,
    int FileCount,
    string Preset,
    long DurationMilliseconds);
