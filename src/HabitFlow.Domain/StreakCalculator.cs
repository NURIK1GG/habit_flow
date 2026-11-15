namespace HabitFlow.Domain;
public static class StreakCalculator
{
    public static int Current(Routine routine, DateOnly today)
    {
        var dates = routine.CheckIns.Select(c => c.Date).ToHashSet();
        var count = 0;
        // Today may still be completed later, so an unfinished today doesn't break yesterday's streak.
        for (var day = today; day >= routine.CreatedOn; day = day.AddDays(-1))
        {
            if (!routine.IsScheduled(day)) continue;
            if (dates.Contains(day)) count++;
            else if (day != today) break;
        }
        return count;
    }
    public static int Best(Routine routine, DateOnly today)
    {
        var dates = routine.CheckIns.Select(c => c.Date).ToHashSet();
        var best = 0; var running = 0;
        for (var day = routine.CreatedOn; day <= today; day = day.AddDays(1))
        {
            if (!routine.IsScheduled(day)) continue;
            running = dates.Contains(day) ? running + 1 : 0;
            best = Math.Max(best, running);
        }
        return best;
    }
}
