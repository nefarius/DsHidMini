#pragma once
#include <DmfModule.h>
#include "uthash.h"


EXTERN_C_START

struct USB_DEVICE_CONTEXT
{
	//
	// Framework USB object
	// 
	WDFUSBDEVICE UsbDevice;

	//
	// USB interface object
	// 
	WDFUSBINTERFACE UsbInterface;

	//
	// USB Interrupt (in) pipe handle
	// 
	WDFUSBPIPE InterruptInPipe;

	//
	// Output report buffer
	// 
	PUCHAR OutputReport;

	//
	// Timestamp to calculate charging cycle state change
	// 
	LARGE_INTEGER ChargingCycleTimestamp;
};

struct BTH_DEVICE_CONTEXT
{
	//
	// Bluetooth PDO I/O target
	// 
	WDFIOTARGET BthIoTarget;

	struct
	{
		//
		// Reusable Request for HID Control IN channel
		// 
		WDFREQUEST ReadRequest;

		//
		// Memory for HID Control IN channel
		// 
		WDFMEMORY ReadMemory;

		//
		// Reusable Request for HID Control OUT channel
		// 
		WDFREQUEST WriteRequest;

		//
		// Memory for HID Control OUT channel
		// 
		WDFMEMORY WriteMemory;

		//
		// Lock to protect async operation
		// 
		WDFWAITLOCK WriteLock;

	} HidControl;

	struct
	{
		//
		// Reusable Request for HID Interrupt IN channel
		// 
		WDFREQUEST ReadRequest;

		//
		// Memory for HID Interrupt IN channel
		// 
		WDFMEMORY ReadMemory;

		//
		// Reusable Request for HID Interrupt OUT channel
		// 
		WDFREQUEST WriteRequest;
		
		//
		// Memory for HID Interrupt OUT channel
		// 
		WDFMEMORY WriteMemory;

	} HidInterrupt;

	struct
	{
		//
		// Periodic timer to consume pending memory
		// 
		WDFTIMER HidControlConsume;

		//
		// Delayed Output Report Timer
		// 
		WDFTIMER HidOutputReport;
				
	} Timers;
	
	//
	// Timestamp to calculate quick disconnect combo detection
	// 
	LARGE_INTEGER QuickDisconnectTimestamp;
};

//
// Sets default values for device configuration
// 
VOID FORCEINLINE DS_DRIVER_CONFIGURATION_INIT_DEFAULTS(
	PDS_DRIVER_CONFIGURATION Configuration
)
{
	Configuration->HidDeviceMode = DsHidMiniDeviceModeSixaxisCompatible;
	Configuration->MuteDigitalPressureButtons = FALSE;
	Configuration->VendorId = 0x054C;
	Configuration->ProductId = 0x0268;
	Configuration->VersionNumber = 0x0101;
	Configuration->DisableAutoPairing = FALSE;
}

#ifdef DSHM_FEATURE_FFB
typedef struct _FFB_ATTRIBUTES
{
	UCHAR EffectBlockIndex;

	PID_EFFECT_TYPE EffectType;

	BOOLEAN Reported;
	
	UT_hash_handle hh; /* makes this structure hashable */
	
} FFB_ATTRIBUTES, *PFFB_ATTRIBUTES;
#endif

typedef struct _DEVICE_CONTEXT
{
	//
	// Device instance ID
	// 
	WDFMEMORY InstanceId;

	//
	// VirtualHidMini DMF module
	// 
	DMFMODULE DsHidMiniModule;

	//
	// Periodic task scheduler to send output reports
	// 
	DMFMODULE OutputReportScheduler;
	
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
	// Current reported battery status
	// 
	DS_BATTERY_STATUS BatteryStatus;

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

	DS_DRIVER_CONFIGURATION Configuration;

} DEVICE_CONTEXT, * PDEVICE_CONTEXT;

//
// This macro will generate an inline function called DeviceGetContext
// which will be used to get a pointer to the device context memory
// in a type safe manner.
//
WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, DeviceGetContext)

EVT_WDF_OBJECT_CONTEXT_CLEANUP DsHidMini_EvtDeviceContextCleanup;

typedef struct
{
	ULONG Dummy;

} DMF_CONFIG_DsHidMini;

DECLARE_DMF_MODULE(DsHidMini)

typedef struct
{
	// 
	// Underlying VHIDMINI2 support.
	//
	DMFMODULE DmfModuleVirtualHidMini;

	//
	// Input report
	// 
	UCHAR InputReport[DS3_HID_INPUT_REPORT_SIZE];

#ifdef DSHM_FEATURE_FFB
	//
	// Force Feedback State Info
	// 
	PFFB_ATTRIBUTES FfbAttributes;
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


EVT_DMF_ScheduledTask_Callback DMF_OutputReportScheduledTaskCallback;

EXTERN_C_END
