using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace HabitFlow.Tests;
internal sealed class FixedClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    public DateOnly Today => DateOnly.FromDateTime(GetUtcNow().UtcDateTime);
}
internal sealed class FakeUser(string id = "owner", bool admin = false) : ICurrentUser
{
    public Task<string> GetIdAsync() => Task.FromResult(id);
    public Task RequireAdminAsync() => admin ? Task.CompletedTask : Task.FromException(new UnauthorizedAccessException());
}
internal sealed class FakeNotifier : IProgressNotifier
{
    public int Count { get; private set; }
    public Task ChangedAsync() { Count++; return Task.CompletedTask; }
}
internal sealed class TestDatabase : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");
    private readonly DbContextOptions<AppDbContext> options;
    public TestDatabase()
    {
        connection.Open(); options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var db = CreateDbContext(); db.Database.Migrate();
        db.Users.AddRange(new AppUser { Id = "owner", UserName = "owner@example.com", DisplayName = "Owner", IsPublic = true },
            new AppUser { Id = "other", UserName = "other@example.com", DisplayName = "Private person", IsPublic = false });
        db.SaveChanges();
    }
    public AppDbContext CreateDbContext() => new(options);
    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    public void Dispose() => connection.Dispose();
}
internal sealed class FakeTracker : ITrackerService
{
    public int Loads { get; private set; }
    public bool Fail { get; set; }
    public async Task<DashboardDto> LoadAsync(CancellationToken ct = default)
    {
        Loads++; await Task.Yield(); ct.ThrowIfCancellationRequested();
        if (Fail) throw new RuleException("Expected validation failure.");
        return new([], [], [], [], new("Test member", false));
    }
    public Task SaveRoutineAsync(RoutineEdit edit, CancellationToken ct = default) => Task.CompletedTask;
    public Task SetArchivedAsync(Guid id, bool archived, CancellationToken ct = default) => Task.CompletedTask;
    public Task CheckInAsync(CheckInEdit edit, CancellationToken ct = default) => Task.CompletedTask;
    public Task UndoAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveProfileAsync(ProfileEdit edit, CancellationToken ct = default) => Task.CompletedTask;
}
