#pragma data_seg("SHARED")
extern unsigned int numInstances;
#pragma data_seg()
#pragma comment(linker, "/section:SHARED,RWS")

//
// Compile with or without additional features
// 

/* Enable Force Feedback features */
#define DSHM_FEATURE_FFB

#include <windows.h>
#include <wdf.h>
#include <initguid.h>
#include <usb.h>
#include <wdfusb.h>

#include <DmfModules.Library.h>
#include "DsHid.h"
#ifdef DSHM_FEATURE_FFB
#include "PID/PIDTypes.h"
#endif
#include "DsCommon.h"
#include "device.h"

#include "dshmguid.h"
#include "DsInternal.h"
#include "DsHidMiniDrv.h"
#include "Power.h"
#include "DsUsb.h"
#include "Ds3.h"
#include "DsBth.h"
#include "Config.h"

#include "Trace.h"


EXTERN_C_START

//
// WDFDRIVER Events
//

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD dshidminiEvtDeviceAdd;
EVT_WDF_OBJECT_CONTEXT_CLEANUP dshidminiEvtDriverContextCleanup;

EXTERN_C_END
