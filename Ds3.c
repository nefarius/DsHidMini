#include "Driver.h"
#include "Ds3.tmh"


//
// Sends the "magic packet" to the DS3 so it starts its interrupt endpoint.
// 
NTSTATUS DsUsb_Ds3Init(PDEVICE_CONTEXT Context)
{
    // "Magic packet"
    UCHAR hidCommandEnable[DS3_HID_COMMAND_ENABLE_SIZE] =
    {
        0x42, 0x0C, 0x00, 0x00
    };

    return SendControlRequest(
        Context,
        BmRequestHostToDevice,
        BmRequestClass,
        SetReport,
        Ds3FeatureStartDevice,
        0,
        hidCommandEnable,
        DS3_HID_COMMAND_ENABLE_SIZE
    );
}
