using System.ComponentModel.DataAnnotations;

namespace LocalSmtpCapture.Configuration;

/// <summary>
/// Provides retention settings for captured email folders.
/// </summary>
public sealed class RetentionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether captured message folders are pruned.
    /// </summary>
    public bool PruneCapturedMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of captured message folders to keep.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? MaxMessages { get; set; } = 30;
}
