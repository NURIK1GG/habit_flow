using HabitFlow.Domain;
namespace HabitFlow.Tests;
public sealed class StreakTests
{
    private readonly DateOnly today = new(2026, 11, 14);
    [Fact]
    public void UnfinishedTodayPreservesYesterdayStreak()
    {
        var r = new Routine { CreatedOn = today.AddDays(-5) };
        r.CheckIns = [new() { Date = today.AddDays(-1) }, new() { Date = today.AddDays(-2) }];
        Assert.Equal(2, StreakCalculator.Current(r, today)); Assert.Equal(2, StreakCalculator.Best(r, today));
    }
    [Fact]
    public void MissingYesterdayBreaksStreak()
    {
        var r = new Routine { CreatedOn = today.AddDays(-5), CheckIns = [new() { Date = today.AddDays(-2) }] };
        Assert.Equal(0, StreakCalculator.Current(r, today));
    }
    [Fact]
    public void RestDaysDoNotBreakStreak()
    {
        var r = new Routine
        {
            CreatedOn = new(2026, 11, 9),
            DaysOfWeek = 42,
            CheckIns = [new() { Date = new(2026, 11, 9) }, new() { Date = new(2026, 11, 11) }, new() { Date = new(2026, 11, 13) }]
        };
        Assert.Equal(3, StreakCalculator.Current(r, today)); Assert.Equal(3, StreakCalculator.Best(r, today));
    }
    [Fact]
    public void BestStreakRemainsAfterBreak()
    {
        var r = new Routine { CreatedOn = today.AddDays(-6), CheckIns = Enumerable.Range(3, 4).Select(i => new CheckIn { Date = today.AddDays(-i) }).ToList() };
        Assert.Equal(4, StreakCalculator.Best(r, today)); Assert.Equal(0, StreakCalculator.Current(r, today));
    }
    [Fact] public void NewRoutineHasZeroStreak() => Assert.Equal(0, StreakCalculator.Current(new() { CreatedOn = today }, today));
}
