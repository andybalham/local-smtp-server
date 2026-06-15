namespace LocalSmtpCapture.Configuration;

/// <summary>
/// Provides the root configuration model for LocalSmtpCapture.
/// </summary>
public sealed class LocalSmtpCaptureOptions
{
    /// <summary>
    /// Gets or sets SMTP listener settings.
    /// </summary>
    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>
    /// Gets or sets filesystem storage settings.
    /// </summary>
    public StorageOptions Storage { get; set; } = new();

    /// <summary>
    /// Gets or sets console summary settings.
    /// </summary>
    public ConsoleOptions Console { get; set; } = new();
}
