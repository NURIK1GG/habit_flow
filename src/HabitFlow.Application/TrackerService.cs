using System.ComponentModel.DataAnnotations;
using HabitFlow.Domain;
namespace HabitFlow.Application;
public sealed class TrackerService(IRoutineRepository repository, ICurrentUser user, TimeProvider clock,
    IProgressNotifier notifier) : ITrackerService
{
    private DateOnly Today => TrackerCalendar.Today(clock);
    private static void Validate(object input)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), results, true))
            throw new RuleException(string.Join(" ", results.Select(r => r.ErrorMessage)));
    }
    public async Task<DashboardDto> LoadAsync(CancellationToken ct = default)
    {
        var id = await user.GetIdAsync(); var today = Today;
        var routines = await repository.ListAsync(id, ct);
        var start = today.AddDays(-27);
        var history = routines.SelectMany(r => r.CheckIns.Select(c => new HistoryDto(c.Id, r.Id, r.Title, c.Date, c.Minutes, c.Notes)))
            .OrderByDescending(c => c.Date).ThenBy(c => c.Title).ToArray();
        var days = Enumerable.Range(0, 28).Select(i => start.AddDays(i)).Select(d => new DayDto(d,
            history.Count(c => c.Date == d), routines.Count(r => !r.Archived && r.IsScheduled(d)),
            history.Where(c => c.Date == d).Sum(c => c.Minutes))).ToArray();
        return new(routines.Select(r => new RoutineDto(r.Id, r.Title, r.Description, r.Kind, r.TargetMinutes,
            r.DaysOfWeek, r.Archived, r.Revision, r.CreatedOn, StreakCalculator.Current(r, today),
            StreakCalculator.Best(r, today), r.CheckIns.Any(c => c.Date == today),
            r.Steps.OrderBy(s => s.Position).Select(s => new StepDto(s.Name, s.Sets, s.Repetitions)).ToArray())).ToArray(),
            history, days, await repository.LeadersAsync(id, new DateOnly(today.Year, today.Month, 1), ct),
            await repository.ProfileAsync(id, ct));
    }
    public async Task SaveRoutineAsync(RoutineEdit edit, CancellationToken ct = default)
    {
        edit.Title = (edit.Title ?? "").Trim(); Validate(edit);
        if (edit.Steps.Count > 20) throw new RuleException("A routine can contain at most 20 exercises.");
        foreach (var step in edit.Steps) { step.Name = (step.Name ?? "").Trim(); Validate(step); }
        var owner = await user.GetIdAsync();
        var existing = edit.Id is null ? null : (await repository.ListAsync(owner, ct)).SingleOrDefault(r => r.Id == edit.Id);
        if (edit.Id is not null && existing is null) throw new RuleException("Routine not found.");
        if (existing is not null && (existing.DaysOfWeek != edit.DaysOfWeek || existing.Kind != edit.Kind) && existing.CheckIns.Count > 0)
            throw new RuleException("Schedule and type are fixed after the first check-in to preserve historical streaks. Create a new routine instead.");
        var routine = new Routine
        {
            Id = edit.Id ?? Guid.NewGuid(),
            OwnerId = owner,
            Title = edit.Title,
            Description = (edit.Description ?? "").Trim(),
            Kind = edit.Kind,
            TargetMinutes = edit.TargetMinutes,
            DaysOfWeek = edit.DaysOfWeek,
            CreatedOn = existing?.CreatedOn ?? Today,
            Revision = edit.Revision,
            Archived = existing?.Archived ?? false,
            Steps = edit.Steps.Select((s, i) => new RoutineStep { Name = s.Name, Sets = s.Sets, Repetitions = s.Repetitions, Position = i }).ToList()
        };
        await repository.SaveAsync(owner, routine, edit.Id is null, ct);
        await notifier.ChangedAsync();
    }
    public async Task SetArchivedAsync(Guid id, bool archived, CancellationToken ct = default)
    { await repository.SetArchivedAsync(await user.GetIdAsync(), id, archived, ct); await notifier.ChangedAsync(); }
    public async Task CheckInAsync(CheckInEdit edit, CancellationToken ct = default)
    {
        Validate(edit); var owner = await user.GetIdAsync();
        var routine = (await repository.ListAsync(owner, ct)).SingleOrDefault(r => r.Id == edit.RoutineId)
            ?? throw new RuleException("Routine not found.");
        if (routine.Archived) throw new RuleException("Restore this routine before checking in.");
        if (edit.Date > Today || edit.Date < Today.AddDays(-365) || !routine.IsScheduled(edit.Date))
            throw new RuleException("Choose a scheduled day within the last year, on or after the routine's creation.");
        await repository.AddCheckInAsync(owner, new CheckIn
        {
            RoutineId = routine.Id,
            Date = edit.Date,
            Minutes = edit.Minutes,
            Notes = (edit.Notes ?? "").Trim()
        }, ct);
        await notifier.ChangedAsync();
    }
    public async Task UndoAsync(Guid checkInId, CancellationToken ct = default)
    { await repository.RemoveCheckInAsync(await user.GetIdAsync(), checkInId, ct); await notifier.ChangedAsync(); }
    public async Task SaveProfileAsync(ProfileEdit edit, CancellationToken ct = default)
    {
        edit.DisplayName = (edit.DisplayName ?? "").Trim(); Validate(edit);
        await repository.SaveProfileAsync(await user.GetIdAsync(), edit, ct); await notifier.ChangedAsync();
    }
}
