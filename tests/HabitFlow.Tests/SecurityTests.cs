using System.Security.Claims;
using HabitFlow.Infrastructure;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
namespace HabitFlow.Tests;
public sealed class SecurityTests : IDisposable
{
    private readonly TestDatabase database = new();
    private sealed class Authentication(string stamp = "valid") : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "owner"), new("AspNet.Identity.SecurityStamp", stamp)], "test"))));
    }
    public SecurityTests()
    {
        using var db = database.CreateDbContext(); var user = db.Users.Single(u => u.Id == "owner"); user.SecurityStamp = "valid"; db.SaveChanges();
    }
    [Fact] public async Task ValidPrincipalReadsFreshUser() => Assert.Equal("owner", await new CurrentUser(new Authentication(), database).GetIdAsync());
    [Fact] public async Task RevokedStampIsRejected() => await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new CurrentUser(new Authentication("revoked"), database).GetIdAsync());
    [Fact]
    public async Task SuspensionIsDetectedWithoutWaitingForCircuitRevalidation()
    {
        var current = new CurrentUser(new Authentication(), database); Assert.Equal("owner", await current.GetIdAsync());
        await using var db = database.CreateDbContext(); var user = await db.Users.SingleAsync(u => u.Id == "owner"); user.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => current.GetIdAsync());
    }
    [Fact]
    public async Task AdminRoleCheckedInDatabase()
    {
        var current = new CurrentUser(new Authentication(), database);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => current.RequireAdminAsync());
        await using var db = database.CreateDbContext(); db.Roles.Add(new() { Id = "admin-role", Name = "Admin", NormalizedName = "ADMIN" });
        db.UserRoles.Add(new() { UserId = "owner", RoleId = "admin-role" }); await db.SaveChangesAsync();
        await current.RequireAdminAsync();
    }
    public void Dispose() => database.Dispose();
}
