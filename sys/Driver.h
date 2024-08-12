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
#include <DsHidMiniETW.h>

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

#include <DsHidMini/dshmguid.h>
#include "DsInternal.h"
#include "DsHidMiniDrv.h"
#include "Power.h"
#include "DsUsb.h"
#include "Ds3.h"
#include "DsBth.h"
#include "HID.ReportHandlers.h"
#include "IPC.h"

#include "Trace.h"


EXTERN_C_START

typedef struct _DSHM_DRIVER_CONTEXT
{
	//
	// IPC-specific fields
	// 
	struct
	{
		//
		// Handle of the memory-mapped file
		// 
		HANDLE MapFile;

		//
		// Mutex handle to sync a connected client application
		// 
		HANDLE ConnectMutex;

		//
		// Read ready event handle
		// 
		HANDLE ReadEvent;

		//
		// Write ready event handle
		// 
		HANDLE WriteEvent;

		//
		// Dispatch thread handle
		// 
		HANDLE DispatchThread;

		//
		// Thread termination event
		// 
		HANDLE DispatchThreadTermination;

		//
		// Pointer to shared memory buffer
		// 
		PUCHAR SharedMemory;

		//
		// Total size of shared memory region
		// 
		size_t SharedMemorySize;

	} IPC;
	
} DSHM_DRIVER_CONTEXT, *PDSHM_DRIVER_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DSHM_DRIVER_CONTEXT, DriverGetContext)

//
// WDFDRIVER Events
//

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD dshidminiEvtDeviceAdd;
EVT_WDF_OBJECT_CONTEXT_CLEANUP dshidminiEvtDriverContextCleanup;

VOID DumpAsHex(PCSTR Prefix, PVOID Buffer, ULONG BufferLength);

EXTERN_C_END
