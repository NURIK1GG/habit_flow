using HabitFlow.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace HabitFlow.Tests;
public sealed class SeederTests
{
    [Fact]
    public async Task ProductionBootstrapsOnlyConfiguredAdministratorAndIsIdempotent()
    {
        using var application = new ProductionApplication();
        using var client = application.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.True((await client.GetAsync("/health")).IsSuccessStatusCode);
        await using var scope = application.Services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        Assert.Equal(1, await db.Users.CountAsync()); Assert.Equal(0, await db.Routines.CountAsync());
        Assert.Equal("production-test@example.com", (await db.Users.SingleAsync()).Email);
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().RunAsync();
        Assert.Equal(1, await db.Users.CountAsync()); Assert.Equal(2, await db.Roles.CountAsync());
    }
}
internal sealed class ProductionApplication : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"habitflow-production-test-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={databasePath};Foreign Keys=True");
        builder.UseSetting("Bootstrap:AdminEmail", "production-test@example.com");
        builder.UseSetting("Bootstrap:AdminPassword", "OnlyForTests!123");
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing); Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(databasePath)!, Path.GetFileName(databasePath) + "*")) File.Delete(file);
    }
}
