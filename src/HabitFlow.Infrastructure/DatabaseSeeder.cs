using HabitFlow.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
namespace HabitFlow.Infrastructure;
public sealed class DatabaseSeeder(AppDbContext db, UserManager<AppUser> users, RoleManager<IdentityRole> roles,
    IHostEnvironment environment, IConfiguration configuration, TimeProvider clock)
{
    public async Task RunAsync()
    {
        await db.Database.MigrateAsync();
        foreach (var role in new[] { "Admin", "User" })
            if (!await roles.RoleExistsAsync(role)) Ensure(await roles.CreateAsync(new IdentityRole(role)));
        if (!environment.IsDevelopment())
        {
            var email = configuration["Bootstrap:AdminEmail"];
            var password = configuration["Bootstrap:AdminPassword"];
            if (!string.IsNullOrWhiteSpace(email) && await users.FindByEmailAsync(email) is null)
            {
                if (string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Bootstrap:AdminPassword is required for a new administrator.");
                await CreateAsync(email, "Administrator", password, true, false);
            }
            return;
        }
        // Idempotent per user, no fixed accounts or passwords are created in Production.
        var demos = new[] { ("admin@habitflow.local", "Ayan Sapar", true), ("user@habitflow.local", "Arman Nurlan", false),
            ("maya@habitflow.local", "Aruzhan Serik", false), ("noah@habitflow.local", "Dias Alikhan", false),
            ("sara@habitflow.local", "Aigerim Bolat", false) };
        var today = TrackerCalendar.Today(clock);
        for (var index = 0; index < demos.Length; index++)
        {
            var (email, name, admin) = demos[index];
            if (await users.FindByEmailAsync(email) is { } existing)
            {
                var originalNames = new[] { "Alex Morgan", "Jamie Rivera", "Maya Chen", "Noah Wilson", "Sara Patel" };
                if (originalNames.Contains(existing.DisplayName)) { existing.DisplayName = name; Ensure(await users.UpdateAsync(existing)); }
                continue;
            }
            var user = await CreateAsync(email, name, "Demo!Pass123", admin, true);
            var routines = new[] {
                new Routine { OwnerId = user.Id, Title = "Morning mobility", Description = "Start the day with a clear mind and an open body.", Kind = RoutineKind.Workout, TargetMinutes = 20,
                    CreatedOn = today.AddDays(-41), Steps = [new() { Name = "World's greatest stretch", Sets = 2, Repetitions = 8, Position = 0 }, new() { Name = "Bodyweight squats", Sets = 3, Repetitions = 12, Position = 1 }] },
                new Routine { OwnerId = user.Id, Title = "Read & reflect", Description = "Read a chapter, then write down one idea to try.", Kind = RoutineKind.Habit, TargetMinutes = 15, CreatedOn = today.AddDays(-41) },
                new Routine { OwnerId = user.Id, Title = "Strength foundations", Description = "A balanced session for sustainable progress.", Kind = RoutineKind.Workout, TargetMinutes = 45, DaysOfWeek = 42, CreatedOn = today.AddDays(-41),
                    Steps = [new() { Name = "Goblet squat", Sets = 3, Repetitions = 10, Position = 0 }, new() { Name = "Push-up", Sets = 3, Repetitions = 8, Position = 1 }, new() { Name = "Dumbbell row", Sets = 3, Repetitions = 12, Position = 2 }] }
            };
            for (var day = 41; day > 0; day--)
                for (var r = 0; r < routines.Length; r++)
                    if (routines[r].IsScheduled(today.AddDays(-day)) && (day <= 4 || (day + index * 2 + r) % (5 + index) != 0))
                        routines[r].CheckIns.Add(new CheckIn
                        {
                            Date = today.AddDays(-day),
                            Minutes = routines[r].TargetMinutes + day % 4,
                            Notes = day % 7 == 0 ? "Felt stronger and more focused today." : ""
                        });
            db.Routines.AddRange(routines); await db.SaveChangesAsync();
        }
    }
    private async Task<AppUser> CreateAsync(string email, string name, string password, bool admin, bool isPublic)
    {
        var user = new AppUser { Email = email, UserName = email, DisplayName = name, EmailConfirmed = true, IsPublic = isPublic };
        Ensure(await users.CreateAsync(user, password)); Ensure(await users.AddToRoleAsync(user, "User"));
        if (admin) Ensure(await users.AddToRoleAsync(user, "Admin"));
        return user;
    }
    private static void Ensure(IdentityResult result)
    { if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description))); }
}
