using HabitFlow.Application;
namespace HabitFlow.Tests;
public sealed class InsightsTests
{
    private static HistoryDto Entry(DateOnly date, int minutes = 20) => new(Guid.NewGuid(), Guid.NewGuid(), "Walk", date, minutes, "");
    [Fact] public void WeekStartsMondayAndExcludesPreviousWeekAndFuture()
    {
        var today = new DateOnly(2026, 11, 15);
        var progress = ProgressInsights.Week([Entry(today.AddDays(-7)), Entry(today.AddDays(-6)), Entry(today), Entry(today.AddDays(1))], today, 5);
        Assert.Equal(new DateOnly(2026, 11, 9), progress.Start); Assert.Equal(today, progress.End);
        Assert.Equal(2, progress.Completed); Assert.Equal(2, progress.ActiveDays); Assert.Equal(40, progress.Minutes); Assert.Equal(40, progress.Percent);
    }
    [Fact] public void NewMondayResetsProgressAcrossYearBoundary()
    {
        var monday = new DateOnly(2027, 1, 4);
        Assert.Equal(0, ProgressInsights.Week([Entry(monday.AddDays(-1))], monday, 5).Completed);
    }
    [Fact] public void ExtraEntriesDoNotOverflowProgressBar()
    {
        var day = new DateOnly(2026, 11, 15);
        var progress = ProgressInsights.Week([Entry(day), Entry(day)], day, 1);
        Assert.Equal(2, progress.Completed); Assert.Equal(100, progress.Percent); Assert.Equal(0, progress.Remaining); Assert.Equal(1, progress.ActiveDays);
    }
    [Theory] [InlineData(0)] [InlineData(101)] public void InvalidTargetRejected(int target)
        => Assert.Throws<ArgumentOutOfRangeException>(() => ProgressInsights.Week([], new(2026, 11, 15), target));
    [Fact] public void UndoRevokesDerivedMilestone()
    {
        var data = new DashboardDto([], [Entry(new(2026, 11, 15), 300)], [], [], new("Aidos Sapar", false));
        Assert.True(ProgressInsights.Achievements(data).Single(a => a.Id == "first").Unlocked);
        Assert.True(ProgressInsights.Achievements(data).Single(a => a.Id == "time").Unlocked);
        Assert.All(ProgressInsights.Achievements(data with { History = [] }), a => Assert.False(a.Unlocked));
    }
}
