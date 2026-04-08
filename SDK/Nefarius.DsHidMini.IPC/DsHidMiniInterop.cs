using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.System.SystemInformation;

using Microsoft.Win32.SafeHandles;

using Nefarius.DsHidMini.IPC.Exceptions;
using Nefarius.DsHidMini.IPC.Models;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.IPC;

/// <summary>
///     Connects to the drivers shared memory region and offers utility methods for data exchange.
/// </summary>
public sealed partial class DsHidMiniInterop : IDisposable
{
    private const string FileMapName = "Global\\DsHidMiniSharedMemory";
    private const string ReadEventName = "Global\\DsHidMiniReadEvent";
    private const string WriteEventName = "Global\\DsHidMiniWriteEvent";
    private const string MutexName = "Global\\DsHidMiniCommandMutex";

    /// <summary>
    ///     Must match <c>DSHM_IPC_HID_REPORT_EVENT_PREFIX</c> in driver <c>IPC.h</c> (suffix is the one-based device index).
    /// </summary>
    private const string HidReportWaitEventNamePrefix = "Global\\DsHidMiniHidReportEvent";

    private readonly Dictionary<int, PnPDevice> _connectedDevices = new();
    private readonly DeviceNotificationListener _deviceListener = new();
    private MEMORY_MAPPED_VIEW_ADDRESS? _cmdView;

    private Mutex? _commandMutex;

    private SafeFileHandle? _fileMapping;
    private MEMORY_MAPPED_VIEW_ADDRESS? _hidView;

    private readonly ConcurrentDictionary<int, EventWaitHandle> _inputReportWaitEvents = new();

    private EventWaitHandle? _readEvent;
    private EventWaitHandle? _writeEvent;

    /// <summary>
    ///     Creates a new <see cref="DsHidMiniInterop" /> instance by connecting to the driver IPC mechanism.
    /// </summary>
    /// <exception cref="DsHidMiniInteropUnavailableException">
    ///     No driver instance is available. Make sure that at least one
    ///     device is connected and that the driver is installed and working properly. Call <see cref="IsAvailable" /> prior to
    ///     avoid this exception.
    /// </exception>
    public DsHidMiniInterop()
    {
        Reconnect();

        _deviceListener.RegisterDeviceArrived(DsHidMiniDeviceArrived, DsHidMiniDriver.DeviceInterfaceGuid);
        _deviceListener.RegisterDeviceRemoved(DsHidMiniDeviceRemoved, DsHidMiniDriver.DeviceInterfaceGuid);

        RefreshDevices();
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
                using MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(FileMapName);

                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_cmdView.HasValue)
        {
            PInvoke.UnmapViewOfFile(_cmdView.Value);
        }

        if (_hidView.HasValue)
        {
            PInvoke.UnmapViewOfFile(_hidView.Value);
        }

        _fileMapping?.Dispose();

        _readEvent?.Dispose();
        _writeEvent?.Dispose();
        DisposeInputReportWaitEvents();

        _commandMutex?.Dispose();
    }

    /// <summary>
    ///     Attempt re-initialization of IPC after all devices got disconnected.
    /// </summary>
    /// <exception cref="DsHidMiniInteropUnavailableException">
    ///     No driver instance is available. Make sure that at least one
    ///     device is connected and that the driver is installed and working properly. Call <see cref="IsAvailable" /> prior to
    ///     avoid this exception.
    /// </exception>
    public void Reconnect()
    {
        DisposeInputReportWaitEvents();

        PInvoke.GetSystemInfo(out SYSTEM_INFO systemInfo);

        try
        {
            _commandMutex = Mutex.OpenExisting(MutexName);
            _readEvent = EventWaitHandle.OpenExisting(ReadEventName);
            _writeEvent = EventWaitHandle.OpenExisting(WriteEventName);
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        try
        {
            _fileMapping = PInvoke.OpenFileMapping(
                (uint)(FILE_MAP.FILE_MAP_READ | FILE_MAP.FILE_MAP_WRITE),
                new BOOL(false),
                FileMapName
            );

            _cmdView = PInvoke.MapViewOfFile(
                _fileMapping,
                FILE_MAP.FILE_MAP_READ | FILE_MAP.FILE_MAP_WRITE,
                0,
                0,
                systemInfo.dwAllocationGranularity
            );

            if (_cmdView.Value == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to access command view");
            }

            uint pageSize = systemInfo.dwAllocationGranularity;
            long alignedOffset = systemInfo.dwAllocationGranularity / pageSize * pageSize;
            long offsetWithinPage = systemInfo.dwAllocationGranularity % pageSize;

            _hidView = PInvoke.MapViewOfFile(
                _fileMapping,
                FILE_MAP.FILE_MAP_READ,
                0,
                (uint)alignedOffset,
                (uint)(systemInfo.dwAllocationGranularity + offsetWithinPage)
            );

            if (_hidView.Value == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to access HID view");
            }
        }
        catch (FileNotFoundException)
        {
            throw new DsHidMiniInteropUnavailableException();
        }
    }

    private void RefreshDevices()
    {
        DisposeInputReportWaitEvents();

        _connectedDevices.Clear();

        var enumerated = new List<PnPDevice>();
        int instanceIndex = 0;
        while (Devcon.FindByInterfaceGuid(DsHidMiniDriver.DeviceInterfaceGuid, out PnPDevice device, instanceIndex++))
        {
            enumerated.Add(device);
        }

        var usedKeys = new HashSet<int>();
        foreach (PnPDevice device in enumerated)
        {
            int? slot = TryGetIpcSlotIndex(device);
            if (slot is int s && !usedKeys.Contains(s))
            {
                _connectedDevices[s] = device;
                usedKeys.Add(s);
            }
        }

        foreach (PnPDevice device in enumerated)
        {
            if (_connectedDevices.ContainsValue(device))
            {
                continue;
            }

            int fallback = 1;
            while (fallback <= byte.MaxValue && usedKeys.Contains(fallback))
            {
                fallback++;
            }

            if (fallback > byte.MaxValue)
            {
                break;
            }

            _connectedDevices[fallback] = device;
            usedKeys.Add(fallback);
        }

        //
        // We need to let go of all shared resources or the driver won't be able to re-create the named objects
        // 
        if (_connectedDevices.Count == 0)
        {
            Dispose();
        }
    }

    /// <summary>
    ///     Reads the driver's one-based IPC slot for a device when available (see <see cref="DsHidMiniDriver.IpcSlotIndexProperty" />).
    /// </summary>
    /// <returns>The slot index, or <see langword="null" /> if the property is missing (older driver) or invalid.</returns>
    private static int? TryGetIpcSlotIndex(PnPDevice device)
    {
        uint slot;
        try
        {
            slot = device.GetProperty<uint>(DsHidMiniDriver.IpcSlotIndexProperty);
        }
        catch (Exception)
        {
            // Older driver without the property, or property not yet available
            return null;
        }

        if (slot is < 1 or > byte.MaxValue)
        {
            return null;
        }

        return (int)slot;
    }

    private void DsHidMiniDeviceRemoved(DeviceEventArgs obj)
    {
        PnPDevice? device = PnPDevice.GetDeviceByInterfaceId(obj.SymLink, DeviceLocationFlags.Phantom);

        if (device is not null)
        {
            _ = _connectedDevices.Single(kvp => kvp.Value.Equals(device));
        }

        RefreshDevices();
    }

    private void DsHidMiniDeviceArrived(DeviceEventArgs obj)
    {
        if (_connectedDevices.Count == 0)
        {
            Reconnect();
        }

        RefreshDevices();
    }

    private void DisposeInputReportWaitEvents()
    {
        foreach (int key in _inputReportWaitEvents.Keys.ToArray())
        {
            if (_inputReportWaitEvents.TryRemove(key, out EventWaitHandle? handle))
            {
                handle.Dispose();
            }
        }
    }

    /// <summary>
    ///     Returns a cached handle to the driver's named auto-reset event for the given one-based device slot (created with
    ///     DACL allowing authenticated users).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="EventWaitHandle.OpenExisting(string)" /> is used to obtain the handle. It throws
    ///         <see cref="WaitHandleCannotBeOpenedException" /> when the named event does not exist (for example, the device
    ///         slot is empty). <see cref="GetRawInputReport" /> catches that exception and returns <see langword="false" />.
    ///     </para>
    /// </remarks>
    /// <exception cref="WaitHandleCannotBeOpenedException">
    ///     No kernel object exists for the per-slot name (prefix <see cref="HidReportWaitEventNamePrefix" /> plus
    ///     <paramref name="deviceIndex" />).
    /// </exception>
    private EventWaitHandle GetOrOpenHidReportWaitEvent(int deviceIndex)
    {
        if (_inputReportWaitEvents.TryGetValue(deviceIndex, out EventWaitHandle? existing))
        {
            return existing;
        }

        string name = $"{HidReportWaitEventNamePrefix}{deviceIndex}";
        EventWaitHandle opened = EventWaitHandle.OpenExisting(name);

        if (_inputReportWaitEvents.TryAdd(deviceIndex, opened))
        {
            return opened;
        }

        opened.Dispose();

        return _inputReportWaitEvents[deviceIndex];
    }

    private void AcquireCommandLock()
    {
        if (_commandMutex is null)
        {
            throw new DsHidMiniInteropUnavailableException();
        }

        try
        {
            if (!_commandMutex.WaitOne(0))
            {
                throw new DsHidMiniInteropConcurrencyException();
            }
        }
        catch (AbandonedMutexException) { }
    }

    /// <summary>
    ///     Ensures the target device index is in a valid range.
    /// </summary>
    /// <exception cref="DsHidMiniInteropInvalidDeviceIndexException">
    ///     The <paramref name="deviceIndex" /> was outside a valid
    ///     range.
    /// </exception>
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
    /// <param name="timeoutMs">Timeout to wait for a reply. Defaults to 500 ms.</param>
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
        return _writeEvent!.WaitOne(timeout);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private void SignalReadFinished()
    {
        // we are done reading from the region, driver can now write
        _writeEvent!.Set();
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private void SignalWriteFinished()
    {
        // we are done writing to the region, driver can now read
        _readEvent!.Set();
    }
}