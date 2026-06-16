using LocalSmtpCapture.Configuration;
using LocalSmtpCapture.Storage;
using Microsoft.Extensions.Options;

namespace LocalSmtpCapture.Tests.Storage;

public sealed class CapturedEmailRetentionPrunerTests
{
    [Fact]
    public async Task PruneAsync_PruningDisabled_DoesNotDeleteCapturedFolders()
    {
        using TemporaryFolder temporaryFolder = new();
        string oldestFolder = CreateCapturedFolder(temporaryFolder.Path, "20260615-103000-oldest");
        string newestFolder = CreateCapturedFolder(temporaryFolder.Path, "20260615-103100-newest");
        CapturedEmailRetentionPruner pruner = CreatePruner(
            temporaryFolder.Path,
            pruneCapturedMessages: false,
            maxMessages: 1);

        RetentionPruneResult result = await pruner.PruneAsync();

        Assert.Empty(result.DeletedFolderPaths);
        Assert.Empty(result.RetainedFolderPaths);
        Assert.True(Directory.Exists(oldestFolder));
        Assert.True(Directory.Exists(newestFolder));
    }

    [Fact]
    public async Task PruneAsync_Enabled_DeletesOldestCapturedFoldersBeyondLimit()
    {
        using TemporaryFolder temporaryFolder = new();
        string oldestFolder = CreateCapturedFolder(temporaryFolder.Path, "20260615-103000-oldest");
        string middleFolder = CreateCapturedFolder(temporaryFolder.Path, "20260615-103100-middle");
        string newestFolder = CreateCapturedFolder(temporaryFolder.Path, "20260615-103200-newest");
        CapturedEmailRetentionPruner pruner = CreatePruner(
            temporaryFolder.Path,
            pruneCapturedMessages: true,
            maxMessages: 2);

        RetentionPruneResult result = await pruner.PruneAsync();

        Assert.Equal([oldestFolder], result.DeletedFolderPaths);
        Assert.Equal([newestFolder, middleFolder], result.RetainedFolderPaths);
        Assert.False(Directory.Exists(oldestFolder));
        Assert.True(Directory.Exists(middleFolder));
        Assert.True(Directory.Exists(newestFolder));
    }

    [Fact]
    public async Task PruneAsync_Enabled_IgnoresDirectoriesWithoutRawMessageFile()
    {
        using TemporaryFolder temporaryFolder = new();
        string unrelatedFolder = Path.Combine(temporaryFolder.Path, "20260615-102900-unrelated");
        Directory.CreateDirectory(unrelatedFolder);
        string oldCapturedFolder = CreateCapturedFolder(temporaryFolder.Path, "20260615-103000-oldest");
        string newCapturedFolder = CreateCapturedFolder(temporaryFolder.Path, "20260615-103100-newest");
        CapturedEmailRetentionPruner pruner = CreatePruner(
            temporaryFolder.Path,
            pruneCapturedMessages: true,
            maxMessages: 1);

        RetentionPruneResult result = await pruner.PruneAsync();

        Assert.Equal([oldCapturedFolder], result.DeletedFolderPaths);
        Assert.Equal([newCapturedFolder], result.RetainedFolderPaths);
        Assert.True(Directory.Exists(unrelatedFolder));
        Assert.False(Directory.Exists(oldCapturedFolder));
        Assert.True(Directory.Exists(newCapturedFolder));
    }

    [Fact]
    public async Task PruneAsync_OutputFolderMissing_DoesNotCreateOutputFolder()
    {
        using TemporaryFolder temporaryFolder = new();
        string missingFolder = Path.Combine(temporaryFolder.Path, "missing");
        CapturedEmailRetentionPruner pruner = CreatePruner(
            missingFolder,
            pruneCapturedMessages: true,
            maxMessages: 1);

        RetentionPruneResult result = await pruner.PruneAsync();

        Assert.Empty(result.DeletedFolderPaths);
        Assert.Empty(result.RetainedFolderPaths);
        Assert.False(Directory.Exists(missingFolder));
    }

    private static CapturedEmailRetentionPruner CreatePruner(
        string outputFolder,
        bool pruneCapturedMessages,
        int? maxMessages)
    {
        LocalSmtpCaptureOptions options = new()
        {
            Storage = new StorageOptions
            {
                OutputFolder = outputFolder,
                Retention = new RetentionOptions
                {
                    PruneCapturedMessages = pruneCapturedMessages,
                    MaxMessages = maxMessages
                }
            }
        };

        return new CapturedEmailRetentionPruner(Options.Create(options));
    }

    private static string CreateCapturedFolder(string outputFolder, string folderName)
    {
        string folderPath = Path.Combine(outputFolder, folderName);
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "message.eml"), "raw message");

        return folderPath;
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
