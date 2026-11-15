using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace HabitFlow.Infrastructure;
public sealed class RoutineRepository(IDbContextFactory<AppDbContext> factory) : IRoutineRepository
{
    public async Task<IReadOnlyList<Routine>> ListAsync(string ownerId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Routines.AsNoTracking().Where(r => r.OwnerId == ownerId)
            .Include(r => r.Steps).Include(r => r.CheckIns).AsSplitQuery().OrderBy(r => r.CreatedOn).ToListAsync(ct);
    }
    public async Task SaveAsync(string ownerId, Routine routine, bool isNew, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (isNew)
        {
            if (await db.Routines.CountAsync(r => r.OwnerId == ownerId, ct) >= 100)
                throw new RuleException("Your workspace supports up to 100 routines.");
            routine.OwnerId = ownerId; db.Routines.Add(routine);
        }
        else
        {
            var stored = await db.Routines.Include(r => r.Steps).SingleOrDefaultAsync(r => r.Id == routine.Id && r.OwnerId == ownerId, ct)
                ?? throw new RuleException("Routine not found.");
            if (stored.Revision != routine.Revision) throw new ConflictException("This routine changed in another tab. Reload and try again.");
            // Also validate the schedule at the persistence boundary against concurrent check-ins.
            if ((stored.DaysOfWeek != routine.DaysOfWeek || stored.Kind != routine.Kind)
                && await db.CheckIns.AnyAsync(c => c.RoutineId == stored.Id, ct))
                throw new RuleException("Schedule and type cannot change after a check-in.");
            stored.Title = routine.Title; stored.Description = routine.Description; stored.Kind = routine.Kind;
            stored.TargetMinutes = routine.TargetMinutes; stored.DaysOfWeek = routine.DaysOfWeek;
            stored.Revision++;
            db.RoutineSteps.RemoveRange(stored.Steps); stored.Steps = routine.Steps;
        }
        await CommitAsync(db, ct);
    }
    public async Task SetArchivedAsync(string ownerId, Guid id, bool archived, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var r = await db.Routines.SingleOrDefaultAsync(r => r.OwnerId == ownerId && r.Id == id, ct)
            ?? throw new RuleException("Routine not found.");
        r.Archived = archived; r.Revision++; await CommitAsync(db, ct);
    }
    public async Task AddCheckInAsync(string ownerId, CheckIn checkIn, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var routine = await db.Routines.SingleOrDefaultAsync(r => r.OwnerId == ownerId && r.Id == checkIn.RoutineId, ct)
            ?? throw new RuleException("Routine not found.");
        if (routine.Archived || !routine.IsScheduled(checkIn.Date)) throw new RuleException("This routine is unavailable on that date.");
        routine.Revision++; // Conflicts with concurrent archive/edit and makes the operation atomic.
        db.CheckIns.Add(checkIn); await CommitAsync(db, ct);
    }
    public async Task RemoveCheckInAsync(string ownerId, Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var item = await db.CheckIns.Join(db.Routines.Where(r => r.OwnerId == ownerId), c => c.RoutineId, r => r.Id,
            (c, r) => new { CheckIn = c, Routine = r }).SingleOrDefaultAsync(x => x.CheckIn.Id == id, ct)
            ?? throw new RuleException("Check-in not found.");
        item.Routine.Revision++; db.CheckIns.Remove(item.CheckIn); await CommitAsync(db, ct);
    }
    public async Task<IReadOnlyList<LeaderboardDto>> LeadersAsync(string viewerId, DateOnly since, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        // SQL aggregation and projection: private profiles never leave the database.
        var now = DateTimeOffset.UtcNow;
        var totals = from c in db.CheckIns
                     where c.Date >= since
                     join r in db.Routines on c.RoutineId equals r.Id
                     group c by r.OwnerId into g
                     select new { OwnerId = g.Key, Count = g.Count(), Minutes = g.Sum(c => c.Minutes) };
        return await (from u in db.Users
                      where u.IsPublic && (u.LockoutEnd == null || u.LockoutEnd <= now)
                      join t in totals on u.Id equals t.OwnerId into stats
                      from t in stats.DefaultIfEmpty()
                      orderby ((int?)t.Count ?? 0) descending, u.DisplayName, u.Id
                      select new LeaderboardDto(u.DisplayName, ((int?)t.Count ?? 0) * 10,
                          (int?)t.Count ?? 0, (int?)t.Minutes ?? 0, u.Id == viewerId)).Take(100).ToListAsync(ct);
    }
    public async Task<ProfileDto> ProfileAsync(string ownerId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Users.Where(u => u.Id == ownerId).Select(u => new ProfileDto(u.DisplayName, u.IsPublic, u.WeeklyGoal)).SingleAsync(ct);
    }
    public async Task SaveProfileAsync(string ownerId, ProfileEdit profile, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.SingleAsync(u => u.Id == ownerId, ct);
        user.DisplayName = profile.DisplayName; user.IsPublic = profile.IsPublic; user.WeeklyGoal = profile.WeeklyGoal; await CommitAsync(db, ct);
    }
    private static async Task CommitAsync(AppDbContext db, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Your data changed in another tab. Reload and try again."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        { throw new RuleException("This day is already completed, or the data violates a storage rule. Reload and check your input."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 5 or 6 })
        { throw new ConflictException("The database is busy. Please retry in a moment."); }
    }
}
