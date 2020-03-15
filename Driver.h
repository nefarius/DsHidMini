/*++

Module Name:

    driver.h

Abstract:

    This file contains the driver definitions.

Environment:

    User-mode Driver Framework 2

--*/

#include <windows.h>
#include <wdf.h>
#include <initguid.h>
#include <usb.h>
#include <wdfusb.h>

#include <DmfModules.Library.h>
#include "DsHid.h"
#include "device.h"
#include "trace.h"
#include "DsHidMiniDrv.h"
#include "Power.h"
#include "DsUsb.h"
#include "Ds3.h"


EXTERN_C_START

//
// WDFDRIVER Events
//

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD dshidminiEvtDeviceAdd;
EVT_WDF_OBJECT_CONTEXT_CLEANUP dshidminiEvtDriverContextCleanup;

EXTERN_C_END
