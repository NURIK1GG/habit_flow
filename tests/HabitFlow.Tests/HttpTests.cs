using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
namespace HabitFlow.Tests;
public sealed class HttpTests : IClassFixture<TestApplication>
{
    private readonly TestApplication application;
    public HttpTests(TestApplication application) => this.application = application;
    private HttpClient Client() => application.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });
    private static string Token(string html) => Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    private static async Task<HttpResponseMessage> Login(HttpClient client, string email = "arman@habitflow.local", string password = "Demo!Pass123")
    {
        var html = await client.GetStringAsync("/account/login"); var token = Token(html); Assert.NotEmpty(token);
        return await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["email"] = email, ["password"] = password, ["__RequestVerificationToken"] = token }));
    }
    [Fact]
    public async Task HealthRespondsAndAnonymousHubIsProtected()
    {
        using var client = Client(); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        var response = await client.PostAsync("/hubs/progress/negotiate?negotiateVersion=1", null);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized);
    }
    [Fact]
    public async Task LoginUsesSecureHttpOnlyCookieAndUserCannotOpenAdminData()
    {
        using var client = Client(); var response = await Login(client); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        var denied = await client.GetAsync("/admin"); Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("/denied", denied.Headers.Location?.OriginalString);
    }
    [Fact]
    public async Task AdminCanSeeAccountAdministration()
    {
        using var client = Client(); Assert.Equal(HttpStatusCode.Redirect, (await Login(client, "ayan@habitflow.local")).StatusCode);
        Assert.Contains("Community accounts", await client.GetStringAsync("/admin"));
    }
    [Fact]
    public async Task MissingAntiforgeryTokenIsRejected()
    {
        using var client = Client(); var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["email"] = "arman@habitflow.local", ["password"] = "Demo!Pass123" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task WrongPasswordDoesNotIssueAuthenticationCookie()
    {
        using var client = Client(); var response = await Login(client, password: "Wrong!Password123");
        Assert.Contains("error=", response.Headers.Location?.OriginalString);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.StartsWith(".AspNetCore.Identity.Application")));
    }
}
public sealed class TestApplication : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"habitflow-tests-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={databasePath};Foreign Keys=True");
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(databasePath)!, Path.GetFileName(databasePath) + "*")) File.Delete(file);
    }
}
