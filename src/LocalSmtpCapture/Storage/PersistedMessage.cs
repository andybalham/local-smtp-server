namespace LocalSmtpCapture.Storage;

/// <summary>
/// Describes the files written for a captured SMTP message.
/// </summary>
/// <param name="MessageFolderPath">The full path to the unique message folder.</param>
/// <param name="RawMessagePath">The full path to the saved raw MIME message.</param>
/// <param name="TextBodyPath">The full path to the saved plain text body, when one was present.</param>
/// <param name="HtmlBodyPath">The full path to the saved HTML body, when one was present.</param>
/// <param name="AttachmentPaths">The full paths to saved attachment files.</param>
public sealed record PersistedMessage(
    string MessageFolderPath,
    string RawMessagePath,
    string? TextBodyPath,
    string? HtmlBodyPath,
    IReadOnlyList<string> AttachmentPaths);
