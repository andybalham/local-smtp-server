using LocalSmtpCapture.Configuration;
using LocalSmtpCapture.Smtp;
using Microsoft.Extensions.Options;

namespace LocalSmtpCapture.Tests.Smtp;

public sealed class SmtpAuthenticationValidatorTests
{
    [Fact]
    public async Task AuthenticateAsync_ConfiguredCredentials_ReturnsTrue()
    {
        SmtpAuthenticationValidator validator = CreateValidator();

        bool isAuthenticated = await validator.AuthenticateAsync(
            context: null!,
            user: "local",
            password: "local",
            cancellationToken: CancellationToken.None);

        Assert.True(isAuthenticated);
    }

    [Theory]
    [InlineData("wrong", "local")]
    [InlineData("local", "wrong")]
    [InlineData("LOCAL", "local")]
    public async Task AuthenticateAsync_InvalidCredentials_ReturnsFalse(string user, string password)
    {
        SmtpAuthenticationValidator validator = CreateValidator();

        bool isAuthenticated = await validator.AuthenticateAsync(
            context: null!,
            user,
            password,
            cancellationToken: CancellationToken.None);

        Assert.False(isAuthenticated);
    }

    private static SmtpAuthenticationValidator CreateValidator()
    {
        LocalSmtpCaptureOptions options = new()
        {
            Smtp = new SmtpOptions
            {
                Authentication = new AuthenticationOptions
                {
                    Enabled = true,
                    Username = "local",
                    Password = "local"
                }
            }
        };

        return new SmtpAuthenticationValidator(Options.Create(options));
    }
}
