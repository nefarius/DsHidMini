namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Provider identity for the drivers this diagnostic listens to. Names and event symbols
///     mirror the ETW instrumentation manifests verbatim so classification stays anchored to the
///     driver source of truth instead of localized text.
/// </summary>
public static class KnownDiagnosticProviders
{
    public static readonly Guid BthPS3 = Guid.Parse("{37dcd579-e844-4c80-9c8b-a10850b6fac6}");
    public static readonly Guid BthPS3Psm = Guid.Parse("{586aa8b1-53a6-404f-9b3e-14483e514a2c}");
    public static readonly Guid DsHidMini = Guid.Parse("{ba65b162-c05c-4bd7-b75d-db8d030daf7f}");
}

/// <summary>
///     Event symbols from <c>BthPS3.man</c>. See <c>BthPS3/BthPS3.man</c> in the BthPS3 repository.
/// </summary>
public static class BthPS3Events
{
    public const string HciVersionTooLow = "HciVersionTooLow";
    public const string RemoteConnectReceived = "RemoteConnectReceived";
    public const string RemoteDeviceName = "RemoteDeviceName";
    public const string RemoteDeviceIdentified = "RemoteDeviceIdentified";
    public const string RemoteDeviceNotIdentified = "RemoteDeviceNotIdentified";
    public const string ChildDeviceCreationSuccessful = "ChildDeviceCreationSuccessful";
    public const string ChildDeviceCreationFailed = "ChildDeviceCreationFailed";
    public const string L2CAPRemoteConnectFailed = "L2CAPRemoteConnectFailed";
    public const string HidControlChannelConnected = "HidControlChannelConnected";
    public const string HidInterruptChannelConnected = "HidInterruptChannelConnected";
    public const string RemoteDeviceOnline = "RemoteDeviceOnline";
    public const string FailedWithNTStatus = "FailedWithNTStatus";
    public const string RemoteDisconnectCompleted = "RemoteDisconnectCompleted";
}

/// <summary>
///     Event symbols from <c>BthPS3PSM.man</c>. See <c>BthPS3PSM/BthPS3PSM.man</c> in the BthPS3 repository.
/// </summary>
public static class BthPS3PsmEvents
{
    public const string FailedWithNTStatus = "FailedWithNTStatus";
    public const string FailedToFindBulkInPipe = "FailedToFindBulkInPipe";
    public const string UnsupportedTransportType = "UnsupportedTransportType";
    public const string TransportTypeDetected = "TransportTypeDetected";
    public const string PsmPatchActivity = "PsmPatchActivity";
}

/// <summary>
///     Event symbols from <c>driver/DsHidMini.man</c>.
/// </summary>
public static class DsHidMiniEvents
{
    public const string BluetoothInputStreamStarted = "BluetoothInputStreamStarted";
    public const string FailedWithHResult = "FailedWithHResult";
    public const string FailedWithNTStatus = "FailedWithNTStatus";
    public const string FailedWithWin32Error = "FailedWithWin32Error";
    public const string JSONParseError = "JSONParseError";
    public const string PairingNoRadioFound = "PairingNoRadioFound";
    public const string WirelessDisconnectRequested = "WirelessDisconnectRequested";
    public const string WirelessDisconnectSignaled = "WirelessDisconnectSignaled";
    public const string WirelessDisconnectEventNotFound = "WirelessDisconnectEventNotFound";
    public const string WirelessDisconnectIoctlCompleted = "WirelessDisconnectIoctlCompleted";
    public const string YieldingToWiredInstance = "YieldingToWiredInstance";

    /// <summary>
    ///     Known DsHidMini failure or disconnect events. These must not count as
    ///     legacy handoff activity when <see cref="BluetoothInputStreamStarted" /> is absent.
    ///     Unknown event names are treated as activity.
    /// </summary>
    public static bool IsFailureOrDisconnect(string eventName) => eventName switch
    {
        FailedWithHResult or
        FailedWithNTStatus or
        FailedWithWin32Error or
        JSONParseError or
        PairingNoRadioFound or
        WirelessDisconnectRequested or
        WirelessDisconnectSignaled or
        WirelessDisconnectEventNotFound or
        WirelessDisconnectIoctlCompleted or
        YieldingToWiredInstance => true,
        _ => false
    };
}
