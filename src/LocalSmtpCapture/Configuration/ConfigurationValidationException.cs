namespace LocalSmtpCapture.Configuration;

/// <summary>
/// Represents one or more configuration validation failures.
/// </summary>
/// <param name="errors">The validation error messages.</param>
public sealed class ConfigurationValidationException(IReadOnlyCollection<string> errors)
    : Exception(CreateMessage(errors))
{
    /// <summary>
    /// Gets the validation error messages.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; } = errors;

    private static string CreateMessage(IReadOnlyCollection<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return errors.Count == 0
            ? "Configuration validation failed."
            : $"Configuration validation failed: {string.Join("; ", errors)}";
    }
}
