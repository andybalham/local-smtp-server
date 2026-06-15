using LocalSmtpCapture.Configuration;
using LocalSmtpCapture.Storage;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;

namespace LocalSmtpCapture.Console;

/// <summary>
/// Formats per-message console summaries for captured email.
/// </summary>
/// <param name="options">The configured application options.</param>
public sealed class EmailSummaryFormatter(IOptions<LocalSmtpCaptureOptions> options) : IEmailSummaryFormatter
{
    /// <inheritdoc />
    public string Format(MimeMessage message, PersistedMessage persistedMessage, DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(persistedMessage);

        IReadOnlyList<string> recipients = GetRecipients(message);
        IReadOnlyList<string> attachmentNames = persistedMessage.AttachmentPaths
            .Select(Path.GetFileName)
            .Where(static fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(static fileName => fileName!)
            .ToArray();

        StringBuilder summary = new();
        summary.AppendLine($"Received email {receivedAt:yyyy-MM-dd HH:mm:ss zzz}");
        summary.AppendLine($"From: {FormatAddresses(message.From)}");
        summary.AppendLine($"Recipient count: {recipients.Count}");

        if (options.Value.Console.IncludeRecipients)
        {
            summary.AppendLine($"To: {FormatValues(recipients)}");
        }

        summary.AppendLine($"Subject: {message.Subject ?? string.Empty}");
        summary.AppendLine(
            $"Bodies: text={FormatBoolean(persistedMessage.TextBodyPath is not null)} html={FormatBoolean(persistedMessage.HtmlBodyPath is not null)}");
        summary.AppendLine($"Attachments: {attachmentNames.Count}{FormatAttachmentNames(attachmentNames)}");
        summary.Append($"Saved: {persistedMessage.MessageFolderPath}");

        return summary.ToString();
    }

    private static IReadOnlyList<string> GetRecipients(MimeMessage message)
    {
        return message.To
            .Concat(message.Cc)
            .Concat(message.Bcc)
            .Select(FormatAddress)
            .ToArray();
    }

    private static string FormatAddresses(InternetAddressList addresses)
    {
        return addresses.Count == 0 ? "(none)" : string.Join(", ", addresses.Select(FormatAddress));
    }

    private static string FormatAddress(InternetAddress address)
    {
        return address is MailboxAddress mailboxAddress ? mailboxAddress.Address : address.ToString();
    }

    private static string FormatValues(IReadOnlyList<string> values)
    {
        return values.Count == 0 ? "(none)" : string.Join(", ", values);
    }

    private static string FormatAttachmentNames(IReadOnlyList<string> attachmentNames)
    {
        return attachmentNames.Count == 0 ? string.Empty : $" ({string.Join(", ", attachmentNames)})";
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "yes" : "no";
    }
}
