using Bunit;
using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Components.Shared;
using HabitFlow.Web.Services;
using Microsoft.Extensions.DependencyInjection;
namespace HabitFlow.Tests;
public sealed class ComponentTests : BunitContext
{
    private static RoutineDto Routine(bool complete = false) => new(Guid.NewGuid(), "Morning mobility", "A gentle start", RoutineKind.Workout,
        20, 127, false, 0, new(2026, 11, 1), 3, 6, complete, [new("Squat", 3, 10)]);
    [Fact]
    public void CardRendersDataAndInvokesCallback()
    {
        Services.AddSingleton<TimeProvider>(new FixedClock()); RoutineDto? selected = null;
        var cut = Render<RoutineCard>(p => p.Add(c => c.Routine, Routine()).Add(c => c.OnComplete, (RoutineDto r) => selected = r));
        Assert.Contains("Morning mobility", cut.Markup); Assert.Contains("3 day streak", cut.Markup);
        cut.Find(".routine-actions > button").Click(); Assert.NotNull(selected);
    }
    [Fact]
    public void CompletedCardDisablesDuplicateCheckIn()
    {
        Services.AddSingleton<TimeProvider>(new FixedClock());
        var cut = Render<RoutineCard>(p => p.Add(c => c.Routine, Routine(true)));
        Assert.True(cut.Find(".routine-actions > button").HasAttribute("disabled"));
    }
    [Fact]
    public void ChartPeriodControlChangesAccessibleData()
    {
        var today = new DateOnly(2026, 11, 14);
        var cut = Render<ProgressChart>(p => p.Add(c => c.Days, Enumerable.Range(0, 28).Select(i => new DayDto(today.AddDays(-27 + i), 2, 3, 30)).ToArray()));
        Assert.Equal(14, cut.FindAll("tbody tr").Count); cut.FindAll(".segmented button")[0].Click(); Assert.Equal(7, cut.FindAll("tbody tr").Count);
        Assert.Contains("14 check-ins total", cut.Find("svg").GetAttribute("aria-label"));
    }
    [Fact]
    public void ToastDismissButtonClearsNotification()
    {
        var toasts = new ToastService(); Services.AddSingleton(toasts); toasts.Show("Recorded");
        var cut = Render<ToastHost>(); Assert.Contains("Recorded", cut.Markup); cut.Find("button").Click(); Assert.Empty(cut.Markup);
    }
    [Fact]
    public void LeaderboardDisplaysSelfWithoutEmailOrPrivateNotes()
    {
        var cut = Render<Leaderboard>(p => p.Add(c => c.Leaders, [new("Jamie", 20, 2, 30, true)]));
        Assert.Contains("Jamie", cut.Markup); Assert.Contains("you", cut.Markup); Assert.Contains("20", cut.Markup);
    }
    [Fact]
    public void RoutineFormBlocksInvalidSubmissionAndEmitsValidModel()
    {
        var module = JSInterop.SetupModule("./js/dialog.js");
        module.SetupVoid("open", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
        RoutineEdit? saved = null;
        var cut = Render<RoutineEditor>(p => p.Add(c => c.Model, new RoutineEdit())
            .Add(c => c.OnSave, (RoutineEdit edit) => saved = edit));
        cut.Find("form").Submit(); Assert.Null(saved); Assert.Contains("required", cut.Markup, StringComparison.OrdinalIgnoreCase);
        cut.Find("#routine-title").Change("Daily walk"); cut.Find("form").Submit();
        Assert.Equal("Daily walk", saved?.Title);
    }
}
