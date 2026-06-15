using LocalSmtpCapture.Configuration;
using LocalSmtpCapture.Console;
using LocalSmtpCapture.Storage;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;

namespace LocalSmtpCapture.Tests.Console;

public sealed class EmailSummaryFormatterTests
{
    [Fact]
    public void Format_IncludesMessageMetadataAndSavedPath()
    {
        MimeMessage message = CreateMessage();
        PersistedMessage persistedMessage = new(
            @"C:\emails\20260615-103000-abc123",
            @"C:\emails\20260615-103000-abc123\message.eml",
            @"C:\emails\20260615-103000-abc123\body.txt",
            @"C:\emails\20260615-103000-abc123\body.html",
            [@"C:\emails\20260615-103000-abc123\attachments\invoice.pdf"]);
        EmailSummaryFormatter formatter = CreateFormatter(includeRecipients: true);

        string summary = formatter.Format(message, persistedMessage, new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.FromHours(1)));

        Assert.Contains("Received email 2026-06-15 10:30:00 +01:00", summary);
        Assert.Contains("From: sender@example.com", summary);
        Assert.Contains("Recipient count: 3", summary);
        Assert.Contains("To: recipient@example.com, copy@example.com, blind@example.com", summary);
        Assert.Contains("Subject: Invoice test", summary);
        Assert.Contains("Bodies: text=yes html=yes", summary);
        Assert.Contains("Attachments: 1 (invoice.pdf)", summary);
        Assert.Contains(@"Saved: C:\emails\20260615-103000-abc123", summary);
    }

    [Fact]
    public void Format_ExcludesBodyAndAttachmentContent()
    {
        MimeMessage message = CreateMessage();
        PersistedMessage persistedMessage = new(
            @"C:\emails\message-1",
            @"C:\emails\message-1\message.eml",
            @"C:\emails\message-1\body.txt",
            null,
            [@"C:\emails\message-1\attachments\invoice.pdf"]);
        EmailSummaryFormatter formatter = CreateFormatter(includeRecipients: true);

        string summary = formatter.Format(message, persistedMessage, DateTimeOffset.UnixEpoch);

        Assert.DoesNotContain("Do not print this body.", summary);
        Assert.DoesNotContain("Attachment content", summary);
        Assert.Contains("Bodies: text=yes html=no", summary);
    }

    [Fact]
    public void Format_IncludeRecipientsFalse_HidesRecipientDetails()
    {
        MimeMessage message = CreateMessage();
        PersistedMessage persistedMessage = new(
            @"C:\emails\message-1",
            @"C:\emails\message-1\message.eml",
            null,
            null,
            []);
        EmailSummaryFormatter formatter = CreateFormatter(includeRecipients: false);

        string summary = formatter.Format(message, persistedMessage, DateTimeOffset.UnixEpoch);

        Assert.Contains("Recipient count: 3", summary);
        Assert.DoesNotContain("To:", summary);
        Assert.DoesNotContain("recipient@example.com", summary);
        Assert.Contains("Attachments: 0", summary);
    }

    private static EmailSummaryFormatter CreateFormatter(bool includeRecipients)
    {
        LocalSmtpCaptureOptions options = new()
        {
            Console = new ConsoleOptions
            {
                IncludeRecipients = includeRecipients
            }
        };

        return new EmailSummaryFormatter(Options.Create(options));
    }

    private static MimeMessage CreateMessage()
    {
        BodyBuilder bodyBuilder = new()
        {
            TextBody = "Do not print this body.",
            HtmlBody = "<p>Do not print this body.</p>"
        };
        bodyBuilder.Attachments.Add("invoice.pdf", Encoding.UTF8.GetBytes("Attachment content"));

        MimeMessage message = new()
        {
            Subject = "Invoice test",
            Body = bodyBuilder.ToMessageBody()
        };
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Recipient", "recipient@example.com"));
        message.Cc.Add(new MailboxAddress("Copy", "copy@example.com"));
        message.Bcc.Add(new MailboxAddress("Blind", "blind@example.com"));

        return message;
    }
}
