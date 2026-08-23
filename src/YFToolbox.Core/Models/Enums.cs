namespace YFToolbox.Core.Models;

public enum FileCategory
{
    Unknown,
    Image,
    Audio,
    Video,
    Pdf,
    Archive,
    Document
}

public enum DetectionConfidence
{
    Unknown,
    ExtensionOnly,
    Signature,
    DecoderVerified
}

public enum ProcessingStatus
{
    Queued,
    Inspecting,
    Running,
    Finalizing,
    Succeeded,
    Failed,
    Cancelled,
    Skipped
}

public enum OutputConflictPolicy
{
    CreateUnique,
    Skip,
    Ask,
    Overwrite
}

public enum OutputMode
{
    CentralFolder,
    SourceFolder
}

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public enum LanguagePreference
{
    System,
    English,
    German
}

public enum QualityPreset
{
    Small,
    Balanced,
    High
}

public enum AppErrorCode
{
    InvalidInput,
    UnsupportedFormat,
    CorruptInput,
    InputChanged,
    FileLocked,
    AccessDenied,
    NameConflict,
    InsufficientDiskSpace,
    ExternalToolMissing,
    ExternalToolFailed,
    Cancelled,
    InternalError
}
