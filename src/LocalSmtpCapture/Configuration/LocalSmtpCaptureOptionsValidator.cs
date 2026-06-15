using System.ComponentModel.DataAnnotations;

namespace LocalSmtpCapture.Configuration;

/// <summary>
/// Validates LocalSmtpCapture configuration objects.
/// </summary>
public static class LocalSmtpCaptureOptionsValidator
{
    /// <summary>
    /// Validates the supplied configuration object and throws when it is invalid.
    /// </summary>
    /// <param name="options">The configuration object to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is null.</exception>
    /// <exception cref="ConfigurationValidationException">Thrown when one or more settings are invalid.</exception>
    public static void ValidateAndThrow(LocalSmtpCaptureOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> errors = [];

        ValidateObject(options.Smtp, "Smtp", errors);
        ValidateObject(options.Smtp.Authentication, "Smtp:Authentication", errors);
        ValidateObject(options.Storage, "Storage", errors);
        ValidateObject(options.Console, "Console", errors);

        if (string.IsNullOrWhiteSpace(options.Smtp.Host))
        {
            errors.Add("Smtp:Host must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.Storage.OutputFolder))
        {
            errors.Add("Storage:OutputFolder must not be empty.");
        }

        if (!string.IsNullOrEmpty(options.Storage.FolderNamePattern)
            && string.IsNullOrWhiteSpace(options.Storage.FolderNamePattern))
        {
            errors.Add("Storage:FolderNamePattern must not be whitespace.");
        }

        if (options.Smtp.Authentication.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Smtp.Authentication.Username))
            {
                errors.Add("Smtp:Authentication:Username must not be empty when authentication is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.Smtp.Authentication.Password))
            {
                errors.Add("Smtp:Authentication:Password must not be empty when authentication is enabled.");
            }
        }

        if (errors.Count > 0)
        {
            throw new ConfigurationValidationException(errors);
        }
    }

    private static void ValidateObject(object instance, string sectionName, List<string> errors)
    {
        ValidationContext context = new(instance);
        List<ValidationResult> results = [];

        if (Validator.TryValidateObject(instance, context, results, validateAllProperties: true))
        {
            return;
        }

        foreach (ValidationResult result in results)
        {
            errors.Add($"{sectionName}: {result.ErrorMessage}");
        }
    }
}
