using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HabitFlow.Application;
namespace HabitFlow.Web.Services;
[Authorize]
public sealed class ProgressHub : Hub;
public sealed class SignalRProgressNotifier(IHubContext<ProgressHub> hub, ILogger<SignalRProgressNotifier> logger) : IProgressNotifier
{
    public async Task ChangedAsync()
    {
        try { await hub.Clients.All.SendAsync("ProgressChanged"); }
        catch (Exception ex) { logger.LogWarning(ex, "Data committed, but live notification delivery failed"); }
    }
}
