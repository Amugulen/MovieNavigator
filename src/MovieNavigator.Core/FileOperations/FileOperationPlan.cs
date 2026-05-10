using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.FileOperations;

public sealed record FileOperationPlan(
    FileOperationType OperationType,
    MediaLibraryType SourceLibrary,
    MediaLibraryType TargetLibrary,
    string SourcePath,
    string TargetPath,
    bool RequiresConfirmation,
    bool RequiresVisibilityBoundaryConfirmation,
    bool CanExecute,
    string? BlockReason)
{
    public static FileOperationPlan Create(FileOperationType operationType, MediaLibraryType library, string sourcePath, string targetPath, bool sourceIsOnline)
    {
        return CreateLibraryTransfer(library, library, sourcePath, targetPath, sourceIsOnline, operationType);
    }

    public static FileOperationPlan CreateLibraryTransfer(
        MediaLibraryType sourceLibrary,
        MediaLibraryType targetLibrary,
        string sourcePath,
        string targetPath,
        bool sourceIsOnline,
        FileOperationType operationType = FileOperationType.Move)
    {
        var crossesVisibilityBoundary = sourceLibrary != targetLibrary;
        return new FileOperationPlan(
            operationType,
            sourceLibrary,
            targetLibrary,
            sourcePath,
            targetPath,
            RequiresConfirmation: true,
            RequiresVisibilityBoundaryConfirmation: crossesVisibilityBoundary,
            CanExecute: sourceIsOnline,
            BlockReason: sourceIsOnline ? null : "Source file is offline.");
    }
}
