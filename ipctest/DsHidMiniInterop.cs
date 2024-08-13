using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

using Nefarius.DsHidMini.IPC.Models;

namespace Nefarius.DsHidMini.IPC;

public sealed class DsHidMiniInteropExclusiveAccessException : Exception
{
    internal DsHidMiniInteropExclusiveAccessException() : base("") { }
}

public sealed class DsHidMiniInteropUnavailableException : Exception
{
    internal DsHidMiniInteropUnavailableException() : base("") { }
}

public class DsHidMiniInterop : IDisposable
{
    private const string FileMapName = "Global\\DsHidMiniSharedMemory";
    private const string ReadEventName = "Global\\DsHidMiniReadEvent";
    private const string WriteEventName = "Global\\DsHidMiniWriteEvent";
    private const int BufferSize = 4096;
    private const string MutexName = "Global\\DsHidMiniMutex";

    private readonly Mutex _connectionMutex;
    private readonly EventWaitHandle _readEvent;
    private readonly EventWaitHandle _writeEvent;
    private readonly MemoryMappedFile _mappedFile;
    private readonly MemoryMappedViewAccessor _accessor;

    public DsHidMiniInterop()
    {
        try
        {
            _connectionMutex = Mutex.OpenExisting(MutexName);

            //
            // Mutex already claimed by someone else, can't continue
            // 
            if (!_connectionMutex.WaitOne(0))
            {
                throw new DsHidMiniInteropExclusiveAccessException();
            }

            _readEvent = EventWaitHandle.OpenExisting(ReadEventName);
            _writeEvent = EventWaitHandle.OpenExisting(WriteEventName);
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        _mappedFile = MemoryMappedFile.OpenExisting(FileMapName);
        _accessor = _mappedFile.CreateViewAccessor(0, BufferSize);
    }

    public unsafe void SendPing()
    {
        byte* buffer = null;

        try
        {
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref buffer);

            DSHM_IPC_MSG_HEADER* message = (DSHM_IPC_MSG_HEADER*)buffer;

            // TODO: implement me

            
        }
        finally
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            SignalWriteFinished();
        }
    }

    private void SignalReadFinished()
    {
        _writeEvent.Set();
    }

    private void SignalWriteFinished()
    {
        _readEvent.Set();
    }

    public static bool IsAvailable
    {
        get
        {
            try
            {
                using Mutex mutex = Mutex.OpenExisting(MutexName);

                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
        }
    }

    public void Dispose()
    {
        _accessor.Dispose();
        _mappedFile.Dispose();

        _connectionMutex.ReleaseMutex();
        _connectionMutex.Dispose();
    }
}