#pragma data_seg("SHARED")
extern unsigned int numInstances;
#pragma data_seg()
#pragma comment(linker, "/section:SHARED,RWS")

//
// Compile with or without additional features
// 

/* Enable Force Feedback features */
#define DSHM_FEATURE_FFB

#include <Windows.h>
#include <devpkey.h>
#include <wdf.h>
#include <initguid.h>
#include <usb.h>
#include <wdfusb.h>
#if DBG
#include <DbgHelp.h>
#endif

#include "JSON/cJSON.h"

#include <DmfModules.Library.h>
#include <DsHidMini/Ds3Types.h>
#include <DsHidMini/ScpTypes.h>
#include "DsCommon.h"
#include "DsHid.h"
#ifdef DSHM_FEATURE_FFB
#include "PID/PIDTypes.h"
#endif
#include "Configuration.h"
#include "Device.h"

#include "dshmguid.h"
#include "DsInternal.h"
#include "DsHidMiniDrv.h"
#include "Power.h"
#include "DsUsb.h"
#include "Ds3.h"
#include "DsBth.h"

#include "Trace.h"


EXTERN_C_START

//
// WDFDRIVER Events
//

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD dshidminiEvtDeviceAdd;
EVT_WDF_OBJECT_CONTEXT_CLEANUP dshidminiEvtDriverContextCleanup;

#if DBG
typedef BOOL(WINAPI *MINIDUMPWRITEDUMP)(
    HANDLE hProcess,
    DWORD dwPid,
    HANDLE hFile,
    MINIDUMP_TYPE DumpType,
    CONST PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
    CONST PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam,
    CONST PMINIDUMP_CALLBACK_INFORMATION CallbackParam
    );

LONG WINAPI DSHM_InternalExceptionHandler(struct _EXCEPTION_POINTERS* apExceptionInfo);
#endif

EXTERN_C_END
