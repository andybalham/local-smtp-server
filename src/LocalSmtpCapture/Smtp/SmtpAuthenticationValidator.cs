using LocalSmtpCapture.Configuration;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Authentication;

namespace LocalSmtpCapture.Smtp;

/// <summary>
/// Validates local SMTP credentials against the configured username and password.
/// </summary>
/// <param name="options">The configured application options.</param>
public sealed class SmtpAuthenticationValidator(IOptions<LocalSmtpCaptureOptions> options) : IUserAuthenticator
{
    /// <inheritdoc />
    public Task<bool> AuthenticateAsync(
        ISessionContext context,
        string user,
        string password,
        CancellationToken cancellationToken)
    {
        AuthenticationOptions authentication = options.Value.Smtp.Authentication;

        if (!authentication.Enabled)
        {
            return Task.FromResult(true);
        }

        bool isAuthenticated =
            string.Equals(user, authentication.Username, StringComparison.Ordinal) &&
            string.Equals(password, authentication.Password, StringComparison.Ordinal);

        return Task.FromResult(isAuthenticated);
    }
}
