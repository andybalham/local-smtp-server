using LocalSmtpCapture.Configuration;

namespace LocalSmtpCapture.Hosting;

/// <summary>
/// Formats the human-readable startup summary.
/// </summary>
public interface IStartupSummaryFormatter
{
    /// <summary>
    /// Formats the effective startup configuration for console display.
    /// </summary>
    /// <param name="options">The effective application options.</param>
    /// <returns>The formatted startup summary.</returns>
    string Format(LocalSmtpCaptureOptions options);
}
