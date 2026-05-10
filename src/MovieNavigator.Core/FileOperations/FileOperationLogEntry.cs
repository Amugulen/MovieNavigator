using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.FileOperations;

public sealed record FileOperationLogEntry(
    Guid Id,
    FileOperationType OperationType,
    DateTimeOffset OccurredAt,
    string SourcePath,
    string TargetPath,
    long? FileSizeBytes,
    Guid? MediaIdBefore,
    Guid? MediaIdAfter,
    MediaLibraryType LibraryType,
    bool Succeeded,
    string? ErrorMessage);
