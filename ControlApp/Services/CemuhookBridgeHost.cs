using System.Net.Sockets;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>Serialized lifecycle and errors of the single shared UDP listener.</summary>
internal sealed class CemuhookBridgeHost(Func<CemuhookUdpSettings, CemuhookServer> start) : IDisposable
{
    private readonly object _lifecycleGate = new();
    private CemuhookServer? _server;
    private CancellationTokenSource? _cts;
    private Task? _completion;
    private string? _activeIp;
    private int _activePort;
    private bool _disposed;
    private string? _startError;
    public event EventHandler? StatusChanged;
    public string? ErrorMessage
    {
        get
        {
            Task? completion = _completion;
            return _startError ?? (completion?.IsFaulted == true
                ? $"The Cemuhook UDP server stopped: {completion.Exception!.GetBaseException().Message}" : null);
        }
    }
    public void NotifySnapshotAvailable(byte? slot = null) => _server?.NotifySnapshotAvailable(slot);

    public bool Update(CemuhookUdpSettings endpoint, bool enabled)
    {
        bool success;
        lock (_lifecycleGate) success = UpdateCore(endpoint, enabled);
        StatusChanged?.Invoke(this, EventArgs.Empty);
        return success;
    }

    private bool UpdateCore(CemuhookUdpSettings endpoint, bool enabled)
    {
        if (_disposed) return false;
        if (!enabled) { Stop(); _startError = null; return true; }
        _startError = CemuhookListenAddresses.ValidateEndpoint(endpoint.IpAddress, endpoint.Port);
        if (_startError != null) return false;
        if (_completion?.IsCompleted == true) Stop();
        if (_server != null && endpoint.IpAddress == _activeIp && endpoint.Port == _activePort) return true;

        // Address changes on the same port can overlap the old socket's binding.
        // Restore it if the replacement cannot bind; other ports can be staged first.
        CemuhookUdpSettings? previous = _server != null && endpoint.Port == _activePort
            ? new() { IpAddress = _activeIp!, Port = _activePort } : null;
        if (previous != null) Stop();
        try
        {
            CemuhookServer replacement = start(endpoint);
            Stop();
            Start(replacement, endpoint);
            return true;
        }
        catch (Exception error)
        {
            _startError = FormatStartError(error, endpoint);
            if (previous != null)
            {
                try { Start(start(previous), previous); }
                catch (Exception restoreError)
                {
                    Stop();
                    _startError += $" Could not restore the previous listener: {restoreError.Message}";
                }
            }
            return false;
        }
    }

    private void Start(CemuhookServer server, CemuhookUdpSettings endpoint)
    {
        _server = server;
        _cts = new();
        CancellationToken token = _cts.Token;
        _completion = Task.Run(() => server.RunAsync(token));
        _activeIp = endpoint.IpAddress;
        _activePort = endpoint.Port;
        _ = _completion.ContinueWith(task =>
        {
            if (task.IsFaulted) Log.Logger.Error(task.Exception, "Shared Cemuhook server failed.");
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    public void Dispose()
    {
        lock (_lifecycleGate) { _disposed = true; Stop(); _startError = null; }
    }
    private void Stop()
    {
        _cts?.Cancel();
        try { _completion?.GetAwaiter().GetResult(); } catch (Exception) { }
        _server?.Dispose(); _cts?.Dispose();
        _server = null; _cts = null; _completion = null; _activeIp = null; _activePort = 0;
    }

    private static string FormatStartError(Exception error, CemuhookUdpSettings udp) => error switch
    {
        SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse } =>
            $"UDP port {udp.Port} is already in use on {udp.IpAddress}. Choose a different port or stop the other server.",
        SocketException { SocketErrorCode: SocketError.AddressNotAvailable } =>
            $"The listen address {udp.IpAddress} is no longer available on this PC. Choose another address.",
        SocketException { SocketErrorCode: SocketError.AccessDenied } =>
            $"Access to {udp.IpAddress}:{udp.Port} was denied. Choose a different listen address or UDP port.",
        _ => $"Could not start the Cemuhook UDP server on {udp.IpAddress}:{udp.Port}: {error.Message}"
    };
}
