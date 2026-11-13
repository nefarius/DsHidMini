using System.Collections.Concurrent;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>Application-wide UDP listener with independent per-controller readers and stable DSU slots.</summary>
public sealed class CemuhookBridgeService : IDisposable
{
    private sealed record Controller(CemuhookControllerIdentity Identity, CemuhookControllerRegistry.Lease Lease,
        Func<CemuhookIpcBinding?> Resolve, CemuhookMotionReader Reader);
    private readonly ConcurrentDictionary<object, Controller> _controllers = new();
    private readonly ConcurrentDictionary<object, string> _errors = new();
    private readonly CemuhookControllerRegistry _registry = new();
    private readonly CemuhookBridgeHost _host;
    private readonly Func<CemuhookUdpSettings> _endpoint;
    private readonly Func<string, int?> _preferred;
    private readonly Action<string, int> _remember;
    private readonly Func<CemuhookIpcBinding, ICemuhookMotionSource> _open;
    private bool _disposed;
    public event EventHandler? StatusChanged;

    public CemuhookBridgeService(DshmConfigManager configuration)
        : this(() => configuration.SharedCemuhookUdp, configuration.GetCemuhookSlot,
            configuration.RememberCemuhookSlot, binding => new CemuhookMotionSource(binding.Slot))
    {
        configuration.DshmConfigurationUpdated += OnConfigurationChanged;
        _configuration = configuration;
    }
    private readonly DshmConfigManager? _configuration;
    private void OnConfigurationChanged(object? sender, EventArgs args)
    {
        if (args is not DshmConfigManager.DshmUpdatedEventArgs { UpdatedSuccessfully: false }) UpdateEndpoint();
    }

    internal CemuhookBridgeService(Func<CemuhookUdpSettings> endpoint, Func<string, int?> preferred,
        Action<string, int> remember, Func<CemuhookIpcBinding, ICemuhookMotionSource> open)
    {
        _endpoint = endpoint; _preferred = preferred; _remember = remember; _open = open;
        _ = _endpoint();
        _host = new(settings => new CemuhookServer(settings.Port, settings.IpAddress, _registry.Get));
        _host.StatusChanged += (_, _) => StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    internal string? GetError(object owner) => _errors.TryGetValue(owner, out string? error) ? error
        : _controllers.TryGetValue(owner, out Controller? entry) && entry.Reader.Completion.IsFaulted
            ? $"Controller motion stopped: {entry.Reader.Completion.Exception!.GetBaseException().Message}"
            : _host.ErrorMessage;
    internal int? GetSlot(object owner) => _controllers.TryGetValue(owner, out Controller? entry) ? entry.Lease.Slot : null;
    internal CemuhookPadState GetPad(byte slot) => _registry.Get(slot);

    internal void UpdateController(object owner, CemuhookControllerIdentity identity, bool enabled, byte connectionType,
        MotionControlsSettings settings, Func<CemuhookIpcBinding?> resolve)
    {
        if (_disposed) return;
        _errors.TryRemove(owner, out _);
        if (!enabled) { RemoveController(owner); return; }
        if (_controllers.TryGetValue(owner, out Controller? current))
        {
            if (current.Identity == identity && !current.Reader.Completion.IsCompleted)
            {
                _registry.SetConnection(current.Lease, connectionType);
                UpdateEndpoint();
                return;
            }
            RemoveReader(owner);
        }
        foreach (var other in _controllers.Where(pair => pair.Value.Identity.Key == identity.Key).ToArray())
        {
            if (other.Value.Resolve()?.ControllerKey == identity.Key)
            {
                _errors[owner] = "This controller is already registered through another connection.";
                StatusChanged?.Invoke(this, EventArgs.Empty);
                return;
            }
            RemoveReader(other.Key);
        }
        try
        {
            var lease = _registry.Attach(identity, connectionType, _preferred(identity.Key));
            CemuhookMotionReader reader;
            try
            {
                reader = new(identity.Key, settings, resolve, _open, snapshot =>
                {
                    if (_registry.Publish(lease, snapshot)) _host.NotifySnapshotAvailable(lease.Slot);
                });
            }
            catch { _registry.Detach(lease); throw; }
            _controllers[owner] = new(identity, lease, resolve, reader);
            _ = reader.Completion.ContinueWith(_ => StatusChanged?.Invoke(this, EventArgs.Empty),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            try { _remember(identity.Key, lease.Slot); }
            catch (Exception error) { Log.Logger.Warning(error, "Could not save the preferred Cemuhook slot."); }
        }
        catch (Exception error) { _errors[owner] = error.Message; }
        UpdateEndpoint();
    }

    internal void RemoveController(object owner)
    {
        _errors.TryRemove(owner, out _);
        RemoveReader(owner);
        if (!_disposed) UpdateEndpoint();
    }
    private void RemoveReader(object owner)
    {
        if (!_controllers.TryRemove(owner, out Controller? entry)) return;
        _registry.Detach(entry.Lease);
        _host.NotifySnapshotAvailable(entry.Lease.Slot);
        entry.Reader.Dispose();
    }
    internal void UpdateEndpoint()
    {
        if (!_disposed) _host.Update(_endpoint(), !_controllers.IsEmpty);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_configuration != null) _configuration.DshmConfigurationUpdated -= OnConfigurationChanged;
        foreach (object owner in _controllers.Keys.ToArray()) RemoveReader(owner);
        _host.Dispose(); _errors.Clear();
    }
}
