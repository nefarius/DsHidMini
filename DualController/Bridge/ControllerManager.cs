namespace DualController.Bridge;

internal sealed class ControllerManager : IAsyncDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private readonly Dictionary<string, ControllerSession> sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> retryAfter = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ControllerStatus> lastError = new(StringComparer.OrdinalIgnoreCase);
    private ControllerStatus[] snapshot = [];
    private string scanStatus = "Looking for physical PS4 controllers…";
    private readonly Task worker;
    private int enabled = 1;
    public bool Enabled { get => Volatile.Read(ref enabled) == 1; set => Volatile.Write(ref enabled, value ? 1 : 0); }
    public ControllerStatus[] Snapshot => Volatile.Read(ref snapshot);
    public string ScanStatus => Volatile.Read(ref scanStatus);

    public ControllerManager() => worker = Task.Run(ScanAsync);

    private async Task ScanAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            do
            {
                List<HidDevice> devices;
                try { devices = HidDevices.Enumerate(); }
                catch (Exception ex)
                {
                    // Stop mapping if enumeration is no longer trustworthy.
                    await StopSessionsAsync().ConfigureAwait(false);
                    Volatile.Write(ref snapshot, []);
                    Volatile.Write(ref scanStatus, $"Device scan failed: {ex.Message}");
                    Log.Write(ex.ToString());
                    continue;
                }
                foreach (string path in sessions.Keys.ToArray())
                {
                    var session = sessions[path];
                    if (!Enabled || session.Completed || !devices.Any(d => d.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (session.Completed)
                        {
                            retryAfter[path] = DateTimeOffset.UtcNow.AddSeconds(5);
                            lastError[path] = session.Status;
                        }
                        await session.DisposeAsync().ConfigureAwait(false);
                        sessions.Remove(path);
                    }
                }
                foreach (var device in devices)
                {
                    if (!Enabled || sessions.ContainsKey(device.Path) ||
                        (retryAfter.TryGetValue(device.Path, out var retry) && DateTimeOffset.UtcNow < retry)) continue;
                    var session = new ControllerSession(device);
                    sessions.Add(device.Path, session); lastError.Remove(device.Path); session.Start();
                }
                foreach (string path in retryAfter.Keys.ToArray())
                    if (!devices.Any(d => d.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    { retryAfter.Remove(path); lastError.Remove(path); }
                Volatile.Write(ref snapshot, devices.Select(d => sessions.TryGetValue(d.Path, out var session)
                    ? session.Status : Enabled && lastError.TryGetValue(d.Path, out var error) ? error
                        : new ControllerStatus($"PS4 ({d.Product:X4})", d.Bluetooth ? "Bluetooth" : "USB",
                            Enabled ? "Retrying in a few seconds…" : "Native PS4 input (Xbox mapping paused)", "—", "—")).ToArray());
                Volatile.Write(ref scanStatus, devices.Count == 0
                    ? "No physical PS4 detected. Connect USB or pair Wireless Controller in Windows Bluetooth settings."
                    : $"{devices.Count} physical PS4 controller(s) detected. PS3 is handled separately by DsHidMini.");
            } while (await timer.WaitForNextTickAsync(lifetime.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally { await StopSessionsAsync().ConfigureAwait(false); }
    }

    private async Task StopSessionsAsync()
    {
        foreach (var session in sessions.Values) await session.DisposeAsync().ConfigureAwait(false);
        sessions.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        lifetime.Cancel();
        await worker.ConfigureAwait(false);
        lifetime.Dispose();
    }
}
