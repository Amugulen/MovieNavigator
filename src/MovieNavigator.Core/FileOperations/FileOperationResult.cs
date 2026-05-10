namespace MovieNavigator.Core.FileOperations;

public sealed record FileOperationResult(bool Succeeded, string? ErrorMessage)
{
    public static FileOperationResult Success() => new(true, null);

    public static FileOperationResult Failure(string errorMessage) => new(false, errorMessage);
}
