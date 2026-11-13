using System.Net.Sockets;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>Lifecycle and errors of the single shared UDP listener. Updated on the UI thread.</summary>
internal sealed class CemuhookBridgeHost(Func<CemuhookUdpSettings, CemuhookServer> start) : IDisposable
{
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

    public void Update(CemuhookUdpSettings endpoint, bool enabled)
    {
        if (_disposed) return;
        string? validation = enabled ? CemuhookListenAddresses.ValidateEndpoint(endpoint.IpAddress, endpoint.Port) : null;
        if (_server != null && (!enabled || validation != null || _completion!.IsCompleted ||
            endpoint.IpAddress != _activeIp || endpoint.Port != _activePort)) Stop();
        _startError = validation;
        if (enabled && validation == null && _server == null)
        {
            try
            {
                _server = start(endpoint);
                _cts = new();
                CancellationToken token = _cts.Token;
                CemuhookServer server = _server;
                _completion = Task.Run(() => server.RunAsync(token));
                _activeIp = endpoint.IpAddress;
                _activePort = endpoint.Port;
                _ = _completion.ContinueWith(task =>
                {
                    if (task.IsFaulted) Log.Logger.Error(task.Exception, "Shared Cemuhook server failed.");
                    StatusChanged?.Invoke(this, EventArgs.Empty);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            catch (Exception error)
            {
                Stop();
                _startError = FormatStartError(error, endpoint);
            }
        }
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() { _disposed = true; Stop(); _startError = null; }
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
