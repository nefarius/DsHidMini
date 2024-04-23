#pragma once
#include <DmfModule.h>


EXTERN_C_START

#define DSHM_NAMED_EVENT_DISCONNECT		L"Global\\DsHidMiniDisconnectEvent%ls"

struct USB_DEVICE_CONTEXT
{
	//
	// Framework USB object
	// 
	WDFUSBDEVICE UsbDevice;

	//
	// Device descriptor
	// 
	USB_DEVICE_DESCRIPTOR UsbDeviceDescriptor;

	//
	// Product string
	// 
	WDFMEMORY ProductString;

	//
	// USB interface object
	// 
	WDFUSBINTERFACE UsbInterface;

	//
	// USB Interrupt (in) pipe handle
	// 
	WDFUSBPIPE InterruptInPipe;

	//
	// USB Interrupt (out) pipe handle
	// 
	WDFUSBPIPE InterruptOutPipe;
	
	//
	// Timestamp to calculate charging cycle state change
	// 
	LARGE_INTEGER ChargingCycleTimestamp;
};

struct BTH_DEVICE_CONTEXT
{
	struct
	{
		DMFMODULE OutputWriterModule;

		WDFIOTARGET OutputWriterIoTarget;

	} HidControl;
	
	struct
	{
		DMFMODULE InputStreamerModule;

		WDFIOTARGET InputStreamerIoTarget;
		
	} HidInterrupt;

	struct
	{
		//
		// Delayed Output Report Timer
		// 
		WDFTIMER HidOutputReport;
				
	} Timers;
	
	//
	// Timestamp to calculate quick disconnect combo detection
	// 
	LARGE_INTEGER QuickDisconnectTimestamp;

	//
	// Event to listen for to disconnect
	//
	HANDLE DisconnectEvent;

	//
	// Wait handle
	//
	HANDLE DisconnectWaitHandle;

	//
	// Timestamp to calculate idle disconnect
	// 
	LARGE_INTEGER IdleDisconnectTimestamp;
};

#ifdef DSHM_FEATURE_FFB
typedef struct _FFB_ATTRIBUTES
{
	UCHAR EffectBlockIndex;

	PID_EFFECT_TYPE EffectType;

	BOOLEAN IsReserved;

	BOOLEAN IsReported;

} FFB_ATTRIBUTES, *PFFB_ATTRIBUTES;
#endif

/**
 * Output report context.
 *
 * @author	Benjamin "Nefarius" Höglinger-Stelzer
 * @date	01.04.2021
 */
typedef struct _DS_OUTPUT_REPORT_CONTEXT
{
	//
	// Time of packet arrival
	// 
	LARGE_INTEGER ReceivedTimestamp;

	//
	// Actual size of buffer
	// 
	size_t BufferSize;

	//
	// The initiator of this report
	// 
	DS_OUTPUT_REPORT_SOURCE ReportSource;
	
} DS_OUTPUT_REPORT_CONTEXT, *PDS_OUTPUT_REPORT_CONTEXT;

/**
 * Cached output report values to help with rate-control.
 *
 * @author	Benjamin "Nefarius" Höglinger-Stelzer
 * @date	12.03.2021
 */
typedef struct _DS_OUTPUT_REPORT_CACHE
{
	//
	// Timestamp of last successful send
	// 
	LARGE_INTEGER LastSentTimestamp;

	//
	// Send delay timer
	// 
	WDFTIMER SendDelayTimer;

	//
	// Pending packet buffer to send
	// 
	PVOID PendingClientBuffer;

	//
	// Pending packet buffer context
	// 
	PDS_OUTPUT_REPORT_CONTEXT PendingClientBufferContext;

	//
	// Lock protecting cache field access
	// 
	WDFWAITLOCK Lock;

	//
	// TRUE if the timer is currently scheduled to get executed
	// 
	BOOLEAN IsScheduled;
	
	//
	// TODO: replace with WDFMEMORY object
	// 
	UCHAR LastReport[0x32]; // Introduce const
	
} DS_OUTPUT_REPORT_CACHE, *PDS_OUTPUT_REPORT_CACHE;

//
// Stores the constants used for rumble rescalling and if it is allowed
//
typedef struct _DS_RESCALE_STATE
{
    BOOLEAN IsAllowed;

    DOUBLE ConstA;

    DOUBLE ConstB;

} DS_RESCALE_STATE, * PDS_RESCALE_STATE;

typedef struct _DEVICE_CONTEXT
{
	//
	// VirtualHidMini DMF module
	// 
	DMFMODULE DsHidMiniModule;
	
	struct
	{
		//
		// Threaded buffer queue worker
		// 
		DMFMODULE Worker;

		//
		// Lock protecting output report buffer access
		// 
		WDFWAITLOCK Lock;

		//
		// Output report mode of operation
		// 
		DS_OUTPUT_REPORT_MODE Mode;

		//
		// Cached output report meta-data
		// 
		DS_OUTPUT_REPORT_CACHE Cache;
		
	} OutputReport;
	
	//
	// Type of connection (wired, wireless)
	// 
	DS_CONNECTION_TYPE ConnectionType;

	//
	// Remote BTH address
	// 
	BD_ADDR HostAddress;

	//
	// Local device BTH address
	// 
	BD_ADDR DeviceAddress;

	//
	// Local device BTH address as hex string
	// 
	CHAR DeviceAddressString[(sizeof(BD_ADDR) * 2) + 1];

	//
	// Current reported battery status
	// 
	DS_BATTERY_STATUS BatteryStatus;

	//
	// Time of last battery status update
	// 
	LARGE_INTEGER BatteryStatusTimestamp;

	union
	{
		//
		// USB-specific properties
		// 
		struct USB_DEVICE_CONTEXT Usb;

		//
		// Bluetooth-specific properties
		// 
		struct BTH_DEVICE_CONTEXT Bth;

	} Connection;
		
	//
	// Vendor ID as reported by hardware
	// 
	USHORT VendorId;

	//
	// Product ID as reported by hardware
	// 
	USHORT ProductId;

	//
	// Version number (rarely used)
	// 
	USHORT VersionNumber;

	//
	// Output report buffer
	// 
	WDFMEMORY OutputReportMemory;

	//
	// Registry-stored driver configuration
	// 
	DS_DRIVER_CONFIGURATION Configuration;

	//
	// Event to listen for to hot-reload properties
	//
	HANDLE ConfigurationDirectoryWatcherEvent;

	//
	// Wait handle for hot-reload
	//
	HANDLE ConfigurationDirectoryWatcherWaitHandle;

	//
	// Lock protecting against parallel refreshing of configuration
	// 
	WDFWAITLOCK ConfigurationDirectoryWatcherLock;

	struct
	{
		//
		// Cache for last received Small Motor Strength value
		// 
		UCHAR LightCache;

		//
		// Cache for last received Big Motor Strength value
		// 
		UCHAR HeavyCache;

        //
        // Defines if heavy rumble strength rescalling is requested
        //
        BOOLEAN HeavyRescaleEnabled;

        //
        // Defines if alternative rumble mode is enabled
        //
        BOOLEAN AltModeEnabled;

        //
        // Current state of heavy rumble rescaling parameters
        //
        DS_RESCALE_STATE HeavyRescale;

        //
        // Current state of light rumble rescaling parameters
        //
        DS_RESCALE_STATE LightRescale;

	} RumbleControlState;

} DEVICE_CONTEXT, * PDEVICE_CONTEXT;

//
// This macro will generate an inline function called DeviceGetContext
// which will be used to get a pointer to the device context memory
// in a type safe manner.
//
WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, DeviceGetContext)

typedef struct
{
	ULONG Unused;

} DMF_CONFIG_DsHidMini;

DECLARE_DMF_MODULE(DsHidMini)

typedef struct
{
	// 
	// Underlying VHIDMINI2 support.
	//
	DMFMODULE DmfModuleVirtualHidMini;

	//
	// Input report (packet format depends on chosen HID mode)
	// 
	UCHAR InputReport[DS3_COMMON_MAX_HID_INPUT_REPORT_SIZE];

	//
	// Raw input report for SIXAXIS.SYS GET_FEATURE report
	// 
	DS3_RAW_INPUT_REPORT GetFeatureReport;

#ifdef DSHM_FEATURE_FFB
	//
	// Force Feedback State Info
	// 
	PFFB_ATTRIBUTES FfbAttributes;

	//
	// Hash table holding FFB effect info
	// 
	DMFMODULE DmfModuleForceFeedback;
#endif
	
} DMF_CONTEXT_DsHidMini;

_Function_class_(DMF_ChildModulesAdd)
_IRQL_requires_max_(PASSIVE_LEVEL)
VOID
DMF_DsHidMini_ChildModulesAdd(
	_In_ DMFMODULE DmfModule,
	_In_ DMF_MODULE_ATTRIBUTES* DmfParentModuleAttributes,
	_In_ PDMFMODULE_INIT DmfModuleInit
);

_Function_class_(DMF_Open)
_IRQL_requires_max_(PASSIVE_LEVEL)
_Must_inspect_result_
static
NTSTATUS
DMF_DsHidMini_Open(
	_In_ DMFMODULE DmfModule
);

_Function_class_(DMF_Close)
_IRQL_requires_max_(PASSIVE_LEVEL)
static
VOID
DMF_DsHidMini_Close(
	_In_ DMFMODULE DmfModule
);


EVT_DMF_ThreadedBufferQueue_Callback DMF_EvtExecuteOutputPacketReceived;

EVT_WDF_TIMER DSHM_OutputReportDelayTimerElapsed;

EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL DSHM_EvtWdfIoQueueIoDeviceControl;

NTSTATUS
DsDevice_ReadProperties(
	WDFDEVICE Device
);

VOID CALLBACK
DsDevice_HotReloadEventCallback(
	_In_ PVOID   lpParameter,
	_In_ BOOLEAN TimerOrWaitFired
);

NTSTATUS
DsDevice_InitContext(
	WDFDEVICE Device
);

NTSTATUS
DsDevice_IsUsbDevice(
	PWDFDEVICE_INIT DeviceInit,
	PBOOLEAN Result
);

void
DsDevice_RegisterHotReloadListener(
	PDEVICE_CONTEXT Context
);

void
DsDevice_RegisterBthDisconnectListener(
	PDEVICE_CONTEXT Context
);

void
DsDevice_InvokeLocalBthDisconnect(
	PDEVICE_CONTEXT Context
);

EXTERN_C_END
