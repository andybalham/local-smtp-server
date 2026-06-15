using System.Text.RegularExpressions;
using LocalSmtpCapture.Storage;
using MimeKit;

namespace LocalSmtpCapture.Tests.Storage;

public sealed class MessageFolderNameGeneratorTests
{
    [Fact]
    public void GenerateFolderName_IncludesLocalTimestamp()
    {
        TimeZoneInfo localTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "TestUtcPlusOne",
            TimeSpan.FromHours(1),
            "Test UTC+1",
            "Test UTC+1");
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 15, 9, 30, 0, TimeSpan.Zero), localTimeZone);
        MessageFolderNameGenerator generator = new(timeProvider);
        MimeMessage message = CreateMessage();

        string folderName = generator.GenerateFolderName(message);

        Assert.StartsWith("20260615-103000-", folderName, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateFolderName_ProducesFilesystemSafeName()
    {
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 15, 9, 30, 0, TimeSpan.Zero));
        MessageFolderNameGenerator generator = new(timeProvider);
        MimeMessage message = CreateMessage();

        string folderName = generator.GenerateFolderName(message);

        Assert.Matches(
            new Regex(
                "^[0-9]{8}-[0-9]{6}-recipient@example-com-Test-subject-[a-f0-9]{8}$",
                RegexOptions.CultureInvariant),
            folderName);
        Assert.DoesNotContain(folderName, Path.GetInvalidFileNameChars());
    }

    [Fact]
    public void GenerateFolderName_RepeatedCallsProduceUniqueNames()
    {
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 15, 9, 30, 0, TimeSpan.Zero));
        MessageFolderNameGenerator generator = new(timeProvider);
        MimeMessage message = CreateMessage();

        HashSet<string> folderNames = [];

        for (int i = 0; i < 100; i++)
        {
            Assert.True(folderNames.Add(generator.GenerateFolderName(message)));
        }
    }

    [Fact]
    public void GenerateFolderName_IncludesFirstRecipientAndSubject()
    {
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 15, 9, 30, 0, TimeSpan.Zero));
        MessageFolderNameGenerator generator = new(timeProvider);
        MimeMessage message = CreateMessage();
        message.To.Insert(0, MailboxAddress.Parse("first@example.com"));
        message.Subject = "Invoice test";

        string folderName = generator.GenerateFolderName(message);

        Assert.Matches(
            new Regex(
                "^[0-9]{8}-[0-9]{6}-first@example-com-Invoice-test-[a-f0-9]{8}$",
                RegexOptions.CultureInvariant),
            folderName);
    }

    [Fact]
    public void GenerateFolderName_UsesFallbacksForMissingRecipientAndSubject()
    {
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 15, 9, 30, 0, TimeSpan.Zero));
        MessageFolderNameGenerator generator = new(timeProvider);
        MimeMessage message = new();

        string folderName = generator.GenerateFolderName(message);

        Assert.Matches(
            new Regex(
                "^[0-9]{8}-[0-9]{6}-unknown-recipient-no-subject-[a-f0-9]{8}$",
                RegexOptions.CultureInvariant),
            folderName);
    }

    [Fact]
    public void GenerateFolderName_ReplacesUnsafeCharactersAndFullStopsInRecipientAndSubject()
    {
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 15, 9, 30, 0, TimeSpan.Zero));
        MessageFolderNameGenerator generator = new(timeProvider);
        MimeMessage message = CreateMessage();
        message.To.Clear();
        message.To.Add(MailboxAddress.Parse("recipient@example.com"));
        message.Subject = "Invoice.v1: test / follow up";

        string folderName = generator.GenerateFolderName(message);

        Assert.Matches(
            new Regex(
                "^[0-9]{8}-[0-9]{6}-recipient@example-com-Invoice-v1-test-follow-up-[a-f0-9]{8}$",
                RegexOptions.CultureInvariant),
            folderName);
        Assert.DoesNotContain(folderName, Path.GetInvalidFileNameChars());
        Assert.DoesNotContain('.', folderName);
    }

    private static MimeMessage CreateMessage()
    {
        MimeMessage message = new()
        {
            Subject = "Test subject"
        };
        message.To.Add(MailboxAddress.Parse("recipient@example.com"));

        return message;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;
        private readonly TimeZoneInfo localTimeZone;

        public FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo? localTimeZone = null)
        {
            this.utcNow = utcNow;
            this.localTimeZone = localTimeZone ?? TimeZoneInfo.Utc;
        }

        public override TimeZoneInfo LocalTimeZone => localTimeZone;

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
