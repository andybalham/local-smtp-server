using System.ComponentModel.DataAnnotations;

namespace LocalSmtpCapture.Configuration;

/// <summary>
/// Provides filesystem storage settings for captured email.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// Gets or sets the folder where captured emails are written.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string OutputFolder { get; set; } = "./emails";

    /// <summary>
    /// Gets or sets the optional message folder naming pattern.
    /// </summary>
    public string? FolderNamePattern { get; set; }

    /// <summary>
    /// Gets or sets retention settings for captured email folders.
    /// </summary>
    public RetentionOptions Retention { get; set; } = new();
}
