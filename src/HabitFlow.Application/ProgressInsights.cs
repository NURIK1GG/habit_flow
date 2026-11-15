namespace HabitFlow.Application;
public sealed record WeeklyProgressDto(DateOnly Start, DateOnly End, int Completed, int Target, int ActiveDays, int Minutes)
{
    public int Percent => Math.Min(100, Completed * 100 / Target);
    public int Remaining => Math.Max(0, Target - Completed);
}
public sealed record AchievementDto(string Id, string Title, string Description, string Icon, int Current, int Target)
{
    public bool Unlocked => Current >= Target;
    public int Percent => Math.Min(100, Current * 100 / Target);
}
/// <summary>Read-only insights derived from actual check-ins; undo immediately recalculates them.</summary>
public static class ProgressInsights
{
    public static WeeklyProgressDto Week(IReadOnlyList<HistoryDto> history, DateOnly today, int target)
    {
        if (target is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(target));
        var start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var entries = history.Where(h => h.Date >= start && h.Date <= today).ToArray();
        return new(start, start.AddDays(6), entries.Length, target, entries.Select(h => h.Date).Distinct().Count(), entries.Sum(h => h.Minutes));
    }
    public static IReadOnlyList<AchievementDto> Achievements(DashboardDto data) =>
    [
        new("first", "First step", "Record your first check-in.", "check", data.History.Count, 1),
        new("ten", "Building momentum", "Complete 10 check-ins across your routines.", "trend", data.History.Count, 10),
        new("streak", "Seven strong", "Reach a 7-day scheduled streak in any routine.", "flame", data.Routines.Select(r => r.BestStreak).DefaultIfEmpty(0).Max(), 7),
        new("time", "Time well spent", "Invest 300 minutes in your routines.", "clock", data.History.Sum(h => h.Minutes), 300)
    ];
}
