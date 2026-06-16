using LocalSmtpCapture.Configuration;
using LocalSmtpCapture.Console;
using LocalSmtpCapture.Hosting;
using LocalSmtpCapture.Smtp;
using LocalSmtpCapture.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmtpServer.Authentication;
using SmtpServer.Storage;

/// <summary>
/// Application entry point and host composition root.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the LocalSmtpCapture console application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A task that completes when the host shuts down.</returns>
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = CreateBuilder(args);

        await builder.Build().RunAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Creates and configures the LocalSmtpCapture generic host builder.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The configured host application builder.</returns>
    public static HostApplicationBuilder CreateBuilder(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.Sources.Clear();
        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();

        builder.Services.Configure<ConsoleLifetimeOptions>(
            options => options.SuppressStatusMessages = true);

        builder.Services
            .AddOptions<LocalSmtpCaptureOptions>()
            .Bind(builder.Configuration)
            .Validate(static options =>
            {
                try
                {
                    LocalSmtpCaptureOptionsValidator.ValidateAndThrow(options);
                    return true;
                }
                catch (ConfigurationValidationException)
                {
                    return false;
                }
            }, "LocalSmtpCapture configuration is invalid.")
            .ValidateOnStart();

        builder.Services.AddHostedService<StartupLoggingService>();
        builder.Services.AddHostedService<SmtpListenerService>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IStartupSummaryFormatter, StartupSummaryFormatter>();
        builder.Services.AddSingleton<IStartupSummaryConsole, StartupSummaryConsole>();
        builder.Services.AddSingleton<IEmailSummaryFormatter, EmailSummaryFormatter>();
        builder.Services.AddSingleton<IMessageFolderNameGenerator, MessageFolderNameGenerator>();
        builder.Services.AddSingleton<ICapturedEmailRetentionPruner, CapturedEmailRetentionPruner>();
        builder.Services.AddSingleton<IEmailMessagePersistenceService, EmailMessagePersistenceService>();
        builder.Services.AddSingleton<IUserAuthenticator, SmtpAuthenticationValidator>();
        builder.Services.AddSingleton<IMessageStore, SmtpMessageStore>();

        return builder;
    }
}
