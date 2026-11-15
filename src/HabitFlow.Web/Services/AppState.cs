using HabitFlow.Application;
namespace HabitFlow.Web.Services;
public sealed class AppState : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource lifetime = new();
    private readonly ITrackerService service;
    private readonly ILogger<AppState> logger;
    private readonly CancellationToken token;
    private bool disposed;
    public AppState(ITrackerService service, ILogger<AppState> logger)
    { this.service = service; this.logger = logger; token = lifetime.Token; }
    public DashboardDto? Data { get; private set; }
    public bool IsBusy { get; private set; }
    public string? Error { get; private set; }
    public event Action? Changed;
    public Task<bool> RefreshAsync() => RunAsync(null);
    public Task<bool> MutateAsync(Func<Task> mutation) => RunAsync(mutation);
    private async Task<bool> RunAsync(Func<Task>? mutation)
    {
        if (disposed) return false;
        try { await gate.WaitAsync(token); }
        catch (OperationCanceledException) { return false; }
        try
        {
            if (disposed) return false;
            IsBusy = true; Error = null; Changed?.Invoke();
            if (mutation is not null) await mutation();
            Data = await service.LoadAsync(token); return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return false; }
        catch (Exception ex)
        {
            Error = ex is RuleException or ConflictException or UnauthorizedAccessException ? ex.Message
                : "We couldn't finish that request. Your latest data may need a refresh. Please try again.";
            logger.LogWarning(ex, "Tracker operation failed"); return false;
        }
        finally { IsBusy = false; gate.Release(); if (!disposed) Changed?.Invoke(); }
    }
    public void Dispose() { if (disposed) return; disposed = true; lifetime.Cancel(); lifetime.Dispose(); }
}
public sealed record ToastMessage(string Text, bool IsError = false);
public sealed class ToastService
{
    public ToastMessage? Current { get; private set; }
    public event Action? Changed;
    public void Show(string text, bool error = false) { Current = new(text, error); Changed?.Invoke(); }
    public void Dismiss() { Current = null; Changed?.Invoke(); }
}
public sealed class UiSession { public string DisplayName { get; set; } = "Your workspace"; }
