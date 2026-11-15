using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace HabitFlow.Tests;
public sealed class TrackerTests : IDisposable
{
    private readonly TestDatabase database = new();
    private readonly FixedClock clock = new();
    private readonly FakeNotifier notifier = new();
    private readonly ITrackerService tracker;
    public TrackerTests() => tracker = new TrackerService(new RoutineRepository(database), new FakeUser(), clock, notifier);
    private async Task<RoutineDto> AddAsync()
    { await tracker.SaveRoutineAsync(new() { Title = "Morning walk", Kind = RoutineKind.Workout, Steps = [new() { Name = "Walk", Sets = 1, Repetitions = 1 }] }); return Assert.Single((await tracker.LoadAsync()).Routines); }
    [Fact]
    public async Task CreateCheckInAndUndoRecalculatePointsAndStreaks()
    {
        var r = await AddAsync();
        await tracker.CheckInAsync(new() { RoutineId = r.Id, Date = clock.Today, Minutes = 22, Notes = "Fresh air" });
        var data = await tracker.LoadAsync();
        Assert.Equal(1, data.Routines[0].CurrentStreak); Assert.Equal(10, data.Leaders.Single().Points);
        Assert.Equal(22, data.Days.Last().Minutes); Assert.Equal("Fresh air", Assert.Single(data.History).Notes);
        await tracker.UndoAsync(data.History[0].Id); data = await tracker.LoadAsync();
        Assert.Empty(data.History); Assert.Equal(0, data.Routines[0].CurrentStreak); Assert.Equal(0, data.Leaders.Single().Points);
        Assert.Equal(3, notifier.Count);
    }
    [Fact]
    public async Task DuplicateDayIsRejectedBySqliteUniqueIndex()
    {
        var r = await AddAsync(); var edit = new CheckInEdit { RoutineId = r.Id, Date = clock.Today };
        await tracker.CheckInAsync(edit); await Assert.ThrowsAsync<RuleException>(() => tracker.CheckInAsync(edit));
        Assert.Single((await tracker.LoadAsync()).History);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(-366)]
    public async Task InvalidDatesAreRejected(int offset)
    { var r = await AddAsync(); await Assert.ThrowsAsync<RuleException>(() => tracker.CheckInAsync(new() { RoutineId = r.Id, Date = clock.Today.AddDays(offset) })); }
    [Fact]
    public async Task AnotherUserCannotReadOrMutateRoutine()
    {
        var r = await AddAsync();
        var other = new TrackerService(new RoutineRepository(database), new FakeUser("other"), clock, notifier);
        Assert.Empty((await other.LoadAsync()).Routines);
        await Assert.ThrowsAsync<RuleException>(() => other.CheckInAsync(new() { RoutineId = r.Id, Date = clock.Today }));
        await Assert.ThrowsAsync<RuleException>(() => other.SetArchivedAsync(r.Id, true));
        await Assert.ThrowsAsync<RuleException>(() => other.SaveRoutineAsync(new() { Id = r.Id, Title = "Stolen" }));
    }
    [Fact]
    public async Task ArchivedRoutineRejectsCheckInsAndCanBeRestored()
    {
        var r = await AddAsync(); await tracker.SetArchivedAsync(r.Id, true);
        await Assert.ThrowsAsync<RuleException>(() => tracker.CheckInAsync(new() { RoutineId = r.Id, Date = clock.Today }));
        await tracker.SetArchivedAsync(r.Id, false);
        await tracker.CheckInAsync(new() { RoutineId = r.Id, Date = clock.Today }); Assert.Single((await tracker.LoadAsync()).History);
    }
    [Fact]
    public async Task ScheduleCannotBeRewrittenAfterCompletion()
    {
        var r = await AddAsync(); await tracker.CheckInAsync(new() { RoutineId = r.Id, Date = clock.Today });
        await Assert.ThrowsAsync<RuleException>(() => tracker.SaveRoutineAsync(new() { Id = r.Id, Revision = r.Revision, Title = r.Title, DaysOfWeek = 42 }));
    }
    [Fact]
    public async Task StaleEditsAreRejected()
    {
        var r = await AddAsync(); await tracker.SaveRoutineAsync(new() { Id = r.Id, Revision = r.Revision, Title = "First edit", Kind = r.Kind });
        await Assert.ThrowsAsync<ConflictException>(() => tracker.SaveRoutineAsync(new() { Id = r.Id, Revision = r.Revision, Title = "Stale edit", Kind = r.Kind }));
    }
    [Fact]
    public async Task NestedExerciseValidationRunsOnServer()
    { await Assert.ThrowsAsync<RuleException>(() => tracker.SaveRoutineAsync(new() { Title = "Strength", Steps = [new() { Name = "Squat", Sets = 0 }] })); Assert.Empty((await tracker.LoadAsync()).Routines); }
    [Fact]
    public async Task PrivateProfilesAreExcludedFromLeaderboard()
    {
        Assert.Single((await tracker.LoadAsync()).Leaders);
        await tracker.SaveProfileAsync(new() { DisplayName = "Owner", IsPublic = false });
        Assert.Empty((await tracker.LoadAsync()).Leaders);
    }
    [Fact]
    public async Task ForeignUserCannotUndoEntry()
    {
        var r = await AddAsync(); await tracker.CheckInAsync(new() { RoutineId = r.Id, Date = clock.Today });
        var id = (await tracker.LoadAsync()).History[0].Id;
        var other = new TrackerService(new RoutineRepository(database), new FakeUser("other"), clock, notifier);
        await Assert.ThrowsAsync<RuleException>(() => other.UndoAsync(id)); Assert.Single((await tracker.LoadAsync()).History);
    }
    [Fact]
    public async Task DatabaseMigrationCreatesExpectedTables()
    {
        await using var db = database.CreateDbContext();
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync()); Assert.Equal(2, await db.Users.CountAsync());
    }
    [Fact] public async Task WeeklyTargetPersistsAndRejectsInvalidValues()
    {
        await tracker.SaveProfileAsync(new() { DisplayName = "Arman Sapar", WeeklyGoal = 12 });
        Assert.Equal(12, (await tracker.LoadAsync()).Profile.WeeklyGoal);
        await Assert.ThrowsAsync<RuleException>(() => tracker.SaveProfileAsync(new() { DisplayName = "Arman Sapar", WeeklyGoal = 0 }));
        Assert.Equal(12, (await tracker.LoadAsync()).Profile.WeeklyGoal);
    }
    public void Dispose() => database.Dispose();
}
