using Microsoft.AspNetCore.Identity;
namespace HabitFlow.Infrastructure;
public sealed class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public bool IsPublic { get; set; }
    public int WeeklyGoal { get; set; } = 5;
}
