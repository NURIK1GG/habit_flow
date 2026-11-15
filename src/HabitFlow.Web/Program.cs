using System.Threading.RateLimiting;
using HabitFlow.Application;
using HabitFlow.Infrastructure;
using HabitFlow.Web.Components;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));
builder.Services.AddRazorComponents().AddInteractiveServerComponents(o => o.DetailedErrors = builder.Environment.IsDevelopment());
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidationProvider>();
builder.Services.AddAuthentication(o => { o.DefaultScheme = IdentityConstants.ApplicationScheme; o.DefaultSignInScheme = IdentityConstants.ExternalScheme; })
    .AddIdentityCookies();
builder.Services.AddIdentityCore<AppUser>(o =>
{
    o.User.RequireUniqueEmail = true; o.Password.RequiredLength = 10;
    o.Lockout.MaxFailedAccessAttempts = 5; o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/account/login"; o.AccessDeniedPath = "/denied"; o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax; o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = true;
});
builder.Services.AddAuthorization();
builder.Services.AddPooledDbContextFactory<AppDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IRoutineRepository, RoutineRepository>();
builder.Services.AddScoped<ITrackerService, TrackerService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<AppState>(); builder.Services.AddScoped<ToastService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProgressNotifier, SignalRProgressNotifier>();
builder.Services.AddSignalR(); builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self' ws: wss:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});
app.UseStaticFiles(); app.UseRouting(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization(); app.UseAntiforgery();
app.MapAccountEndpoints();
app.MapHub<ProgressHub>("/hubs/progress").RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
await using (var scope = app.Services.CreateAsyncScope()) await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().RunAsync();
await app.RunAsync();
public partial class Program;
