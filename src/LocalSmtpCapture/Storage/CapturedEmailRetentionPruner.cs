using System.Globalization;
using LocalSmtpCapture.Configuration;
using Microsoft.Extensions.Options;

namespace LocalSmtpCapture.Storage;

/// <summary>
/// Prunes captured email folders from filesystem storage.
/// </summary>
/// <param name="options">The configured application options.</param>
public sealed class CapturedEmailRetentionPruner(IOptions<LocalSmtpCaptureOptions> options)
    : ICapturedEmailRetentionPruner
{
    private const string RawMessageFileName = "message.eml";
    private const string FolderTimestampFormat = "yyyyMMdd-HHmmss";
    private const int FolderTimestampLength = 15;

    /// <inheritdoc />
    public Task<RetentionPruneResult> PruneAsync(CancellationToken cancellationToken = default)
    {
        RetentionOptions retention = options.Value.Storage.Retention;

        if (!retention.PruneCapturedMessages || retention.MaxMessages is null)
        {
            return Task.FromResult(RetentionPruneResult.Empty);
        }

        string outputFolder = options.Value.Storage.OutputFolder;

        if (!Directory.Exists(outputFolder))
        {
            return Task.FromResult(RetentionPruneResult.Empty);
        }

        IReadOnlyList<DirectoryInfo> capturedFolders = Directory
            .EnumerateDirectories(outputFolder)
            .Select(static folderPath => new DirectoryInfo(folderPath))
            .Where(static folder => File.Exists(Path.Combine(folder.FullName, RawMessageFileName)))
            .OrderByDescending(GetCapturedAtUtc)
            .ThenByDescending(static folder => folder.Name, StringComparer.Ordinal)
            .ToArray();

        List<string> retainedFolderPaths = [];
        List<string> deletedFolderPaths = [];

        for (int index = 0; index < capturedFolders.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DirectoryInfo capturedFolder = capturedFolders[index];

            if (index < retention.MaxMessages.Value)
            {
                retainedFolderPaths.Add(capturedFolder.FullName);
                continue;
            }

            Directory.Delete(capturedFolder.FullName, recursive: true);
            deletedFolderPaths.Add(capturedFolder.FullName);
        }

        return Task.FromResult(new RetentionPruneResult(deletedFolderPaths, retainedFolderPaths));
    }

    private static DateTimeOffset GetCapturedAtUtc(DirectoryInfo folder)
    {
        if (folder.Name.Length >= FolderTimestampLength
            && DateTimeOffset.TryParseExact(
                folder.Name[..FolderTimestampLength],
                FolderTimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out DateTimeOffset capturedAt))
        {
            return capturedAt.ToUniversalTime();
        }

        return folder.CreationTimeUtc;
    }
}
