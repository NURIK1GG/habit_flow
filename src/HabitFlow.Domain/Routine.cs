namespace HabitFlow.Domain;
public enum RoutineKind { Habit, Workout }
public sealed class Routine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public RoutineKind Kind { get; set; }
    public int TargetMinutes { get; set; } = 15;
    // Bit zero is Sunday, bit six is Saturday. 127 = every day.
    public int DaysOfWeek { get; set; } = 127;
    public bool Archived { get; set; }
    public int Revision { get; set; }
    public DateOnly CreatedOn { get; set; }
    public List<RoutineStep> Steps { get; set; } = [];
    public List<CheckIn> CheckIns { get; set; } = [];
    public bool IsScheduled(DateOnly date) => date >= CreatedOn && (DaysOfWeek & (1 << (int)date.DayOfWeek)) != 0;
}
public sealed class RoutineStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoutineId { get; set; }
    public string Name { get; set; } = "";
    public int Sets { get; set; } = 3;
    public int Repetitions { get; set; } = 10;
    public int Position { get; set; }
}
public sealed class CheckIn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoutineId { get; set; }
    public DateOnly Date { get; set; }
    public int Minutes { get; set; }
    public string Notes { get; set; } = "";
}
