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

//
// Artificial limit to make memory management easier (should be enough ;)
// 
#define DSHM_MAX_DEVICES	UCHAR_MAX

#include "Configuration.h"
#include "IPC.h"
#include "Device.h"

#include <DsHidMini/dshmguid.h>
#include "DsInternal.h"
#include "DsHidMiniDrv.h"
#include "Power.h"
#include "DsUsb.h"
#include "Ds3.h"
#include "DsBth.h"
#include "HID.ReportHandlers.h"

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
		// Shared memory regions details
		// 
		struct
		{
			//
			// Low frequency, low lifetime command exchange region
			// 
			struct
			{
				//
				// Pointer to shared memory buffer
				// 
				PUCHAR Buffer;

				//
				// Total size of shared memory region
				// 
				size_t BufferSize;
			} Commands;

			//
			// High-frequency, high throughput read-only region
			struct
			{
				//
				// Pointer to shared memory buffer
				// 
				PUCHAR Buffer;

				//
				// Total size of shared memory region
				// 
				size_t BufferSize;
			} HID;
		} SharedRegions;

		//
		// Message dispatcher callback handlers
		// 
		struct
		{
			PFN_DSHM_IPC_DispatchDeviceMessage Callbacks[DSHM_MAX_DEVICES];

			PDEVICE_CONTEXT Contexts[DSHM_MAX_DEVICES];
		} DeviceDispatchers;
	} IPC;

	//
	// Index slots to associate connected devices in IPC
	// 
	LONG Slots[8]; // 256 usable bits

	//
	// Lock protecting access to Slots
	// 
	WDFWAITLOCK SlotsLock;
} DSHM_DRIVER_CONTEXT, * PDSHM_DRIVER_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DSHM_DRIVER_CONTEXT, DriverGetContext)

LONG
FORCEINLINE
SET_SLOT(
	PDSHM_DRIVER_CONTEXT Context,
	UINT32 SlotIndex
)
{
	const UINT32 bits = sizeof(Context->Slots);

	return InterlockedOr(&Context->Slots[SlotIndex / bits], 1 << (SlotIndex % bits));
}

BOOLEAN
FORCEINLINE
TEST_SLOT(
	PDSHM_DRIVER_CONTEXT Context,
	UINT32 SlotIndex
)
{
	const UINT32 bits = sizeof(Context->Slots);

	return (BOOLEAN)(Context->Slots[SlotIndex / bits] & (1 << (SlotIndex % bits)));
}

LONG
FORCEINLINE
CLEAR_SLOT(
	PDSHM_DRIVER_CONTEXT Context,
	UINT32 SlotIndex
)
{
	const UINT32 bits = sizeof(Context->Slots);

	return InterlockedAnd(&Context->Slots[SlotIndex / bits], ~(1 << (SlotIndex % bits)));
}

//
// WDFDRIVER Events
//

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD dshidminiEvtDeviceAdd;
EVT_WDF_OBJECT_CONTEXT_CLEANUP dshidminiEvtDriverContextCleanup;

VOID DumpAsHex(PCSTR Prefix, PVOID Buffer, ULONG BufferLength);

EXTERN_C_END
