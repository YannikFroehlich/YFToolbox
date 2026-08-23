namespace YFToolbox.Features.Files.Rename;

public sealed record RenameOptions(
    string Prefix = "",
    string Suffix = "",
    string Find = "",
    string Replace = "",
    bool AddNumbering = false,
    int StartNumber = 1,
    int Padding = 2,
    bool PreserveExtension = true);

public sealed record RenamePreviewItem(
    string SourcePath,
    string SourceName,
    string TargetPath,
    string TargetName,
    bool IsValid,
    string? Error);

public sealed record RenameResult(
    bool Succeeded,
    IReadOnlyList<RenamePreviewItem> Items,
    IReadOnlyList<string> Errors);

public interface IRenameService
{
    IReadOnlyList<RenamePreviewItem> CreatePreview(IEnumerable<string> paths, RenameOptions options);

    Task<RenameResult> ExecuteAsync(
        IReadOnlyList<RenamePreviewItem> preview,
        CancellationToken cancellationToken = default);
}
