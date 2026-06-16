using LocalSmtpCapture.Configuration;
using Microsoft.Extensions.Configuration;

namespace LocalSmtpCapture.Tests.Configuration;

public sealed class LocalSmtpCaptureOptionsTests
{
    [Fact]
    public void Bind_DefaultAppSettings_UsesExpectedDefaults()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        LocalSmtpCaptureOptions options = new();

        configuration.Bind(options);
        LocalSmtpCaptureOptionsValidator.ValidateAndThrow(options);

        Assert.Equal("127.0.0.1", options.Smtp.Host);
        Assert.Equal(2525, options.Smtp.Port);
        Assert.True(options.Smtp.Authentication.Enabled);
        Assert.Equal("local", options.Smtp.Authentication.Username);
        Assert.Equal("local", options.Smtp.Authentication.Password);
        Assert.Equal("./emails", options.Storage.OutputFolder);
        Assert.Null(options.Storage.FolderNamePattern);
        Assert.False(options.Storage.Retention.PruneCapturedMessages);
        Assert.Null(options.Storage.Retention.MaxMessages);
        Assert.True(options.Console.IncludeRecipients);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void ValidateAndThrow_InvalidPort_ThrowsConfigurationValidationException(int port)
    {
        LocalSmtpCaptureOptions options = CreateValidOptions();
        options.Smtp.Port = port;

        ConfigurationValidationException exception = Assert.Throws<ConfigurationValidationException>(
            () => LocalSmtpCaptureOptionsValidator.ValidateAndThrow(options));

        Assert.Contains(exception.Errors, error => error.Contains("Smtp", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => error.Contains("Port", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => error.Contains("1 and 65535", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndThrow_MissingOrEmptyOutputFolder_ThrowsConfigurationValidationException(string outputFolder)
    {
        LocalSmtpCaptureOptions options = CreateValidOptions();
        options.Storage.OutputFolder = outputFolder;

        ConfigurationValidationException exception = Assert.Throws<ConfigurationValidationException>(
            () => LocalSmtpCaptureOptionsValidator.ValidateAndThrow(options));

        Assert.Contains("Storage:OutputFolder must not be empty.", exception.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateAndThrow_InvalidRetentionMaxMessages_ThrowsConfigurationValidationException(int maxMessages)
    {
        LocalSmtpCaptureOptions options = CreateValidOptions();
        options.Storage.Retention.PruneCapturedMessages = true;
        options.Storage.Retention.MaxMessages = maxMessages;

        ConfigurationValidationException exception = Assert.Throws<ConfigurationValidationException>(
            () => LocalSmtpCaptureOptionsValidator.ValidateAndThrow(options));

        Assert.Contains(exception.Errors, error => error.Contains("Storage:Retention", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => error.Contains("MaxMessages", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateAndThrow_PruneCapturedMessagesWithoutLimit_ThrowsConfigurationValidationException()
    {
        LocalSmtpCaptureOptions options = CreateValidOptions();
        options.Storage.Retention.PruneCapturedMessages = true;

        ConfigurationValidationException exception = Assert.Throws<ConfigurationValidationException>(
            () => LocalSmtpCaptureOptionsValidator.ValidateAndThrow(options));

        Assert.Contains(
            "Storage:Retention:MaxMessages is required when captured message pruning is enabled.",
            exception.Errors);
    }

    [Fact]
    public void ValidateAndThrow_PruneCapturedMessagesWithMaxMessages_DoesNotThrow()
    {
        LocalSmtpCaptureOptions options = CreateValidOptions();
        options.Storage.Retention.PruneCapturedMessages = true;
        options.Storage.Retention.MaxMessages = 100;

        LocalSmtpCaptureOptionsValidator.ValidateAndThrow(options);
    }

    private static LocalSmtpCaptureOptions CreateValidOptions()
    {
        return new LocalSmtpCaptureOptions
        {
            Smtp = new SmtpOptions
            {
                Host = "127.0.0.1",
                Port = 2525,
                Authentication = new AuthenticationOptions
                {
                    Enabled = true,
                    Username = "local",
                    Password = "local"
                }
            },
            Storage = new StorageOptions
            {
                OutputFolder = "./emails",
                Retention = new RetentionOptions()
            },
            Console = new ConsoleOptions
            {
                IncludeRecipients = true
            }
        };
    }
}
