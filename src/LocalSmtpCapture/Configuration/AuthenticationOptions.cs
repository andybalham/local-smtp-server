using System.ComponentModel.DataAnnotations;

namespace LocalSmtpCapture.Configuration;

/// <summary>
/// Provides SMTP authentication settings for local client connections.
/// </summary>
public sealed class AuthenticationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether SMTP authentication is required.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the username accepted by the local SMTP server.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Username { get; set; } = "local";

    /// <summary>
    /// Gets or sets the password accepted by the local SMTP server.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = "local";
}
