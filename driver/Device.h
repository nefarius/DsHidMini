#pragma once
#include <DmfModule.h>
#include "DsIdentification.h"
#include "DsMotion.h"


EXTERN_C_START

#define DSHM_NAMED_EVENT_DISCONNECT			L"Global\\DsHidMiniDisconnectEvent%ls"
#define DSHM_NAMED_MUTEX_DISCONNECT			L"Global\\DsHidMiniDisconnectLock%ls"

//
// Local System, Local Service (WUDFHost), and Administrators only.
// Authenticated users must not be able to signal or spoof these objects.
// 
#define DSHM_HOST_NAMED_OBJECT_SDDL \
	TEXT("D:(A;;0x001F0003;;;SY)(A;;0x001F0003;;;LS)(A;;0x001F0003;;;BA)")

#define DSHM_DEVICE_ADDRESS_CCH				13
#define DSHM_NAMED_EVENT_NAME_CCH			64
#define DSHM_BTH_DISCONNECT_RETRY_COUNT		3
#define DSHM_BTH_DISCONNECT_RETRY_DELAY_MS	300
#define DSHM_BTH_DISCONNECT_LOCK_TIMEOUT_MS	2000

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
	// USB Interrupt (out) pipe handle. NULL if the device does not expose one
	// (see issue #321); OutputTransport will then be forced to ControlEndpoint.
	// 
	WDFUSBPIPE InterruptOutPipe;

	//
	// Transport actually used to send output reports on this device, resolved
	// once in DsUsb_PrepareHardware from configuration and pipe availability.
	// Never Auto at runtime.
	// 
	DS_USB_OUTPUT_REPORT_TRANSPORT OutputTransport;

	//
	// Timestamp to calculate charging cycle state change
	// 
	LARGE_INTEGER ChargingCycleTimestamp;

	//
	// Retries signalling the wireless instance when it has not created
	// its disconnect event yet (arrival race).
	// 
	WDFTIMER DisconnectRetryTimer;

	//
	// Remaining off-thread attempts after the initial PrepareHardware try.
	// 
	ULONG DisconnectRetryRemaining;
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
		// Delayed startup timer
		// 
		WDFTIMER StartupDelay;

		//
		// Post-delayed start timer
		// 
		WDFTIMER PostStartupTasks;

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
 * @author	Benjamin "Nefarius" H?glinger-Stelzer
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
 * @author	Benjamin "Nefarius" H?glinger-Stelzer
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
// Stores the constants used for rumble rescaling and if it is allowed
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

	//
	// Set once an input report has been dropped because the DsHidMini or
	// VirtualHidMini DMF Module was unavailable (closing/closed), so that
	// only the first drop per power cycle gets logged instead of flooding
	// the trace at report rate. Reset in DsHidMini_EvtDeviceD0Entry.
	// 
	BOOLEAN InputReportDropLogged;

	//
	// Timer used to defer a self re-enumeration request off the D0Entry
	// path when DMF_DsHidMini_Open detects that the HID mode loaded from
	// configuration differs from the mode already exposed via
	// DEVPKEY_DsHidMini_RW_HidDeviceMode (see issue #374).
	// 
	WDFTIMER HidModeRestartTimer;

	//
	// Set once a self re-enumeration has been requested because of a HID
	// mode mismatch, so this power-up only asks for it once. Reset in
	// DsHidMini_EvtDeviceD0Entry.
	// 
	BOOLEAN HidModeRestartRequested;
	
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
	// TRUE if DeviceAddress was not reported by the hardware (GET Feature
	// 0xF2 failed on all retries) and was synthesized instead. See issue #321.
	// 
	BOOLEAN DeviceAddressSynthesized;

		//
		// TRUE once GET Feature 0xF2 (device address) has succeeded at least once
		// this power-up. Gates host-address discovery, Bluetooth pairing and the
		// wireless-instance disconnect signal, none of which are meaningful for a
		// device that never reported a Bluetooth MAC of its own. See issue #321.
		// 
		BOOLEAN SupportsBluetoothAddressReports;

		//
		// Feature 0x01 identification decoded at USB PrepareHardware. Present is
		// FALSE for Bluetooth instances and when GET or parse failed. See issue #50.
		// 
		BOOLEAN IdentificationPresent;
		DS_IDENTIFICATION_INFO Identification;

	//
	// Motion calibration, gyro tracker, and last corrected sample.
	// Seeded with nominal values; USB PrepareHardware may replace them
	// from EEPROM page 0xA0. See docs/MOTION.md and issue #217.
	// 
	DS_MOTION_STATE Motion;

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
	// Hardware family derived from VendorId/ProductId. Navigation (PID
	// 0x042F) has one LED and no rumble; see issue #48.
	// 
	DS_DEVICE_TYPE DeviceType;

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
		// Defines if heavy rumble strength rescaling is requested
		//
		BOOLEAN HeavyRescaleEnabled;

		struct {

			//
			// Defines if alternative rumble mode is enabled
			//
			BOOLEAN IsEnabled;

			//
			// Current state of light rumble rescaling parameters
			//
			DS_RESCALE_STATE LightRescale;

			//
			// Allows toggling to occur if the button combo conditions are satisfied
			//
			BOOLEAN IsToggleAllowed;

			//
			// Timestamp to calculate alt mode toggle combo detection
			//
			LARGE_INTEGER QuickToggleTimestamp;

		} AltMode;

		//
		// Current state of heavy rumble rescaling parameters
		//
		DS_RESCALE_STATE HeavyRescale;

		//
		// Periodically re-sends the current output report while at least
		// one motor is active, so the finite motor duration written by
		// DS3_PROCESS_RUMBLE_STRENGTH never lapses for as long as rumble
		// stays engaged (issue #356). Started/restarted from
		// DS3_PROCESS_RUMBLE_STRENGTH, stopped once both motors go quiet
		// and on device power-down (see driver/Power.c).
		//
		WDFTIMER RumbleKeepAliveTimer;

		//
		// Set by DsHidMini_EvtDeviceD0Exit/DsHidMini_EvtDeviceReleaseHardware
		// before RumbleKeepAliveTimer is stopped, and checked by
		// DS3_PROCESS_RUMBLE_STRENGTH before (re-)arming it, so a rumble
		// write racing with power-down cannot re-arm the timer after
		// teardown has decided to stop it (issue #356). Reset per power
		// cycle in DsHidMini_EvtDeviceD0Entry, matching the
		// InputReportDropLogged/HidModeRestartRequested latches.
		//
		BOOLEAN IsTearingDown;

	} RumbleControlState;

	UINT32 SlotIndex;

	struct
	{
		HANDLE InputReportWaitHandle;
	} IPC;

} DEVICE_CONTEXT, * PDEVICE_CONTEXT;

#include <pshpack1.h>
//
// Describes a raw input report packet shared via IPC
// 
typedef struct _IPC_HID_INPUT_REPORT_MESSAGE
{
	//
	// One-based device index
	// 
	UINT32 SlotIndex;

	//
	// Seqlock: odd = write in progress, even = stable snapshot
	// 
	volatile LONG SequenceNumber;

	//
	// Input report copy
	// 
	DS3_RAW_INPUT_REPORT InputReport;

	//
	// Pad so each slot is a multiple of 4 and SequenceNumber stays aligned
	// at (slot - 1) * sizeof(...) + FIELD_OFFSET(..., SequenceNumber).
	// 
	UCHAR AlignmentPadding[3];
} IPC_HID_INPUT_REPORT_MESSAGE, *PIPC_HID_INPUT_REPORT_MESSAGE;
#include <poppack.h>

//
// Packed layout: UINT32 + LONG + 49-byte DS3_RAW_INPUT_REPORT + 3-byte pad.
// Must stay in sync with the SDK IPC_HID_INPUT_REPORT_MESSAGE mirror (Pack = 1).
// 
C_ASSERT(sizeof(IPC_HID_INPUT_REPORT_MESSAGE) == 60);
C_ASSERT((FIELD_OFFSET(IPC_HID_INPUT_REPORT_MESSAGE, SequenceNumber) % sizeof(LONG)) == 0);
C_ASSERT((sizeof(IPC_HID_INPUT_REPORT_MESSAGE) % sizeof(LONG)) == 0);

//
// This macro will generate an inline function called DeviceGetContext
// which will be used to get a pointer to the device context memory
// in a type safe manner.
//
WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, DeviceGetContext)

DECLARE_DMF_MODULE_NO_CONFIG(DsHidMini)

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

DMF_Open DMF_DsHidMini_Open;

EVT_DMF_ThreadedBufferQueue_Callback DSHM_EvtExecuteOutputPacketReceived;

EVT_WDF_TIMER DSHM_OutputReportDelayTimerElapsed;

EVT_WDF_TIMER DsDevice_EvtHidModeRestartTimerFunc;

EVT_WDF_TIMER DsDevice_EvtBthDisconnectRetryTimerFunc;

EVT_WDF_TIMER DS3_EvtRumbleKeepAliveTimerFunc;

EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL DSHM_EvtWdfIoQueueIoDeviceControl;

EVT_DSHM_IPC_DispatchDeviceMessage DSHM_EvtDispatchDeviceMessage;

NTSTATUS
DsDevice_ReadProperties(
	WDFDEVICE Device
);

VOID
DsDevice_AssignDeviceType(
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

void
DsDevice_FormatCanonicalAddress(
	_In_ PDEVICE_CONTEXT Context,
	_Out_writes_(BufferChars) PWCHAR Buffer,
	_In_ size_t BufferChars
);

BOOLEAN
DsDevice_IsWiredInstancePresent(
	_In_ PDEVICE_CONTEXT Context
);

EXTERN_C_END
