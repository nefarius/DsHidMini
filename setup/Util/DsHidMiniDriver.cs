using System;

namespace Nefarius.DsHidMini.Setup.Util;

public static class DsHidMiniDriver
{
    /// <summary>
    ///     Interface GUID common to all devices the DsHidMini driver supports.
    /// </summary>
    public static Guid DeviceInterfaceGuid => Guid.Parse("{399ED672-E0BD-4FB3-AB0C-4955B56FB86A}");
}