using System.ComponentModel.DataAnnotations;

namespace LocalSmtpCapture.Configuration;

/// <summary>
/// Provides SMTP listener settings.
/// </summary>
public sealed class SmtpOptions
{
    /// <summary>
    /// Gets or sets the host address the SMTP server binds to.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the TCP port the SMTP server listens on.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 2525;

    /// <summary>
    /// Gets or sets SMTP authentication settings.
    /// </summary>
    public AuthenticationOptions Authentication { get; set; } = new();
}
