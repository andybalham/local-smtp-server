namespace LocalSmtpCapture.Hosting;

/// <summary>
/// Writes the human-readable startup summary to the console.
/// </summary>
public interface IStartupSummaryConsole
{
    /// <summary>
    /// Writes startup summary text.
    /// </summary>
    /// <param name="text">The text to write.</param>
    void Write(string text);
}
