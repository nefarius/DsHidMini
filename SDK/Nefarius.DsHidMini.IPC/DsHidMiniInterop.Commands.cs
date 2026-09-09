using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Nefarius.DsHidMini.IPC.Exceptions;
using Nefarius.DsHidMini.IPC.Models;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.IPC;

public partial class DsHidMiniInterop
{
    /// <summary>
    ///     Attempts to read the <see cref="DS3_RAW_INPUT_REPORT" /> from a given device instance.
    /// </summary>
    /// <remarks>
    ///     If <paramref name="timeout" /> is null, this method returns the last known input report copy immediately. If
    ///     you use this call in a busy loop, you should set a timeout so this call becomes event-based, meaning the call will
    ///     only return when the driver signaled that new data is available, otherwise you will just burn through CPU for no
    ///     good reason. A new input report is typically available each average 5 milliseconds, depending on the connection
    ///     (wired or wireless) so a timeout of 20 milliseconds should be a good recommendation.
    ///     When <paramref name="timeout" /> is set, the implementation waits on the driver's per-slot named manual-reset event
    ///     (same DACL as other IPC objects); it does not require administrator elevation. Multiple clients can wait on the
    ///     same slot without splitting wakeups.
    /// </remarks>
    /// <param name="deviceIndex">The one-based device index.</param>
    /// <param name="report">The <see cref="DS3_RAW_INPUT_REPORT" /> to populate.</param>
    /// <param name="timeout">Optional timeout to wait for a report update to arrive. Default invocation returns immediately.</param>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException">The driver returned unexpected or malformed data.</exception>
    /// <exception cref="DsHidMiniInteropUnavailableException">
    ///     No driver instance is available. Make sure that at least one
    ///     device is connected and that the driver is installed and working properly. Call <see cref="IsAvailable" /> prior to
    ///     avoid this exception.
    /// </exception>
    /// <returns>
    ///     TRUE if <paramref name="report" /> got filled in or FALSE if the given <paramref name="deviceIndex" /> is not
    ///     occupied, if <paramref name="timeout" /> is used and the named wait event for that slot does not exist (no device
    ///     in that slot), or if <paramref name="timeout" /> expires before a new report generation arrives.
    /// </returns>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe bool GetRawInputReport(int deviceIndex, ref DS3_RAW_INPUT_REPORT report, TimeSpan? timeout = null)
    {
        if (_hidView is null)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        ValidateDeviceIndex(deviceIndex);

        nuint byteOffset = (nuint)((deviceIndex - 1) * Marshal.SizeOf<IPC_HID_INPUT_REPORT_MESSAGE>());
        void* pMessage = (byte*)_hidView.Value + byteOffset;
        ref IPC_HID_INPUT_REPORT_MESSAGE message = ref Unsafe.AsRef<IPC_HID_INPUT_REPORT_MESSAGE>(pMessage);

        if (timeout.HasValue)
        {
            EventWaitHandle waitEvent;
            try
            {
                waitEvent = GetOrOpenHidReportWaitEvent(deviceIndex);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }

            Stopwatch waitClock = Stopwatch.StartNew();
            TimeSpan waitBudget = timeout.Value < TimeSpan.Zero ? TimeSpan.Zero : timeout.Value;
            while (true)
            {
                int sequence = Volatile.Read(ref message.SequenceNumber);
                if ((sequence & 1) == 0
                    && sequence != 0
                    && (!_lastSeenSequences.TryGetValue(deviceIndex, out int lastSeen) || sequence != lastSeen))
                {
                    return TryCopyRawInputReport(deviceIndex, ref message, waitBudget - waitClock.Elapsed, out report);
                }

                if (message.SlotIndex == 0)
                {
                    return false;
                }

                TimeSpan waitRemaining = waitBudget - waitClock.Elapsed;
                if (waitRemaining <= TimeSpan.Zero)
                {
                    return false;
                }

                if ((sequence & 1) == 0
                    && _lastSeenSequences.TryGetValue(deviceIndex, out lastSeen)
                    && sequence == lastSeen)
                {
                    // Already consumed this generation; the manual-reset event stays
                    // signaled until the driver ResetEvent()s at the next write.
                    Thread.Sleep((int)Math.Min(waitRemaining.TotalMilliseconds, 1));
                }
                else
                {
                    waitEvent.WaitOne(waitRemaining);
                }
            }
        }

        return TryCopyRawInputReport(deviceIndex, ref message, timeout: null, out report);
    }

    /// <summary>
    ///     Copies a stable HID slot snapshot using the driver seqlock.
    /// </summary>
    private bool TryCopyRawInputReport(
        int deviceIndex,
        ref IPC_HID_INPUT_REPORT_MESSAGE message,
        TimeSpan? timeout,
        out DS3_RAW_INPUT_REPORT report
    )
    {
        Stopwatch? copyClock = timeout.HasValue ? Stopwatch.StartNew() : null;
        TimeSpan copyBudget = timeout.GetValueOrDefault();
        // A zero remaining budget still allows one seqlock attempt for a generation
        // we already observed as stable before calling in.
        bool allowOneAttempt = timeout.HasValue && copyBudget <= TimeSpan.Zero;

        while (true)
        {
            if (copyClock is not null && !allowOneAttempt && copyClock.Elapsed >= copyBudget)
            {
                report = default;
                return false;
            }

            allowOneAttempt = false;

            int first = Volatile.Read(ref message.SequenceNumber);
            if ((first & 1) != 0)
            {
                Thread.Yield();
                continue;
            }

            //
            // Device is/got disconnected
            // 
            if (message.SlotIndex == 0)
            {
                report = default;
                return false;
            }

            //
            // Index mismatch is not supposed to happen
            // 
            if (message.SlotIndex != deviceIndex)
            {
                throw new DsHidMiniInteropUnexpectedReplyException();
            }

            DS3_RAW_INPUT_REPORT copy = message.InputReport;
            int second = Volatile.Read(ref message.SequenceNumber);
            if (first != second)
            {
                continue;
            }

            report = copy;
            _lastSeenSequences[deviceIndex] = first;
            return true;
        }
    }

    /// <summary>
    ///     Send a PING to the driver and awaits the reply.
    /// </summary>
    /// <exception cref="DsHidMiniInteropUnavailableException">
    ///     Driver IPC unavailable, make sure that at least one compatible
    ///     controller is connected and operational.
    /// </exception>
    /// <exception cref="DsHidMiniInteropReplyTimeoutException">The driver didn't respond within an expected period.</exception>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException">The driver returned unexpected or malformed data.</exception>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe void SendPing()
    {
        if (_commandMutex is null || _cmdView is null)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        AcquireCommandLock();

        try
        {
            ref DSHM_IPC_MSG_HEADER message = ref Unsafe.AsRef<DSHM_IPC_MSG_HEADER>(_cmdView);

            message.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE;
            message.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DRIVER;
            message.Command.Driver = DSHM_IPC_MSG_CMD_DRIVER.DSHM_IPC_MSG_CMD_DRIVER_PING;
            message.TargetIndex = 0;
            message.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_HEADER>();

            if (!SendAndWait())
            {
                throw new DsHidMiniInteropReplyTimeoutException();
            }

            ref DSHM_IPC_MSG_HEADER reply = ref Unsafe.AsRef<DSHM_IPC_MSG_HEADER>(_cmdView);

            //
            // Plausibility check
            // 
            if (reply is
                {
                    Type: DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY,
                    Target: DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT,
                    Command.Driver: DSHM_IPC_MSG_CMD_DRIVER.DSHM_IPC_MSG_CMD_DRIVER_PING, TargetIndex: 0
                }
                && reply.Size == Marshal.SizeOf<DSHM_IPC_MSG_HEADER>())
            {
                return;
            }

            throw new DsHidMiniInteropUnexpectedReplyException(ref reply);
        }
        finally
        {
            _commandMutex.ReleaseMutex();
        }
    }

    /// <summary>
    ///     Writes a new host address to the given device.
    /// </summary>
    /// <exception cref="DsHidMiniInteropUnavailableException">
    ///     Driver IPC unavailable, make sure that at least one compatible
    ///     controller is connected and operational.
    /// </exception>
    /// <returns>A <see cref="SetHostResult" /> containing success (or error) details.</returns>
    /// <remarks>This is synonymous with "pairing" to a new Bluetooth host.</remarks>
    /// <param name="deviceIndex">The one-based device index.</param>
    /// <param name="hostAddress">The new host address.</param>
    /// <exception cref="DsHidMiniInteropInvalidDeviceIndexException">
    ///     The <paramref name="deviceIndex" /> was outside a valid
    ///     range.
    /// </exception>
    /// <exception cref="DsHidMiniInteropConcurrencyException">A different thread is currently performing a data exchange.</exception>
    /// <exception cref="DsHidMiniInteropReplyTimeoutException">The driver didn't respond within an expected period.</exception>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException">The driver returned unexpected or malformed data.</exception>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe SetHostResult SetHostAddress(int deviceIndex, PhysicalAddress hostAddress)
    {
        if (_commandMutex is null || _cmdView is null)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        ValidateDeviceIndex(deviceIndex);

        AcquireCommandLock();

        try
        {
            ref DSHM_IPC_MSG_PAIR_TO_REQUEST request = ref Unsafe.AsRef<DSHM_IPC_MSG_PAIR_TO_REQUEST>(_cmdView);

            request.Header.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE;
            request.Header.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DEVICE;
            request.Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO;
            request.Header.TargetIndex = (uint)deviceIndex;
            request.Header.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_PAIR_TO_REQUEST>();

            fixed (byte* source = hostAddress.GetAddressBytes())
            fixed (byte* address = request.Address)
            {
                if (source is not null)
                {
                    Buffer.MemoryCopy(source, address, 6, 6);
                }
                else
                {
                    // there might be previous values there we need to zero out
                    Unsafe.InitBlockUnaligned(address, 0, 6);
                }
            }

            if (!SendAndWait())
            {
                throw new DsHidMiniInteropReplyTimeoutException();
            }

            ref DSHM_IPC_MSG_PAIR_TO_REPLY reply = ref Unsafe.AsRef<DSHM_IPC_MSG_PAIR_TO_REPLY>(_cmdView);

            //
            // Plausibility check
            // 
            if (reply.Header.Type == DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY
                && reply.Header is
                {
                    Target: DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT,
                    Command.Device: DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO
                }
                && reply.Header.TargetIndex == deviceIndex
                && reply.Header.Size == Marshal.SizeOf<DSHM_IPC_MSG_PAIR_TO_REPLY>())
            {
                return new SetHostResult { WriteStatus = reply.WriteStatus, ReadStatus = reply.ReadStatus };
            }

            throw new DsHidMiniInteropUnexpectedReplyException(ref reply.Header);
        }
        finally
        {
            _commandMutex.ReleaseMutex();
        }
    }

    /// <summary>
    ///     Overwrites the player slot indicator (player LEDs) of the given device.
    ///     The change is volatile: Automatic LED authority is handed to the application
    ///     for the rest of the session so driver battery refreshes do not overwrite it.
    ///     Returns <c>STATUS_ACCESS_DENIED</c> when LED authority is configured as Driver.
    /// </summary>
    /// <param name="deviceIndex">The one-based device index.</param>
    /// <param name="playerIndex">The player index to set to. Valid values include 1 to 7.</param>
    /// <exception cref="DsHidMiniInteropUnavailableException">
    ///     Driver IPC unavailable, make sure that at least one compatible
    ///     controller is connected and operational.
    /// </exception>
    /// <returns></returns>
    /// <exception cref="DsHidMiniInteropInvalidDeviceIndexException">
    ///     The <paramref name="deviceIndex" /> was outside the valid range 1..255.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     The <paramref name="playerIndex" /> was outside the valid range 1..7.
    /// </exception>
    /// <exception cref="DsHidMiniInteropConcurrencyException">A different thread is currently performing a data exchange.</exception>
    /// <exception cref="DsHidMiniInteropReplyTimeoutException">The driver didn't respond within an expected period.</exception>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException">The driver returned unexpected or malformed data.</exception>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe UInt32 SetPlayerIndex(int deviceIndex, byte playerIndex)
    {
        if (_commandMutex is null || _cmdView is null)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        ValidateDeviceIndex(deviceIndex);

        if (playerIndex is < 1 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(playerIndex),
                "Player index must be between (including) 1 and 7.");
        }

        AcquireCommandLock();

        try
        {
            ref DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST request =
                ref Unsafe.AsRef<DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST>(_cmdView);

            request.Header.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE;
            request.Header.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DEVICE;
            request.Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX;
            request.Header.TargetIndex = (uint)deviceIndex;
            request.Header.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST>();

            request.PlayerIndex = playerIndex;

            if (!SendAndWait())
            {
                throw new DsHidMiniInteropReplyTimeoutException();
            }

            ref DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY reply =
                ref Unsafe.AsRef<DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY>(_cmdView);

            //
            // Plausibility check
            // 
            if (reply.Header is
                {
                    Type: DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY,
                    Target: DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT,
                    Command.Device: DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX
                }
                && reply.Header.TargetIndex == deviceIndex
                && reply.Header.Size == Marshal.SizeOf<DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY>())
            {
                return reply.NtStatus;
            }

            throw new DsHidMiniInteropUnexpectedReplyException(ref reply.Header);
        }
        finally
        {
            _commandMutex.ReleaseMutex();
        }
    }

    /// <summary>
    ///     Sends the PlayStation 3 USB power-off sequence to a wired device: a 48-byte zero
    ///     output report, then Feature 0xF4 disable. The controller stays enumerated.
    /// </summary>
    /// <param name="deviceIndex">The one-based device index.</param>
    /// <exception cref="DsHidMiniInteropUnavailableException">
    ///     Driver IPC unavailable, make sure that at least one compatible
    ///     controller is connected and operational.
    /// </exception>
    /// <exception cref="DsHidMiniInteropInvalidDeviceIndexException">
    ///     The <paramref name="deviceIndex" /> was outside the valid range 1..255.
    /// </exception>
    /// <exception cref="DsHidMiniInteropConcurrencyException">A different thread is currently performing a data exchange.</exception>
    /// <exception cref="DsHidMiniInteropReplyTimeoutException">The driver didn't respond within an expected period.</exception>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException">The driver returned unexpected or malformed data.</exception>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe PowerOffUsbResult PowerOffUsbDevice(int deviceIndex)
    {
        if (_commandMutex is null || _cmdView is null)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        ValidateDeviceIndex(deviceIndex);

        AcquireCommandLock();

        try
        {
            ref DSHM_IPC_MSG_USB_POWER_OFF_REQUEST request =
                ref Unsafe.AsRef<DSHM_IPC_MSG_USB_POWER_OFF_REQUEST>(_cmdView);

            request.Header.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE;
            request.Header.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DEVICE;
            request.Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_USB_POWER_OFF;
            request.Header.TargetIndex = (uint)deviceIndex;
            request.Header.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_USB_POWER_OFF_REQUEST>();

            if (!SendAndWait())
            {
                throw new DsHidMiniInteropReplyTimeoutException();
            }

            ref DSHM_IPC_MSG_USB_POWER_OFF_REPLY reply =
                ref Unsafe.AsRef<DSHM_IPC_MSG_USB_POWER_OFF_REPLY>(_cmdView);

            if (reply.Header is
                {
                    Type: DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY,
                    Target: DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT,
                    Command.Device: DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_USB_POWER_OFF
                }
                && reply.Header.TargetIndex == deviceIndex
                && reply.Header.Size == Marshal.SizeOf<DSHM_IPC_MSG_USB_POWER_OFF_REPLY>())
            {
                return new PowerOffUsbResult
                {
                    IndicatorsOffStatus = reply.IndicatorsOffStatus,
                    ShutdownStatus = reply.ShutdownStatus
                };
            }

            throw new DsHidMiniInteropUnexpectedReplyException(ref reply.Header);
        }
        finally
        {
            _commandMutex.ReleaseMutex();
        }
    }

    /// <summary>
    ///     Sets rumble motor strengths on a device. Values are processed through the driver's
    ///     existing rescale, alternative-mode, keep-alive, and Navigation-controller rules.
    ///     The change is volatile and is not written to the JSON configuration.
    /// </summary>
    /// <param name="deviceIndex">The one-based device index.</param>
    /// <param name="largeMotor">Heavy / left motor strength (0-255).</param>
    /// <param name="smallMotor">Light / right motor strength (0-255).</param>
    /// <returns>The NTSTATUS returned by the driver.</returns>
    /// <exception cref="DsHidMiniInteropUnavailableException">
    ///     Driver IPC unavailable, make sure that at least one compatible
    ///     controller is connected and operational.
    /// </exception>
    /// <exception cref="DsHidMiniInteropInvalidDeviceIndexException">
    ///     The <paramref name="deviceIndex" /> was outside the valid range 1..255.
    /// </exception>
    /// <exception cref="DsHidMiniInteropConcurrencyException">A different thread is currently performing a data exchange.</exception>
    /// <exception cref="DsHidMiniInteropReplyTimeoutException">The driver didn't respond within an expected period.</exception>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException">The driver returned unexpected or malformed data.</exception>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe UInt32 SetRumble(int deviceIndex, byte largeMotor, byte smallMotor)
    {
        if (_commandMutex is null || _cmdView is null)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        ValidateDeviceIndex(deviceIndex);

        AcquireCommandLock();

        try
        {
            ref DSHM_IPC_MSG_SET_RUMBLE_REQUEST request =
                ref Unsafe.AsRef<DSHM_IPC_MSG_SET_RUMBLE_REQUEST>(_cmdView);

            request.Header.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE;
            request.Header.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DEVICE;
            request.Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_RUMBLE;
            request.Header.TargetIndex = (uint)deviceIndex;
            request.Header.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_SET_RUMBLE_REQUEST>();

            request.LargeMotor = largeMotor;
            request.SmallMotor = smallMotor;

            if (!SendAndWait())
            {
                throw new DsHidMiniInteropReplyTimeoutException();
            }

            ref DSHM_IPC_MSG_SET_RUMBLE_REPLY reply =
                ref Unsafe.AsRef<DSHM_IPC_MSG_SET_RUMBLE_REPLY>(_cmdView);

            if (reply.Header is
                {
                    Type: DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY,
                    Target: DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT,
                    Command.Device: DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_RUMBLE
                }
                && reply.Header.TargetIndex == deviceIndex
                && reply.Header.Size == Marshal.SizeOf<DSHM_IPC_MSG_SET_RUMBLE_REPLY>())
            {
                return reply.NtStatus;
            }

            throw new DsHidMiniInteropUnexpectedReplyException(ref reply.Header);
        }
        finally
        {
            _commandMutex.ReleaseMutex();
        }
    }

    /// <summary>
    ///     Enables or disables alternative rumble mode for the current session only.
    ///     The persisted JSON configuration is not updated; a driver config reload
    ///     restores the file value.
    /// </summary>
    /// <param name="deviceIndex">The one-based device index.</param>
    /// <param name="enabled"><see langword="true" /> to enable alternative rumble mode.</param>
    /// <returns>The NTSTATUS returned by the driver.</returns>
    /// <exception cref="DsHidMiniInteropUnavailableException">
    ///     Driver IPC unavailable, make sure that at least one compatible
    ///     controller is connected and operational.
    /// </exception>
    /// <exception cref="DsHidMiniInteropInvalidDeviceIndexException">
    ///     The <paramref name="deviceIndex" /> was outside the valid range 1..255.
    /// </exception>
    /// <exception cref="DsHidMiniInteropConcurrencyException">A different thread is currently performing a data exchange.</exception>
    /// <exception cref="DsHidMiniInteropReplyTimeoutException">The driver didn't respond within an expected period.</exception>
    /// <exception cref="DsHidMiniInteropUnexpectedReplyException">The driver returned unexpected or malformed data.</exception>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public unsafe UInt32 SetAlternateRumbleMode(int deviceIndex, bool enabled)
    {
        if (_commandMutex is null || _cmdView is null)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        ValidateDeviceIndex(deviceIndex);

        AcquireCommandLock();

        try
        {
            ref DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST request =
                ref Unsafe.AsRef<DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST>(_cmdView);

            request.Header.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE;
            request.Header.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DEVICE;
            request.Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_ALTERNATE_RUMBLE_MODE;
            request.Header.TargetIndex = (uint)deviceIndex;
            request.Header.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST>();

            request.IsEnabled = enabled ? (byte)1 : (byte)0;

            if (!SendAndWait())
            {
                throw new DsHidMiniInteropReplyTimeoutException();
            }

            ref DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY reply =
                ref Unsafe.AsRef<DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY>(_cmdView);

            if (reply.Header is
                {
                    Type: DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY,
                    Target: DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT,
                    Command.Device: DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_ALTERNATE_RUMBLE_MODE
                }
                && reply.Header.TargetIndex == deviceIndex
                && reply.Header.Size == Marshal.SizeOf<DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY>())
            {
                return reply.NtStatus;
            }

            throw new DsHidMiniInteropUnexpectedReplyException(ref reply.Header);
        }
        finally
        {
            _commandMutex.ReleaseMutex();
        }
    }
}
