using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Nefarius.DsHidMini.IPC.Exceptions;
using Nefarius.DsHidMini.IPC.Models;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.IPC;

/// <summary>
///     Connects to the drivers shared memory region and keeps it locked to a single instance until disposed.
/// </summary>
public sealed class DsHidMiniInterop : IDisposable
{
    private const string FileMapName = "Global\\DsHidMiniSharedMemory";
    private const string ReadEventName = "Global\\DsHidMiniReadEvent";
    private const string WriteEventName = "Global\\DsHidMiniWriteEvent";
    internal const int BufferSize = 4096; // 4KB, keep in sync with driver
    internal const int CommandRegionSizeSize = 1024;
    internal const int HidRegionSizeSize = 1024;
    private const string MutexName = "Global\\DsHidMiniMutex";
    private readonly MemoryMappedViewAccessor _cmdAccessor;

    private readonly Mutex _commandMutex;
    private readonly MemoryMappedViewAccessor _hidAccessor;

    private readonly MemoryMappedFile _mappedFile;
    private readonly EventWaitHandle _readEvent;
    private readonly EventWaitHandle _writeEvent;

    /// <summary>
    ///     Creates a new <see cref="DsHidMiniInterop" /> instance by connecting to the driver IPC mechanism.
    /// </summary>
    /// <exception cref="DsHidMiniInteropExclusiveAccessException"></exception>
    /// <exception cref="DsHidMiniInteropUnavailableException"></exception>
    public DsHidMiniInterop()
    {
        try
        {
            _commandMutex = Mutex.OpenExisting(MutexName);

            //
            // Mutex already claimed by someone else, can't continue
            // 
            if (!_commandMutex.WaitOne(0))
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

        try
        {
            _mappedFile = MemoryMappedFile.OpenExisting(FileMapName);
            _cmdAccessor = _mappedFile.CreateViewAccessor(0, CommandRegionSizeSize);
            _hidAccessor = _mappedFile.CreateViewAccessor(CommandRegionSizeSize, HidRegionSizeSize);
        }
        catch (FileNotFoundException)
        {
            throw new DsHidMiniInteropUnavailableException();
        }
    }

    /// <summary>
    ///     Gets whether driver IPC is available.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
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

    /// <inheritdoc />
    public void Dispose()
    {
        _cmdAccessor.Dispose();
        _hidAccessor.Dispose();
        _mappedFile.Dispose();

        _readEvent.Dispose();
        _writeEvent.Dispose();

        _commandMutex.ReleaseMutex();
        _commandMutex.Dispose();
    }

    /// <summary>
    ///     Send a PING to the driver and awaits the reply.
    /// </summary>
    /// <exception cref="DsHidMiniInteropReplyTimeoutException"></exception>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException"></exception>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe void SendPing()
    {
        if (!_commandMutex.WaitOne(0))
        {
            throw new DsHidMiniInteropConcurrencyException();
        }

        try
        {
            byte* buffer = null;
            _cmdAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref buffer);

            DSHM_IPC_MSG_HEADER* message = (DSHM_IPC_MSG_HEADER*)buffer;

            message->Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE;
            message->Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DRIVER;
            message->Command.Driver = DSHM_IPC_MSG_CMD_DRIVER.DSHM_IPC_MSG_CMD_DRIVER_PING;
            message->TargetIndex = 0;
            message->Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_HEADER>();

            if (!SendAndWait())
            {
                throw new DsHidMiniInteropReplyTimeoutException();
            }

            //
            // Plausibility check
            // 
            if (message->Type == DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY
                && message->Target == DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT
                && message->Command.Driver == DSHM_IPC_MSG_CMD_DRIVER.DSHM_IPC_MSG_CMD_DRIVER_PING
                && message->TargetIndex == 0
                && message->Size == Marshal.SizeOf<DSHM_IPC_MSG_HEADER>())
            {
                return;
            }

            throw new DsHidMiniInteropUnexpectedReplyException(message);
        }
        finally
        {
            _cmdAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _commandMutex.ReleaseMutex();
        }
    }

    /// <summary>
    ///     Writes a new host address to the given device.
    /// </summary>
    /// <returns>A <see cref="SetHostResult" />.</returns>
    /// <remarks>This is synonymous with "pairing" to a new Bluetooth host.</remarks>
    /// <param name="deviceIndex">The one-based device index.</param>
    /// <param name="hostAddress">The new host address.</param>
    /// <exception cref="DsHidMiniInteropInvalidDeviceIndexException"></exception>
    /// <exception cref="DsHidMiniInteropConcurrencyException"></exception>
    /// <exception cref="DsHidMiniInteropReplyTimeoutException"></exception>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException"></exception>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe SetHostResult SetHostAddress(int deviceIndex, PhysicalAddress hostAddress)
    {
        ValidateDeviceIndex(deviceIndex);

        if (!_commandMutex.WaitOne(0))
        {
            throw new DsHidMiniInteropConcurrencyException();
        }

        try
        {
            byte* buffer = null;
            _cmdAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref buffer);

            DSHM_IPC_MSG_PAIR_TO_REQUEST* request = (DSHM_IPC_MSG_PAIR_TO_REQUEST*)buffer;

            request->Header.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE;
            request->Header.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DEVICE;
            request->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO;
            request->Header.TargetIndex = (uint)deviceIndex;
            request->Header.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_PAIR_TO_REQUEST>();

            fixed (byte* source = hostAddress.GetAddressBytes())
            {
                if (source is not null)
                {
                    Buffer.MemoryCopy(source, request->Address, 6, 6);
                }
                else
                {
                    // there might be previous values there we need to zero out
                    Unsafe.InitBlockUnaligned(request->Address, 0, 6);
                }
            }

            if (!SendAndWait())
            {
                throw new DsHidMiniInteropReplyTimeoutException();
            }

            DSHM_IPC_MSG_PAIR_TO_REPLY* reply = (DSHM_IPC_MSG_PAIR_TO_REPLY*)buffer;

            //
            // Plausibility check
            // 
            if (reply->Header.Type == DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY
                && reply->Header.Target == DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT
                && reply->Header.Command.Device == DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO
                && reply->Header.TargetIndex == deviceIndex
                && reply->Header.Size == Marshal.SizeOf<DSHM_IPC_MSG_PAIR_TO_REPLY>())
            {
                return new SetHostResult { WriteStatus = reply->WriteStatus, ReadStatus = reply->ReadStatus };
            }

            throw new DsHidMiniInteropUnexpectedReplyException(&reply->Header);
        }
        finally
        {
            _cmdAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _commandMutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="deviceIndex">The one-based device index.</param>
    /// <param name="playerIndex">The player index to set to. Valid values include 1 to 7.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="DsHidMiniInteropConcurrencyException"></exception>
    /// <exception cref="DsHidMiniInteropReplyTimeoutException"></exception>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException"></exception>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe UInt32 SetPlayerIndex(int deviceIndex, byte playerIndex)
    {
        ValidateDeviceIndex(deviceIndex);

        if (playerIndex is < 1 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(playerIndex),
                "Player index must be between (including) 1 and 7.");
        }

        if (!_commandMutex.WaitOne(0))
        {
            throw new DsHidMiniInteropConcurrencyException();
        }

        try
        {
            byte* buffer = null;
            _cmdAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref buffer);

            DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST* request = (DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST*)buffer;

            request->Header.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE;
            request->Header.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DEVICE;
            request->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX;
            request->Header.TargetIndex = (uint)deviceIndex;
            request->Header.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST>();

            request->PlayerIndex = playerIndex;

            if (!SendAndWait())
            {
                throw new DsHidMiniInteropReplyTimeoutException();
            }

            DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY* reply = (DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY*)buffer;

            //
            // Plausibility check
            // 
            if (reply->Header.Type == DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY
                && reply->Header.Target == DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT
                && reply->Header.Command.Device == DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX
                && reply->Header.TargetIndex == deviceIndex
                && reply->Header.Size == Marshal.SizeOf<DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY>())
            {
                return reply->NtStatus;
            }

            throw new DsHidMiniInteropUnexpectedReplyException(&reply->Header);
        }
        finally
        {
            _cmdAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _commandMutex.ReleaseMutex();
        }
    }

    /// <summary>
    ///     Ensures the target device index is in a valid range.
    /// </summary>
    private static void ValidateDeviceIndex(int deviceIndex)
    {
        if (deviceIndex is <= 0 or > byte.MaxValue)
        {
            throw new DsHidMiniInteropInvalidDeviceIndexException(deviceIndex);
        }
    }

    /// <summary>
    ///     Signal the driver that we are done modifying the shared region and are now awaiting an update from the driver.
    /// </summary>
    /// <param name="timeoutMs">Timeout to wait for a reply. Defaults to 500ms.</param>
    /// <returns>TRUE if we got a reply in time, FALSE otherwise.</returns>
    private bool SendAndWait(int timeoutMs = 500)
    {
        return SendAndWait(TimeSpan.FromMilliseconds(timeoutMs));
    }

    /// <summary>
    ///     Signal the driver that we are done modifying the shared region and are now awaiting an update from the driver.
    /// </summary>
    /// <param name="timeout">Timeout to wait for a reply.</param>
    /// <returns>TRUE if we got a reply in time, FALSE otherwise.</returns>
    private bool SendAndWait(TimeSpan timeout)
    {
        SignalWriteFinished();
        return _writeEvent.WaitOne(timeout);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private void SignalReadFinished()
    {
        _writeEvent.Set();
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private void SignalWriteFinished()
    {
        _readEvent.Set();
    }
}