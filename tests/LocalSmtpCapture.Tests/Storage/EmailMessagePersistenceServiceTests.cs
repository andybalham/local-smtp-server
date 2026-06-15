using LocalSmtpCapture.Configuration;
using LocalSmtpCapture.Storage;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;

namespace LocalSmtpCapture.Tests.Storage;

public sealed class EmailMessagePersistenceServiceTests
{
    [Fact]
    public async Task SaveAsync_PlainTextEmail_SavesRawMessageAndTextBody()
    {
        using TemporaryFolder temporaryFolder = new();
        MimeMessage message = CreatePlainTextMessage("Plain body");
        EmailMessagePersistenceService service = CreateService(temporaryFolder.Path);

        PersistedMessage persistedMessage = await service.SaveAsync(message);

        Assert.True(File.Exists(persistedMessage.RawMessagePath));
        Assert.True(File.Exists(persistedMessage.TextBodyPath));
        Assert.Equal("Plain body", await File.ReadAllTextAsync(persistedMessage.TextBodyPath!));
        Assert.Null(persistedMessage.HtmlBodyPath);
        Assert.Empty(persistedMessage.AttachmentPaths);
        Assert.Equal("message-1", Path.GetFileName(persistedMessage.MessageFolderPath));
    }

    [Fact]
    public async Task SaveAsync_HtmlEmail_SavesRawMessageAndHtmlBody()
    {
        using TemporaryFolder temporaryFolder = new();
        BodyBuilder bodyBuilder = new()
        {
            HtmlBody = "<p>HTML body</p>"
        };
        MimeMessage message = CreateMessage(bodyBuilder.ToMessageBody());
        EmailMessagePersistenceService service = CreateService(temporaryFolder.Path);

        PersistedMessage persistedMessage = await service.SaveAsync(message);

        Assert.True(File.Exists(persistedMessage.RawMessagePath));
        Assert.Null(persistedMessage.TextBodyPath);
        Assert.True(File.Exists(persistedMessage.HtmlBodyPath));
        Assert.Equal("<p>HTML body</p>", await File.ReadAllTextAsync(persistedMessage.HtmlBodyPath!));
    }

    [Fact]
    public async Task SaveAsync_MultipartEmail_SavesTextAndHtmlBodies()
    {
        using TemporaryFolder temporaryFolder = new();
        BodyBuilder bodyBuilder = new()
        {
            TextBody = "Plain body",
            HtmlBody = "<p>HTML body</p>"
        };
        MimeMessage message = CreateMessage(bodyBuilder.ToMessageBody());
        EmailMessagePersistenceService service = CreateService(temporaryFolder.Path);

        PersistedMessage persistedMessage = await service.SaveAsync(message);

        Assert.True(File.Exists(persistedMessage.TextBodyPath));
        Assert.True(File.Exists(persistedMessage.HtmlBodyPath));
        Assert.Equal("Plain body", await File.ReadAllTextAsync(persistedMessage.TextBodyPath!));
        Assert.Equal("<p>HTML body</p>", await File.ReadAllTextAsync(persistedMessage.HtmlBodyPath!));
    }

    [Fact]
    public async Task SaveAsync_AttachmentEmail_SavesAttachmentUnderAttachmentsFolder()
    {
        using TemporaryFolder temporaryFolder = new();
        BodyBuilder bodyBuilder = new()
        {
            TextBody = "See attached."
        };
        bodyBuilder.Attachments.Add("invoice.txt", Encoding.UTF8.GetBytes("Invoice content"));
        MimeMessage message = CreateMessage(bodyBuilder.ToMessageBody());
        EmailMessagePersistenceService service = CreateService(temporaryFolder.Path);

        PersistedMessage persistedMessage = await service.SaveAsync(message);

        string attachmentPath = Assert.Single(persistedMessage.AttachmentPaths);
        Assert.True(File.Exists(attachmentPath));
        Assert.Equal("attachments", Path.GetFileName(Path.GetDirectoryName(attachmentPath)));
        Assert.Equal("invoice.txt", Path.GetFileName(attachmentPath));
        Assert.Equal("Invoice content", await File.ReadAllTextAsync(attachmentPath));
    }

    [Fact]
    public async Task SaveAsync_OutputFolderMissing_CreatesOutputFolder()
    {
        using TemporaryFolder temporaryFolder = new();
        string outputFolder = Path.Combine(temporaryFolder.Path, "missing");
        MimeMessage message = CreatePlainTextMessage("Plain body");
        EmailMessagePersistenceService service = CreateService(outputFolder);

        PersistedMessage persistedMessage = await service.SaveAsync(message);

        Assert.True(Directory.Exists(outputFolder));
        Assert.True(Directory.Exists(persistedMessage.MessageFolderPath));
        Assert.True(File.Exists(persistedMessage.RawMessagePath));
    }

    private static EmailMessagePersistenceService CreateService(string outputFolder)
    {
        LocalSmtpCaptureOptions options = new()
        {
            Storage = new StorageOptions
            {
                OutputFolder = outputFolder
            }
        };

        return new EmailMessagePersistenceService(Options.Create(options), new SequentialFolderNameGenerator());
    }

    private static MimeMessage CreatePlainTextMessage(string textBody)
    {
        BodyBuilder bodyBuilder = new()
        {
            TextBody = textBody
        };

        return CreateMessage(bodyBuilder.ToMessageBody());
    }

    private static MimeMessage CreateMessage(MimeEntity body)
    {
        MimeMessage message = new();
        message.From.Add(MailboxAddress.Parse("sender@example.com"));
        message.To.Add(MailboxAddress.Parse("recipient@example.com"));
        message.Subject = "Test subject";
        message.Body = body;

        return message;
    }

    private sealed class SequentialFolderNameGenerator : IMessageFolderNameGenerator
    {
        private int nextId;

        public string GenerateFolderName(MimeMessage message)
        {
            nextId++;
            return $"message-{nextId}";
        }
    }

    private sealed class TemporaryFolder : IDisposable
    {
        public TemporaryFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"LocalSmtpCapture-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
