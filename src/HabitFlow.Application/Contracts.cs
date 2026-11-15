using System.ComponentModel.DataAnnotations;
using HabitFlow.Domain;
namespace HabitFlow.Application;
public sealed class RoutineEdit
{
    public Guid? Id { get; set; }
    public int Revision { get; set; }
    [Required, StringLength(80, MinimumLength = 2)] public string Title { get; set; } = "";
    [StringLength(500)] public string Description { get; set; } = "";
    [EnumDataType(typeof(RoutineKind))] public RoutineKind Kind { get; set; }
    [Range(1, 600)] public int TargetMinutes { get; set; } = 15;
    [Range(1, 127)] public int DaysOfWeek { get; set; } = 127;
    public List<StepEdit> Steps { get; set; } = [];
}
public sealed class StepEdit
{
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Range(1, 30)] public int Sets { get; set; } = 3;
    [Range(1, 500)] public int Repetitions { get; set; } = 10;
}
public sealed class CheckInEdit
{
    public Guid RoutineId { get; set; }
    public DateOnly Date { get; set; }
    [Range(1, 600)] public int Minutes { get; set; } = 15;
    [StringLength(300)] public string Notes { get; set; } = "";
}
public sealed record StepDto(string Name, int Sets, int Repetitions);
public sealed record RoutineDto(Guid Id, string Title, string Description, RoutineKind Kind, int TargetMinutes,
    int DaysOfWeek, bool Archived, int Revision, DateOnly CreatedOn, int CurrentStreak, int BestStreak,
    bool CompletedToday, IReadOnlyList<StepDto> Steps);
public sealed record HistoryDto(Guid Id, Guid RoutineId, string Title, DateOnly Date, int Minutes, string Notes);
public sealed record DayDto(DateOnly Date, int Completed, int Scheduled, int Minutes);
public sealed record LeaderboardDto(string DisplayName, int Points, int Completions, int Minutes, bool IsMe);
public sealed record ProfileDto(string DisplayName, bool IsPublic, int WeeklyGoal = 5);
public sealed record DashboardDto(IReadOnlyList<RoutineDto> Routines, IReadOnlyList<HistoryDto> History,
    IReadOnlyList<DayDto> Days, IReadOnlyList<LeaderboardDto> Leaders, ProfileDto Profile);
public sealed class ProfileEdit
{
    [Range(1, 100)] public int WeeklyGoal { get; set; } = 5;
    [Required, StringLength(40, MinimumLength = 2)] public string DisplayName { get; set; } = "";
    public bool IsPublic { get; set; }
}
public sealed record AdminUserDto(string Id, string Email, string DisplayName, bool IsAdmin, bool IsLocked, int Routines);
public sealed class RuleException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public interface ICurrentUser
{
    Task<string> GetIdAsync();
    Task RequireAdminAsync();
}
public interface IRoutineRepository
{
    Task<IReadOnlyList<Routine>> ListAsync(string ownerId, CancellationToken ct = default);
    Task SaveAsync(string ownerId, Routine routine, bool isNew, CancellationToken ct = default);
    Task SetArchivedAsync(string ownerId, Guid id, bool archived, CancellationToken ct = default);
    Task AddCheckInAsync(string ownerId, CheckIn checkIn, CancellationToken ct = default);
    Task RemoveCheckInAsync(string ownerId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LeaderboardDto>> LeadersAsync(string viewerId, DateOnly since, CancellationToken ct = default);
    Task<ProfileDto> ProfileAsync(string ownerId, CancellationToken ct = default);
    Task SaveProfileAsync(string ownerId, ProfileEdit profile, CancellationToken ct = default);
}
public interface IProgressNotifier { Task ChangedAsync(); }
public interface ITrackerService
{
    Task<DashboardDto> LoadAsync(CancellationToken ct = default);
    Task SaveRoutineAsync(RoutineEdit edit, CancellationToken ct = default);
    Task SetArchivedAsync(Guid id, bool archived, CancellationToken ct = default);
    Task CheckInAsync(CheckInEdit edit, CancellationToken ct = default);
    Task UndoAsync(Guid checkInId, CancellationToken ct = default);
    Task SaveProfileAsync(ProfileEdit edit, CancellationToken ct = default);
}
public interface IAdminService
{
    Task<IReadOnlyList<AdminUserDto>> UsersAsync();
    Task SetLockedAsync(string userId, bool locked);
}
