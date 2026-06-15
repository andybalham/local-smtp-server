using MimeKit;

namespace LocalSmtpCapture.Storage;

/// <summary>
/// Persists captured email messages to local filesystem storage.
/// </summary>
public interface IEmailMessagePersistenceService
{
    /// <summary>
    /// Saves a MIME message and its extractable content to a unique message folder.
    /// </summary>
    /// <param name="message">The parsed MIME message to persist.</param>
    /// <param name="cancellationToken">A token that cancels the persistence operation.</param>
    /// <returns>Metadata describing the files written for the message.</returns>
    Task<PersistedMessage> SaveAsync(MimeMessage message, CancellationToken cancellationToken = default);
}
