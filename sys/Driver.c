/*++

Module Name:

    driver.c

Abstract:

    This file contains the driver entry points and callbacks.

Environment:

    User-mode Driver Framework 2

--*/

#include "Driver.h"
#include "Driver.tmh"

unsigned int numInstances = 0;


/*WPP_INIT_TRACING(); (This comment is necessary for WPP Scanner.)*/
#pragma code_seg("INIT")
DMF_DEFAULT_DRIVERENTRY(DriverEntry,
    dshidminiEvtDriverContextCleanup,
    dshidminiEvtDeviceAdd,
    L"DsHidMini")
#pragma code_seg()
