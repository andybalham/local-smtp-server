namespace LocalSmtpCapture.Storage;

/// <summary>
/// Describes the result of a captured email retention pruning operation.
/// </summary>
/// <param name="DeletedFolderPaths">The full paths to captured message folders removed by pruning.</param>
/// <param name="RetainedFolderPaths">The full paths to captured message folders retained after pruning.</param>
public sealed record RetentionPruneResult(
    IReadOnlyList<string> DeletedFolderPaths,
    IReadOnlyList<string> RetainedFolderPaths)
{
    /// <summary>
    /// Gets an empty retention pruning result.
    /// </summary>
    public static RetentionPruneResult Empty { get; } = new([], []);
}
