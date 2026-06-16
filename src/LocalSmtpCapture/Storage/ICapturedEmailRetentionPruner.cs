namespace LocalSmtpCapture.Storage;

/// <summary>
/// Prunes captured email folders according to configured retention settings.
/// </summary>
public interface ICapturedEmailRetentionPruner
{
    /// <summary>
    /// Removes captured message folders that exceed the configured retention limit.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the pruning operation.</param>
    /// <returns>A result describing which captured message folders were removed or retained.</returns>
    Task<RetentionPruneResult> PruneAsync(CancellationToken cancellationToken = default);
}
