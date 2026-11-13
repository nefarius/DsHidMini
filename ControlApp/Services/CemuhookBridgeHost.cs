using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>Owns the bridge for one device. Update and Dispose run on the device view model's UI thread.</summary>
internal sealed class CemuhookBridgeHost(
    Func<CemuhookUdpSettings, MotionControlsSettings, CemuhookBridgeService?> start) : IDisposable
{
    private CemuhookBridgeService? _service;
    private string? _activeIp;
    private int _activePort;
    private bool _disposed;
    private string? _startError;

    // Worker completion can raise this event on a background thread; UI subscribers must dispatch it.
    public event EventHandler? StatusChanged;

    public string? ErrorMessage
    {
        get
        {
            var service = _service;
            return _startError ?? (service?.Completion.IsFaulted == true
                ? $"The Cemuhook UDP server stopped: {service.Completion.Exception!.GetBaseException().Message}"
                : null);
        }
    }

    public void Update(CemuhookUdpSettings udp, MotionControlsSettings motion)
    {
        if (_disposed)
            return;

        string? validation = udp.Enabled ? CemuhookListenAddresses.ValidateEndpoint(udp.IpAddress, udp.Port) : null;
        if (_service != null && (!udp.Enabled || validation != null || _service.Completion.IsCompleted ||
                                 udp.IpAddress != _activeIp || udp.Port != _activePort))
        {
            Stop();
        }

        _startError = validation;
        if (udp.Enabled && validation == null && _service == null)
        {
            try
            {
                _service = start(udp, motion);
                _activeIp = udp.IpAddress;
                _activePort = udp.Port;
                if (_service == null)
                {
                    _startError = "The Cemuhook UDP server could not start. Check that the controller and driver are available.";
                }
                else
                {
                    _ = _service.Completion.ContinueWith(
                        _ => StatusChanged?.Invoke(this, EventArgs.Empty),
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
            }
            catch (Exception ex)
            {
                _startError = FormatStartError(ex, udp);
            }
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _disposed = true;
        Stop();
        _startError = null;
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

    private void Stop()
    {
        _service?.Dispose();
        _service = null;
        _activeIp = null;
        _activePort = 0;
    }
}
