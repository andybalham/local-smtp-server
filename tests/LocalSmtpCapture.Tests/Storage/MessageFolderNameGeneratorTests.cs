using System.Text.RegularExpressions;
using LocalSmtpCapture.Storage;

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

        string folderName = generator.GenerateFolderName();

        Assert.StartsWith("20260615-103000-", folderName, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateFolderName_ProducesFilesystemSafeName()
    {
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 15, 9, 30, 0, TimeSpan.Zero));
        MessageFolderNameGenerator generator = new(timeProvider);

        string folderName = generator.GenerateFolderName();

        Assert.Matches(new Regex("^[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$", RegexOptions.CultureInvariant), folderName);
        Assert.DoesNotContain(folderName, Path.GetInvalidFileNameChars());
    }

    [Fact]
    public void GenerateFolderName_RepeatedCallsProduceUniqueNames()
    {
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 15, 9, 30, 0, TimeSpan.Zero));
        MessageFolderNameGenerator generator = new(timeProvider);

        HashSet<string> folderNames = [];

        for (int i = 0; i < 100; i++)
        {
            Assert.True(folderNames.Add(generator.GenerateFolderName()));
        }
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
