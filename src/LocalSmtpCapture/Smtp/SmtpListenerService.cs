using LocalSmtpCapture.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Storage;
using System.Net;
using SmtpComponentProvider = SmtpServer.ComponentModel.ServiceProvider;
using SmtpServerHost = SmtpServer.SmtpServer;

namespace LocalSmtpCapture.Smtp;

/// <summary>
/// Hosts the local SMTP listener for captured test email.
/// </summary>
/// <param name="options">The configured application options.</param>
/// <param name="messageStore">The message store used for accepted SMTP messages.</param>
/// <param name="userAuthenticator">The authenticator used for SMTP AUTH.</param>
/// <param name="logger">The listener logger.</param>
public sealed class SmtpListenerService(
    IOptions<LocalSmtpCaptureOptions> options,
    IMessageStore messageStore,
    IUserAuthenticator userAuthenticator,
    ILogger<SmtpListenerService> logger)
    : IHostedService
{
    private CancellationTokenSource? listenerCancellation;
    private SmtpServerHost? server;
    private Task? serverTask;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        SmtpOptions smtp = options.Value.Smtp;
        IPEndPoint endpoint = CreateEndpoint(smtp.Host, smtp.Port);

        ISmtpServerOptions serverOptions = new SmtpServerOptionsBuilder()
            .ServerName("LocalSmtpCapture")
            .Endpoint(endpointBuilder => endpointBuilder
                .Endpoint(endpoint)
                .AuthenticationRequired(smtp.Authentication.Enabled)
                .AllowUnsecureAuthentication(smtp.Authentication.Enabled))
            .Build();

        SmtpComponentProvider componentProvider = new();
        componentProvider.Add(messageStore);

        if (smtp.Authentication.Enabled)
        {
            componentProvider.Add(userAuthenticator);
        }

        listenerCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        server = new SmtpServerHost(serverOptions, componentProvider);
        serverTask = server.StartAsync(listenerCancellation.Token);

        logger.LogInformation("SMTP listener started on {SmtpHost}:{SmtpPort}.", smtp.Host, smtp.Port);

        return serverTask.IsFaulted ? serverTask : Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (server is null)
        {
            return;
        }

        logger.LogInformation("SMTP listener stopping.");

        server.Shutdown();
        listenerCancellation?.Cancel();

        try
        {
            if (serverTask is not null)
            {
                try
                {
                    await serverTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                }
            }

            logger.LogInformation("SMTP listener stopped.");
        }
        finally
        {
            listenerCancellation?.Dispose();
            listenerCancellation = null;
            server = null;
            serverTask = null;
        }
    }

    private static IPEndPoint CreateEndpoint(string host, int port)
    {
        if (IPAddress.TryParse(host, out IPAddress? address))
        {
            return new IPEndPoint(address, port);
        }

        IPAddress[] addresses = Dns.GetHostAddresses(host);
        IPAddress? firstAddress = addresses.FirstOrDefault(static candidate =>
            candidate.AddressFamily is System.Net.Sockets.AddressFamily.InterNetwork or
                System.Net.Sockets.AddressFamily.InterNetworkV6);

        return firstAddress is null
            ? throw new InvalidOperationException($"Could not resolve SMTP host '{host}'.")
            : new IPEndPoint(firstAddress, port);
    }
}
