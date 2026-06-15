using LocalSmtpCapture.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LocalSmtpCapture.Storage;

/// <summary>
/// Persists captured MIME messages to the configured output folder.
/// </summary>
/// <param name="options">The configured application options.</param>
/// <param name="folderNameGenerator">The service used to create unique message folder names.</param>
public sealed class EmailMessagePersistenceService(
    IOptions<LocalSmtpCaptureOptions> options,
    IMessageFolderNameGenerator folderNameGenerator)
    : IEmailMessagePersistenceService
{
    private const string RawMessageFileName = "message.eml";
    private const string TextBodyFileName = "body.txt";
    private const string HtmlBodyFileName = "body.html";
    private const string AttachmentsFolderName = "attachments";

    /// <inheritdoc />
    public async Task<PersistedMessage> SaveAsync(MimeMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        string outputFolder = options.Value.Storage.OutputFolder;
        Directory.CreateDirectory(outputFolder);

        string messageFolderPath = CreateUniqueMessageFolder(outputFolder, message);
        string rawMessagePath = Path.Combine(messageFolderPath, RawMessageFileName);

        await using (FileStream rawMessageStream = File.Create(rawMessagePath))
        {
            await message.WriteToAsync(rawMessageStream, cancellationToken).ConfigureAwait(false);
        }

        string? textBodyPath = null;
        if (message.TextBody is not null)
        {
            textBodyPath = Path.Combine(messageFolderPath, TextBodyFileName);
            await File.WriteAllTextAsync(textBodyPath, message.TextBody, cancellationToken).ConfigureAwait(false);
        }

        string? htmlBodyPath = null;
        if (message.HtmlBody is not null)
        {
            htmlBodyPath = Path.Combine(messageFolderPath, HtmlBodyFileName);
            await File.WriteAllTextAsync(htmlBodyPath, message.HtmlBody, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<string> attachmentPaths = await SaveAttachmentsAsync(
            message,
            messageFolderPath,
            cancellationToken).ConfigureAwait(false);

        return new PersistedMessage(
            messageFolderPath,
            rawMessagePath,
            textBodyPath,
            htmlBodyPath,
            attachmentPaths);
    }

    private string CreateUniqueMessageFolder(string outputFolder, MimeMessage message)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            string messageFolderPath = Path.Combine(outputFolder, folderNameGenerator.GenerateFolderName(message));

            if (Directory.Exists(messageFolderPath))
            {
                continue;
            }

            Directory.CreateDirectory(messageFolderPath);
            return messageFolderPath;
        }

        throw new IOException("Could not create a unique message folder after 10 attempts.");
    }

    private static async Task<IReadOnlyList<string>> SaveAttachmentsAsync(
        MimeMessage message,
        string messageFolderPath,
        CancellationToken cancellationToken)
    {
        List<string> attachmentPaths = [];
        int attachmentIndex = 0;

        foreach (MimeEntity attachment in message.Attachments)
        {
            attachmentIndex++;
            string attachmentsFolderPath = Path.Combine(messageFolderPath, AttachmentsFolderName);
            Directory.CreateDirectory(attachmentsFolderPath);

            string attachmentFileName = GetSafeAttachmentFileName(attachment, attachmentIndex);
            string attachmentPath = CreateUniqueAttachmentPath(attachmentsFolderPath, attachmentFileName);

            await using FileStream attachmentStream = File.Create(attachmentPath);

            if (attachment is MessagePart { Message: not null } messagePart)
            {
                await messagePart.Message.WriteToAsync(attachmentStream, cancellationToken).ConfigureAwait(false);
            }
            else if (attachment is MimePart { Content: not null } mimePart)
            {
                await mimePart.Content.DecodeToAsync(attachmentStream, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await attachment.WriteToAsync(attachmentStream, cancellationToken).ConfigureAwait(false);
            }

            attachmentPaths.Add(attachmentPath);
        }

        return attachmentPaths;
    }

    private static string GetSafeAttachmentFileName(MimeEntity attachment, int attachmentIndex)
    {
        string? fileName = attachment.ContentDisposition?.FileName ?? attachment.ContentType.Name;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"attachment-{attachmentIndex}";
        }

        fileName = Path.GetFileName(fileName);
        char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
        string safeFileName = string.Concat(fileName.Select(character =>
            invalidFileNameChars.Contains(character) ? '_' : character));

        return string.IsNullOrWhiteSpace(safeFileName) ? $"attachment-{attachmentIndex}" : safeFileName;
    }

    private static string CreateUniqueAttachmentPath(string attachmentsFolderPath, string fileName)
    {
        string attachmentPath = Path.Combine(attachmentsFolderPath, fileName);

        if (!File.Exists(attachmentPath))
        {
            return attachmentPath;
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        for (int attempt = 1; attempt < 100; attempt++)
        {
            string candidatePath = Path.Combine(attachmentsFolderPath, $"{baseName}-{attempt}{extension}");

            if (!File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        throw new IOException($"Could not create a unique attachment file path for '{fileName}'.");
    }
}
