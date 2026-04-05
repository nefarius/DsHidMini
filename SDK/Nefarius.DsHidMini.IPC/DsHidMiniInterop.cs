using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
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

    private readonly Dictionary<int, EventWaitHandle> _inputReportWaitEvents = new();

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

        int instanceIndex = 0;

        while (Devcon.FindByInterfaceGuid(DsHidMiniDriver.DeviceInterfaceGuid, out PnPDevice device, instanceIndex++))
        {
            _connectedDevices.Add(instanceIndex, device);
        }

        //
        // We need to let go of all shared resources or the driver won't be able to re-create the named objects
        // 
        if (_connectedDevices.Count == 0)
        {
            Dispose();
        }
    }

    private void DsHidMiniDeviceRemoved(DeviceEventArgs obj)
    {
        PnPDevice? device = PnPDevice.GetDeviceByInterfaceId(obj.SymLink, DeviceLocationFlags.Phantom);

        KeyValuePair<int, PnPDevice> item = _connectedDevices.Single(kvp => kvp.Value.Equals(device));

        // TODO: react to removal

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
        foreach (EventWaitHandle handle in _inputReportWaitEvents.Values)
        {
            handle.Dispose();
        }

        _inputReportWaitEvents.Clear();
    }

    /// <summary>
    ///     Opens the driver's named auto-reset event for the given one-based device slot (created with DACL allowing
    ///     authenticated users).
    /// </summary>
    private EventWaitHandle GetOrOpenHidReportWaitEvent(int deviceIndex)
    {
        if (_inputReportWaitEvents.TryGetValue(deviceIndex, out EventWaitHandle? existing))
        {
            return existing;
        }

        string name = $"{HidReportWaitEventNamePrefix}{deviceIndex}";
        EventWaitHandle opened = EventWaitHandle.OpenExisting(name);
        _inputReportWaitEvents[deviceIndex] = opened;

        return opened;
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