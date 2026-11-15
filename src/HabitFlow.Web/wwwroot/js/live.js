export function connect(dotnet) {
    let stopped = false, timer, refreshTimer;
    const connection = new signalR.HubConnectionBuilder().withUrl('/hubs/progress').withAutomaticReconnect([0, 2000, 5000, 10000, 30000]).build();
    const call = (name, ...args) => { if (!stopped) dotnet.invokeMethodAsync(name, ...args).catch(() => {}); };
    connection.on('ProgressChanged', () => { clearTimeout(refreshTimer); refreshTimer = setTimeout(() => call('Refresh'), 200); });
    connection.onreconnecting(() => call('ConnectionChanged', false));
    connection.onreconnected(() => { call('ConnectionChanged', true); call('Refresh'); });
    connection.onclose(() => { if (!stopped) { call('ConnectionChanged', false); timer = setTimeout(start, 10000); } });
    async function start() {
        if (stopped) return;
        try { await connection.start(); call('ConnectionChanged', true); call('Refresh'); }
        catch { call('ConnectionChanged', false); if (!stopped) timer = setTimeout(start, 10000); }
    }
    start();
    return { async stop() { stopped = true; clearTimeout(timer); clearTimeout(refreshTimer); await connection.stop(); } };
}
