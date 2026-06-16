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
public sealed class StartupLoggingService(
    IOptions<LocalSmtpCaptureOptions> options,
    ILogger<StartupLoggingService> logger)
    : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        LocalSmtpCaptureOptions value = options.Value;

        logger.LogInformation("LocalSmtpCapture starting.");
        logger.LogInformation("SMTP host: {SmtpHost}", value.Smtp.Host);
        logger.LogInformation("SMTP port: {SmtpPort}", value.Smtp.Port);
        logger.LogInformation("SMTP authentication enabled: {AuthenticationEnabled}", value.Smtp.Authentication.Enabled);
        logger.LogInformation("Email output folder: {OutputFolder}", value.Storage.OutputFolder);
        logger.LogInformation(
            "Captured message pruning enabled: {PruneCapturedMessages}",
            value.Storage.Retention.PruneCapturedMessages);
        logger.LogInformation("Captured message retention limit: {MaxMessages}", value.Storage.Retention.MaxMessages);
        logger.LogInformation("Configuration sources: appsettings.json, environment variables.");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("LocalSmtpCapture stopping.");

        return Task.CompletedTask;
    }
}
