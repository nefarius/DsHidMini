using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>Owns the bridge for one device. Called on the device view model's UI thread.</summary>
internal sealed class CemuhookBridgeHost(
    Func<CemuhookUdpSettings, MotionControlsSettings, CemuhookBridgeService?> start) : IDisposable
{
    private CemuhookBridgeService? _service;
    private string? _activeIp;
    private int _activePort;
    private bool _disposed;

    public void Update(CemuhookUdpSettings udp, MotionControlsSettings motion)
    {
        if (_disposed)
            return;

        if (_service != null && (!udp.Enabled || _service.Completion.IsCompleted ||
                                 udp.IpAddress != _activeIp || udp.Port != _activePort))
        {
            Stop();
        }

        if (udp.Enabled && _service == null)
        {
            _service = start(udp, motion);
            _activeIp = udp.IpAddress;
            _activePort = udp.Port;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        Stop();
    }

    private void Stop()
    {
        _service?.Dispose();
        _service = null;
        _activeIp = null;
        _activePort = 0;
    }
}
