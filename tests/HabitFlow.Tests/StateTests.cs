using HabitFlow.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
namespace HabitFlow.Tests;
public sealed class StateTests
{
    [Fact]
    public async Task RefreshPublishesBusyThenLoadedState()
    {
        var fake = new FakeTracker(); using var state = new AppState(fake, NullLogger<AppState>.Instance);
        var snapshots = new List<bool>(); state.Changed += () => snapshots.Add(state.IsBusy);
        Assert.True(await state.RefreshAsync()); Assert.Equal([true, false], snapshots); Assert.NotNull(state.Data);
    }
    [Fact]
    public async Task ConcurrentRefreshesAreSerialized()
    {
        var fake = new FakeTracker(); using var state = new AppState(fake, NullLogger<AppState>.Instance);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => state.RefreshAsync())); Assert.Equal(8, fake.Loads); Assert.False(state.IsBusy);
    }
    [Fact]
    public async Task MutationRefreshesAndValidationPreservesLastSnapshot()
    {
        var fake = new FakeTracker(); using var state = new AppState(fake, NullLogger<AppState>.Instance);
        await state.RefreshAsync(); var before = state.Data; fake.Fail = true;
        Assert.False(await state.MutateAsync(() => Task.CompletedTask)); Assert.Same(before, state.Data); Assert.Equal("Expected validation failure.", state.Error);
    }
    [Fact]
    public async Task DisposedStateDoesNotLoad()
    {
        var fake = new FakeTracker(); var state = new AppState(fake, NullLogger<AppState>.Instance); state.Dispose();
        Assert.False(await state.RefreshAsync()); Assert.Equal(0, fake.Loads);
    }
    [Fact]
    public void ToastNotifiesAndCanBeDismissed()
    {
        var toasts = new ToastService(); var count = 0; toasts.Changed += () => count++;
        toasts.Show("Saved"); Assert.Equal("Saved", toasts.Current?.Text); toasts.Dismiss(); Assert.Null(toasts.Current); Assert.Equal(2, count);
    }
}
