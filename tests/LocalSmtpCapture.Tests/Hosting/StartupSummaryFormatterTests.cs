using LocalSmtpCapture.Configuration;
using LocalSmtpCapture.Hosting;

namespace LocalSmtpCapture.Tests.Hosting;

public sealed class StartupSummaryFormatterTests
{
    [Fact]
    public void Format_ValidOptions_ReturnsAlignedSummaryWithReadyLine()
    {
        LocalSmtpCaptureOptions options = new()
        {
            Smtp = new SmtpOptions
            {
                Host = "localhost",
                Port = 2500,
                Authentication = new AuthenticationOptions
                {
                    Enabled = false,
                },
            },
            Storage = new StorageOptions
            {
                OutputFolder = "captured-mail",
            },
        };
        StartupSummaryFormatter formatter = new();

        string summary = formatter.Format(options);

        string[] lines = summary.Split(Environment.NewLine, StringSplitOptions.None);
        Assert.Equal(string.Empty, lines[0]);
        Assert.Equal("LocalSmtpCapture", lines[1]);
        Assert.Equal("Configuration", lines[2]);
        Assert.Equal("  SMTP endpoint         localhost:2500", lines[3]);
        Assert.Equal("  Authentication        disabled", lines[4]);
        Assert.Equal("  Email output folder   captured-mail", lines[5]);
        Assert.Equal("  Configuration sources appsettings.json, environment variables", lines[6]);
        Assert.Equal("Ready.", lines[7]);
        Assert.Equal("Press Ctrl+C to stop.", lines[8]);
    }

    [Fact]
    public void Format_AuthenticationEnabled_DisplaysEnabled()
    {
        LocalSmtpCaptureOptions options = new()
        {
            Smtp = new SmtpOptions
            {
                Authentication = new AuthenticationOptions
                {
                    Enabled = true,
                },
            },
        };
        StartupSummaryFormatter formatter = new();

        string summary = formatter.Format(options);

        Assert.Contains("  Authentication        enabled", summary, StringComparison.Ordinal);
    }
}
