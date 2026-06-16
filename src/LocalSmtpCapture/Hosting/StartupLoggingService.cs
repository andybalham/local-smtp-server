using LocalSmtpCapture.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalSmtpCapture.Hosting;

/// <summary>
/// Logs the effective startup configuration for the local SMTP capture server.
/// </summary>
/// <param name="options">The configured application options.</param>
/// <param name="logger">The startup logger.</param>
/// <param name="applicationLifetime">The host application lifetime notifications.</param>
/// <param name="startupSummaryFormatter">The human-readable startup summary formatter.</param>
/// <param name="startupSummaryConsole">The console writer for the startup summary.</param>
public sealed class StartupLoggingService(
    IOptions<LocalSmtpCaptureOptions> options,
    ILogger<StartupLoggingService> logger,
    IHostApplicationLifetime applicationLifetime,
    IStartupSummaryFormatter startupSummaryFormatter,
    IStartupSummaryConsole startupSummaryConsole)
    : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("LocalSmtpCapture starting.");

        applicationLifetime.ApplicationStarted.Register(() =>
        {
            startupSummaryConsole.Write(startupSummaryFormatter.Format(options.Value));
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("LocalSmtpCapture stopping.");

        return Task.CompletedTask;
    }
}
