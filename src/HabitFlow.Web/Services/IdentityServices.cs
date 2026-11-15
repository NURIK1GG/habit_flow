using System.Security.Claims;
using HabitFlow.Application;
using HabitFlow.Infrastructure;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace HabitFlow.Web.Services;
public sealed class CurrentUser(AuthenticationStateProvider authentication, IDbContextFactory<AppDbContext> factory) : ICurrentUser
{
    public async Task<string> GetIdAsync()
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal.Identity?.IsAuthenticated != true || id is null) throw new UnauthorizedAccessException("Please sign in to continue.");
        await using var db = await factory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
        if (user is null || user.LockoutEnd > DateTimeOffset.UtcNow) throw new UnauthorizedAccessException("Your session is no longer available. Please sign in again.");
        if (principal.FindFirstValue("AspNet.Identity.SecurityStamp") != user.SecurityStamp)
            throw new UnauthorizedAccessException("Your session expired. Please sign in again.");
        return id;
    }
    public async Task RequireAdminAsync()
    {
        var id = await GetIdAsync();
        await using var db = await factory.CreateDbContextAsync();
        if (!await (from ur in db.UserRoles
                    join r in db.Roles on ur.RoleId equals r.Id
                    where ur.UserId == id && r.Name == "Admin"
                    select ur).AnyAsync())
            throw new UnauthorizedAccessException("Administrator access is required.");
    }
}
public sealed class IdentityRevalidationProvider(ILoggerFactory logger, IServiceScopeFactory scopes)
    : RevalidatingServerAuthenticationStateProvider(logger)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);
    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState state, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await users.GetUserAsync(state.User);
        return user is not null && !await users.IsLockedOutAsync(user)
            && state.User.FindFirstValue("AspNet.Identity.SecurityStamp") == await users.GetSecurityStampAsync(user);
    }
}
