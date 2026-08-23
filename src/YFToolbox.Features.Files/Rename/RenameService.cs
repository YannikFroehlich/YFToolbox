using System.IO;
using System.Globalization;
using System.Text;
using YFToolbox.Core.Localization;

namespace YFToolbox.Features.Files.Rename;

public sealed class RenameService : IRenameService
{
    private static readonly CompositeFormat RollbackFailedMessage =
        CompositeFormat.Parse(AppStrings.RollbackFailedFormat);

    private static readonly HashSet<string> ReservedNames = new(
        [
            "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        ],
        StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RenamePreviewItem> CreatePreview(IEnumerable<string> paths, RenameOptions options)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        var sourcePaths = paths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var candidates = new List<(string Source, string SourceName, string Target, string TargetName, string? Error)>();

        for (var index = 0; index < sourcePaths.Length; index++)
        {
            var source = sourcePaths[index];
            var sourceName = Path.GetFileName(source);
            var directory = Path.GetDirectoryName(source)!;
            var extension = options.PreserveExtension ? Path.GetExtension(source) : string.Empty;
            var name = options.PreserveExtension ? Path.GetFileNameWithoutExtension(source) : sourceName;

            if (!string.IsNullOrEmpty(options.Find))
            {
                name = name.Replace(options.Find, options.Replace, StringComparison.Ordinal);
            }

            var number = options.AddNumbering
                ? (options.StartNumber + index).ToString($"D{Math.Clamp(options.Padding, 1, 10)}")
                : string.Empty;
            var separator = options.AddNumbering && (options.Prefix.Length > 0 || name.Length > 0 || options.Suffix.Length > 0)
                ? " "
                : string.Empty;
            var targetName = $"{options.Prefix}{name}{options.Suffix}{separator}{number}{extension}";
            var target = Path.Combine(directory, targetName);
            var error = ValidateName(targetName, target);
            candidates.Add((source, sourceName, target, targetName, error));
        }

        var duplicateTargets = candidates
            .GroupBy(candidate => candidate.Target, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sourceSet = sourcePaths.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return candidates.Select(candidate =>
        {
            var error = candidate.Error;
            if (duplicateTargets.Contains(candidate.Target))
            {
                error = AppStrings.DuplicateTargetName;
            }
            else if (File.Exists(candidate.Target) &&
                     !sourceSet.Contains(candidate.Target) &&
                     !string.Equals(candidate.Source, candidate.Target, StringComparison.OrdinalIgnoreCase))
            {
                error = AppStrings.TargetExists;
            }

            return new RenamePreviewItem(
                candidate.Source,
                candidate.SourceName,
                candidate.Target,
                candidate.TargetName,
                error is null,
                error);
        }).ToArray();
    }

    public Task<RenameResult> ExecuteAsync(
        IReadOnlyList<RenamePreviewItem> preview,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(preview, cancellationToken), CancellationToken.None);

    private static RenameResult Execute(
        IReadOnlyList<RenamePreviewItem> preview,
        CancellationToken cancellationToken)
    {
        if (preview.Count == 0 || preview.Any(item => !item.IsValid))
        {
            return new RenameResult(false, preview, [AppStrings.AllPreviewRowsValid]);
        }

        var staged = new List<(RenamePreviewItem Item, string TempPath)>();
        var completed = new List<(RenamePreviewItem Item, string TempPath)>();
        var errors = new List<string>();
        try
        {
            foreach (var item in preview.Where(item =>
                         !string.Equals(item.SourcePath, item.TargetPath, StringComparison.OrdinalIgnoreCase)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var temporaryPath = Path.Combine(
                    Path.GetDirectoryName(item.SourcePath)!,
                    $".yftoolbox-{Guid.NewGuid():N}.tmp");
                File.Move(item.SourcePath, temporaryPath);
                staged.Add((item, temporaryPath));
            }

            foreach (var entry in staged)
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(entry.TempPath, entry.Item.TargetPath);
                completed.Add(entry);
            }

            return new RenameResult(true, preview, []);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            errors.Add(exception.Message);
            RollBack(completed, staged, errors);
            return new RenameResult(false, preview, errors);
        }
    }

    private static void RollBack(
        IEnumerable<(RenamePreviewItem Item, string TempPath)> completed,
        IEnumerable<(RenamePreviewItem Item, string TempPath)> staged,
        ICollection<string> errors)
    {
        foreach (var entry in completed.Reverse())
        {
            try
            {
                if (File.Exists(entry.Item.TargetPath) && !File.Exists(entry.Item.SourcePath))
                {
                    File.Move(entry.Item.TargetPath, entry.Item.SourcePath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add(string.Format(
                    CultureInfo.CurrentCulture,
                    RollbackFailedMessage,
                    entry.Item.SourceName,
                    exception.Message));
            }
        }

        foreach (var entry in staged.Reverse())
        {
            try
            {
                if (File.Exists(entry.TempPath) && !File.Exists(entry.Item.SourcePath))
                {
                    File.Move(entry.TempPath, entry.Item.SourcePath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add(string.Format(
                    CultureInfo.CurrentCulture,
                    RollbackFailedMessage,
                    entry.Item.SourceName,
                    exception.Message));
            }
        }
    }

    private static string? ValidateName(string fileName, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return AppStrings.TargetNameEmpty;
        }

        if (fileName.EndsWith(' ') || fileName.EndsWith('.'))
        {
            return AppStrings.TargetNameEnding;
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return AppStrings.InvalidTargetCharacters;
        }

        if (ReservedNames.Contains(Path.GetFileNameWithoutExtension(fileName)))
        {
            return AppStrings.ReservedTargetName;
        }

        if (fileName.Length > 255 || fullPath.Length > 32_767)
        {
            return AppStrings.TargetPathTooLong;
        }

        return null;
    }
}
