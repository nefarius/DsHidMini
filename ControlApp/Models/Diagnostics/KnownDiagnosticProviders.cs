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
