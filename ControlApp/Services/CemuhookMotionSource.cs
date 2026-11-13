using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Services;

internal interface ICemuhookMotionSource : IDisposable
{
    bool TryRead(out DsMotionSnapshot snapshot, TimeSpan timeout);
}

internal sealed class CemuhookMotionSource : ICemuhookMotionSource
{
    private readonly DsHidMiniInterop _interop;
    private readonly int _deviceSlot;

    public CemuhookMotionSource(int deviceSlot)
    {
        _deviceSlot = deviceSlot;
        _interop = new DsHidMiniInterop();
        if (!_interop.HasMotionTelemetry)
        {
            _interop.Dispose();
            throw new InvalidOperationException("The driver does not provide motion telemetry.");
        }
    }

    public bool TryRead(out DsMotionSnapshot snapshot, TimeSpan timeout) =>
        _interop.GetMotionSnapshot(_deviceSlot, out snapshot, timeout);

    public void Dispose() => _interop.Dispose();
}
