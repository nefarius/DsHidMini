using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace Nefarius.DsHidMini.IPC.Util;

internal sealed unsafe class Overlay : IDisposable
{
    private readonly byte* _pointer;
    private readonly MemoryMappedViewAccessor _view;

    public Overlay(MemoryMappedViewAccessor view)
    {
        _view = view;
        view.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
    }

    public void Dispose()
    {
        _view.SafeMemoryMappedViewHandle.ReleasePointer();
    }

    public ref T As<T>() where T : struct
    {
        return ref Unsafe.AsRef<T>(_pointer);
    }
}