using LocalSmtpCapture.Storage;
using MimeKit;

namespace LocalSmtpCapture.Console;

/// <summary>
/// Formats captured email metadata for console output.
/// </summary>
public interface IEmailSummaryFormatter
{
    /// <summary>
    /// Formats a console-safe summary of a received email message.
    /// </summary>
    /// <param name="message">The parsed MIME message.</param>
    /// <param name="persistedMessage">The persisted message metadata.</param>
    /// <param name="receivedAt">The timestamp when the message was received.</param>
    /// <returns>A readable message summary that excludes email body and attachment contents.</returns>
    string Format(MimeMessage message, PersistedMessage persistedMessage, DateTimeOffset receivedAt);
}
