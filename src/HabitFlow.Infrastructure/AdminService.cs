using HabitFlow.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace HabitFlow.Infrastructure;
public sealed class AdminService(ICurrentUser current, IServiceScopeFactory scopes,
    IDbContextFactory<AppDbContext> factory, IProgressNotifier notifier) : IAdminService
{
    public async Task<IReadOnlyList<AdminUserDto>> UsersAsync()
    {
        await current.RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var role = await db.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).SingleAsync();
        return await db.Users.AsNoTracking().OrderBy(u => u.Email).Select(u => new AdminUserDto(u.Id,
            u.Email ?? "", u.DisplayName, db.UserRoles.Any(r => r.UserId == u.Id && r.RoleId == role),
            u.LockoutEnd != null && u.LockoutEnd > now, db.Routines.Count(r => r.OwnerId == u.Id))).Take(500).ToListAsync();
    }
    public async Task SetLockedAsync(string userId, bool locked)
    {
        await current.RequireAdminAsync();
        if (userId == await current.GetIdAsync()) throw new RuleException("You cannot suspend your own account.");
        await using var scope = scopes.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var target = await users.FindByIdAsync(userId) ?? throw new RuleException("User not found.");
        if (await users.IsInRoleAsync(target, "Admin")) throw new RuleException("Administrator accounts cannot be suspended here.");
        var result = await users.SetLockoutEndDateAsync(target, locked ? DateTimeOffset.UtcNow.AddYears(100) : null);
        if (!result.Succeeded) throw new RuleException(string.Join(" ", result.Errors.Select(e => e.Description)));
        if (locked) await users.UpdateSecurityStampAsync(target);
        await notifier.ChangedAsync();
    }
}
