using MimeKit;

namespace LocalSmtpCapture.Storage;

/// <summary>
/// Generates filesystem-safe folder names for captured email messages.
/// </summary>
public interface IMessageFolderNameGenerator
{
    /// <summary>
    /// Generates a unique folder name for a captured email message.
    /// </summary>
    /// <param name="message">The captured email message to use when generating the folder name.</param>
    /// <returns>A filesystem-safe folder name containing a local timestamp and short unique identifier.</returns>
    string GenerateFolderName(MimeMessage message);
}
