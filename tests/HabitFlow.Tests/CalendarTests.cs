using HabitFlow.Domain;
namespace HabitFlow.Tests;
public sealed class CalendarTests
{
    private sealed class Clock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }
    [Theory]
    [InlineData("2026-11-14T18:59:00Z", "2026-11-14")]
    [InlineData("2026-11-14T19:00:00Z", "2026-11-15")]
    [InlineData("2026-12-31T20:00:00Z", "2027-01-01")]
    public void TrackingDateUsesAstanaMidnight(string utc, string expected)
    { Assert.Equal(DateOnly.Parse(expected), TrackerCalendar.Today(new Clock(DateTimeOffset.Parse(utc)))); }
}
