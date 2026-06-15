using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;

namespace LocalSmtpCapture.Tests.Smtp;

public sealed class SmtpEndToEndTests
{
    [Fact]
    public async Task SmtpServer_PlainTextMessage_SavesMessageFolderWithRawMessageAndBody()
    {
        using TemporaryFolder temporaryFolder = new();
        int port = GetAvailablePort();
        using IHost host = await StartHostAsync(port, temporaryFolder.Path);

        using MailMessage message = new(
            from: "sender@example.com",
            to: "recipient@example.com",
            subject: "Plain SMTP test",
            body: "Plain body from SMTP.");

        await SendAsync(port, message);

        string messageFolderPath = GetSingleMessageFolder(temporaryFolder.Path);
        string rawMessagePath = Path.Combine(messageFolderPath, "message.eml");
        string textBodyPath = Path.Combine(messageFolderPath, "body.txt");

        Assert.True(File.Exists(rawMessagePath));
        Assert.True(File.Exists(textBodyPath));
        Assert.Contains("Plain body from SMTP.", await File.ReadAllTextAsync(textBodyPath));
    }

    [Fact]
    public async Task SmtpServer_HtmlMessageWithAttachment_SavesHtmlBodyAndAttachment()
    {
        using TemporaryFolder temporaryFolder = new();
        int port = GetAvailablePort();
        using IHost host = await StartHostAsync(port, temporaryFolder.Path);

        using MemoryStream attachmentStream = new(Encoding.UTF8.GetBytes("Attachment content"));
        using Attachment attachment = new(attachmentStream, "invoice.txt", "text/plain");
        using MailMessage message = new(
            from: "sender@example.com",
            to: "recipient@example.com",
            subject: "HTML SMTP test",
            body: "<p>HTML body from SMTP.</p>")
        {
            IsBodyHtml = true
        };
        message.Attachments.Add(attachment);

        await SendAsync(port, message);

        string messageFolderPath = GetSingleMessageFolder(temporaryFolder.Path);
        string rawMessagePath = Path.Combine(messageFolderPath, "message.eml");
        string htmlBodyPath = Path.Combine(messageFolderPath, "body.html");
        string attachmentPath = Path.Combine(messageFolderPath, "attachments", "invoice.txt");

        Assert.True(File.Exists(rawMessagePath));
        Assert.True(File.Exists(htmlBodyPath));
        Assert.Equal("<p>HTML body from SMTP.</p>", await File.ReadAllTextAsync(htmlBodyPath));
        Assert.True(File.Exists(attachmentPath));
        Assert.Equal("Attachment content", await File.ReadAllTextAsync(attachmentPath));
    }

    [Fact]
    public async Task StopAsync_RunningSmtpServer_StopsAcceptingConnections()
    {
        using TemporaryFolder temporaryFolder = new();
        int port = GetAvailablePort();
        using IHost host = await StartHostAsync(port, temporaryFolder.Path);

        await AssertConnectionAcceptedAsync(port);

        await host.StopAsync();

        await AssertConnectionRejectedAsync(port);
    }

    private static async Task<IHost> StartHostAsync(int port, string outputFolder)
    {
        HostApplicationBuilder builder = Program.CreateBuilder([]);
        builder.Configuration["Smtp:Port"] = port.ToString(CultureInfo.InvariantCulture);
        builder.Configuration["Storage:OutputFolder"] = outputFolder;

        IHost host = builder.Build();
        await host.StartAsync();

        return host;
    }

    private static async Task SendAsync(int port, MailMessage message)
    {
        using SmtpClient client = new("127.0.0.1", port)
        {
            Credentials = new NetworkCredential("local", "local"),
            EnableSsl = false
        };

        await client.SendMailAsync(message).WaitAsync(TimeSpan.FromSeconds(10));
    }

    private static async Task AssertConnectionAcceptedAsync(int port)
    {
        using TcpClient client = new();

        await client.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(client.Connected);
    }

    private static async Task AssertConnectionRejectedAsync(int port)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromMilliseconds(250));
            }
            catch (SocketException)
            {
                return;
            }
            catch (TimeoutException)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        throw new TimeoutException("SMTP listener continued accepting connections after host shutdown.");
    }

    private static int GetAvailablePort()
    {
        TcpListener listener = new(IPAddress.Loopback, port: 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static string GetSingleMessageFolder(string outputFolder)
    {
        Assert.True(Directory.Exists(outputFolder));

        string[] messageFolders = Directory.GetDirectories(outputFolder);

        return Assert.Single(messageFolders);
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
