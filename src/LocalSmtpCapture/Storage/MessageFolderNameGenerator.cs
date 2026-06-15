using System.Globalization;
using System.Text;
using MimeKit;

namespace LocalSmtpCapture.Storage;

/// <summary>
/// Generates default message folder names using the local timestamp, first recipient, subject, and a short unique identifier.
/// </summary>
public sealed class MessageFolderNameGenerator : IMessageFolderNameGenerator
{
    private const int UniqueIdentifierLength = 8;
    private const int RecipientSlugMaximumLength = 48;
    private const int SubjectSlugMaximumLength = 80;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageFolderNameGenerator"/> class.
    /// </summary>
    /// <param name="timeProvider">The time provider used to read the current local time.</param>
    public MessageFolderNameGenerator(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public string GenerateFolderName(MimeMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(utcNow, timeProvider.LocalTimeZone);
        string timestamp = localNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string recipient = CreateSlug(GetFirstRecipient(message), "unknown-recipient", RecipientSlugMaximumLength);
        string subject = CreateSlug(message.Subject, "no-subject", SubjectSlugMaximumLength);
        string uniqueIdentifier = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..UniqueIdentifierLength];

        return $"{timestamp}-{recipient}-{subject}-{uniqueIdentifier}";
    }

    private static string? GetFirstRecipient(MimeMessage message)
    {
        return message.To.Mailboxes
            .Concat(message.Cc.Mailboxes)
            .Concat(message.Bcc.Mailboxes)
            .FirstOrDefault()
            ?.Address;
    }

    private static string CreateSlug(string? value, string fallback, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
        StringBuilder safeValue = new(value.Length);
        bool previousWasSeparator = false;

        foreach (char character in value.Trim())
        {
            bool isSeparator = character == '.'
                || char.IsWhiteSpace(character)
                || invalidFileNameChars.Contains(character);

            if (isSeparator)
            {
                if (!previousWasSeparator)
                {
                    safeValue.Append('-');
                    previousWasSeparator = true;
                }

                continue;
            }

            safeValue.Append(character);
            previousWasSeparator = false;
        }

        string slug = safeValue.ToString().Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            return fallback;
        }

        if (slug.Length <= maximumLength)
        {
            return slug;
        }

        return slug[..maximumLength].TrimEnd('-');
    }
}
