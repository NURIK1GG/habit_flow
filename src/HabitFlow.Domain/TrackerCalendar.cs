namespace HabitFlow.Domain;
/// <summary>Astana civil calendar (UTC+5), independent of the server operating system.</summary>
public static class TrackerCalendar
{
    public static DateTimeOffset Now(TimeProvider clock) => clock.GetUtcNow().ToOffset(TimeSpan.FromHours(5));
    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(Now(clock).DateTime);
}
