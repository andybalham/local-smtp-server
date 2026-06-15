using System.Globalization;

namespace LocalSmtpCapture.Storage;

/// <summary>
/// Generates default message folder names using the local timestamp and a short unique identifier.
/// </summary>
public sealed class MessageFolderNameGenerator : IMessageFolderNameGenerator
{
    private const int UniqueIdentifierLength = 8;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageFolderNameGenerator"/> class.
    /// </summary>
    /// <param name="timeProvider">The time provider used to read the current local time.</param>
    public MessageFolderNameGenerator(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public string GenerateFolderName()
    {
        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(utcNow, timeProvider.LocalTimeZone);
        string timestamp = localNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string uniqueIdentifier = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..UniqueIdentifierLength];

        return $"{timestamp}-{uniqueIdentifier}";
    }
}
