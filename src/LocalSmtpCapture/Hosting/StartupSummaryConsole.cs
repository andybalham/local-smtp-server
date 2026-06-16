namespace LocalSmtpCapture.Hosting;

/// <summary>
/// Writes startup summary text to the system console.
/// </summary>
public sealed class StartupSummaryConsole : IStartupSummaryConsole
{
    /// <inheritdoc />
    public void Write(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        global::System.Console.Write(text);
    }
}
