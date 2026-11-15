using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Components;
namespace HabitFlow.Web.Components.Shared;
public abstract class TrackerPage : ComponentBase, IDisposable
{
    [Inject] protected AppState State { get; set; } = default!;
    [Inject] protected HabitFlow.Application.ITrackerService Tracker { get; set; } = default!;
    [Inject] protected ToastService Toasts { get; set; } = default!;
    protected bool IsInteractive { get; private set; }
    protected override void OnAfterRender(bool firstRender)
    { if (firstRender) { IsInteractive = true; StateHasChanged(); } }
    protected override async Task OnInitializedAsync()
    { State.Changed += Update; if (State.Data is null) await State.RefreshAsync(); }
    private void Update() => _ = InvokeAsync(StateHasChanged);
    protected async Task<bool> Act(Func<Task> action, string message)
    {
        var success = await State.MutateAsync(action);
        Toasts.Show(success ? message : State.Error ?? "Please retry.", !success); return success;
    }
    public void Dispose() { State.Changed -= Update; GC.SuppressFinalize(this); }
}
