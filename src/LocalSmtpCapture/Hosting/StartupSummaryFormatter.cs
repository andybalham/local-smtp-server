using System.Text;
using LocalSmtpCapture.Configuration;

namespace LocalSmtpCapture.Hosting;

/// <summary>
/// Formats a compact, human-readable startup summary.
/// </summary>
public sealed class StartupSummaryFormatter : IStartupSummaryFormatter
{
    private const int LabelWidth = 21;

    /// <inheritdoc />
    public string Format(LocalSmtpCaptureOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        StringBuilder builder = new();
        builder.AppendLine();
        builder.AppendLine("LocalSmtpCapture");
        builder.AppendLine("Configuration");
        AppendRow(builder, "SMTP endpoint", FormattableString.Invariant($"{options.Smtp.Host}:{options.Smtp.Port}"));
        AppendRow(builder, "Authentication", options.Smtp.Authentication.Enabled ? "enabled" : "disabled");
        AppendRow(builder, "Email output folder", options.Storage.OutputFolder);
        AppendRow(builder, "Configuration sources", "appsettings.json, environment variables");
        builder.AppendLine("Ready.");
        builder.AppendLine("Press Ctrl+C to stop.");

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, string label, string value)
    {
        builder.Append("  ");
        builder.Append(label.PadRight(LabelWidth, ' '));
        builder.Append(' ');
        builder.AppendLine(value);
    }
}
