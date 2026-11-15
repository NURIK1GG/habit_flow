using System.ComponentModel.DataAnnotations;
using HabitFlow.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
namespace HabitFlow.Web.Services;
public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (HttpContext context, IAntiforgery antiforgery, SignInManager<AppUser> signIn) =>
        {
            if (!await ValidFormAsync(antiforgery, context)) return Results.BadRequest("Your form expired. Reload the page and try again.");
            var form = await context.Request.ReadFormAsync();
            var email = form["email"].ToString().Trim(); var password = form["password"].ToString();
            if (email.Length > 254 || password.Length > 128) return Failure("login", "Invalid email or password.");
            var result = await signIn.PasswordSignInAsync(email, password, false, true);
            return result.Succeeded ? Results.LocalRedirect("/") : Failure("login", "Invalid credentials, or the account is temporarily suspended. Please try again later.");
        }).AllowAnonymous().RequireRateLimiting("account");
        app.MapPost("/auth/register", async (HttpContext context, IAntiforgery antiforgery, UserManager<AppUser> users, SignInManager<AppUser> signIn) =>
        {
            if (!await ValidFormAsync(antiforgery, context)) return Results.BadRequest("Your form expired. Reload the page and try again.");
            var form = await context.Request.ReadFormAsync();
            var email = form["email"].ToString().Trim(); var name = form["name"].ToString().Trim(); var password = form["password"].ToString();
            if (!new EmailAddressAttribute().IsValid(email) || email.Length > 254 || name.Length is < 2 or > 40 || password.Length is < 10 or > 128)
                return Failure("register", "Use a valid email, a 2–40 character name and a 10–128 character password.");
            var user = new AppUser { Email = email, UserName = email, DisplayName = name, IsPublic = false };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) return Failure("register", "Registration failed. Use a unique email and a password with uppercase, lowercase, a number and a symbol.");
            var role = await users.AddToRoleAsync(user, "User");
            if (!role.Succeeded) { await users.DeleteAsync(user); return Failure("register", "Account setup failed. Please retry."); }
            await signIn.SignInAsync(user, false); return Results.LocalRedirect("/");
        }).AllowAnonymous().RequireRateLimiting("account");
        app.MapPost("/auth/logout", async (HttpContext context, IAntiforgery antiforgery, SignInManager<AppUser> signIn) =>
        {
            if (!await ValidFormAsync(antiforgery, context)) return Results.BadRequest("Your form expired. Reload the page and try again."); await signIn.SignOutAsync(); return Results.LocalRedirect("/account/login");
        }).RequireAuthorization();
    }
    private static async Task<bool> ValidFormAsync(IAntiforgery antiforgery, HttpContext context)
    {
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
    private static IResult Failure(string page, string message) => Results.LocalRedirect($"/account/{page}?error={Uri.EscapeDataString(message)}");
}
