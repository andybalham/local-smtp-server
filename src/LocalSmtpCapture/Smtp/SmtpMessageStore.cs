using LocalSmtpCapture.Console;
using LocalSmtpCapture.Storage;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;

namespace LocalSmtpCapture.Smtp;

/// <summary>
/// Handles accepted SMTP messages by parsing, persisting, and summarizing them.
/// </summary>
/// <param name="persistenceService">The persistence service for captured messages.</param>
/// <param name="retentionPruner">The retention pruner for captured message folders.</param>
/// <param name="summaryFormatter">The console summary formatter.</param>
/// <param name="timeProvider">The time provider used to timestamp received messages.</param>
/// <param name="logger">The SMTP message logger.</param>
public sealed class SmtpMessageStore(
    IEmailMessagePersistenceService persistenceService,
    ICapturedEmailRetentionPruner retentionPruner,
    IEmailSummaryFormatter summaryFormatter,
    TimeProvider timeProvider,
    ILogger<SmtpMessageStore> logger)
    : MessageStore
{
    /// <inheritdoc />
    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            await using MemoryStream stream = new();

            foreach (ReadOnlyMemory<byte> segment in buffer)
            {
                await stream.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
            }

            stream.Position = 0;
            MimeMessage message = await MimeMessage.LoadAsync(stream, cancellationToken).ConfigureAwait(false);
            DateTimeOffset receivedAt = timeProvider.GetLocalNow();
            PersistedMessage persistedMessage = await persistenceService
                .SaveAsync(message, cancellationToken)
                .ConfigureAwait(false);

            string summary = summaryFormatter.Format(message, persistedMessage, receivedAt);

            logger.LogInformation("Received message saved to {MessageFolderPath}", persistedMessage.MessageFolderPath);
            logger.LogInformation("{EmailSummary}", summary);
            await PruneCapturedMessagesAsync(cancellationToken).ConfigureAwait(false);

            return SmtpResponse.Ok;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save received SMTP message.");

            return SmtpResponse.TransactionFailed;
        }
    }

    private async Task PruneCapturedMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            RetentionPruneResult result = await retentionPruner.PruneAsync(cancellationToken).ConfigureAwait(false);

            if (result.DeletedFolderPaths.Count > 0)
            {
                logger.LogInformation(
                    "Pruned {DeletedFolderCount} captured message folder(s).",
                    result.DeletedFolderPaths.Count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to prune captured message folders after saving SMTP message.");
        }
    }
}
