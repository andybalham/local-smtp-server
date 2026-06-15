namespace LocalSmtpCapture.Configuration;

/// <summary>
/// Provides console output settings for received email summaries.
/// </summary>
public sealed class ConsoleOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether recipient addresses are printed in summaries.
    /// </summary>
    public bool IncludeRecipients { get; set; } = true;
}
