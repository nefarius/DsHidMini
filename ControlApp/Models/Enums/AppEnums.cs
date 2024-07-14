namespace Nefarius.DsHidMini.ControlApp.Models.Enums;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum HidModeShort : byte
{
    Unknown = 0x00,
    SDF = 0x01,
    GPJ = 0x02,
    SXS = 0x03,
    DS4W = 0x04,
    XInput = 0x05
}