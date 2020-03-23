/*++

Module Name:

    device.h

Abstract:

    This file contains the device definitions.

Environment:

    User-mode Driver Framework 2

--*/

#include "public.h"
#include <DmfModule.h>


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
    // USB Interrupt (out) pipe handle
    // 
    WDFUSBPIPE InterruptOutPipe;
};

struct BTH_DEVICE_CONTEXT
{
    WDFIOTARGET BthIoTarget;

    WDFREQUEST HidControlReadRequest;

	WDFMEMORY HidControlReadMemory;

    WDFREQUEST HidControlWriteRequest;

    WDFMEMORY HidControlWriteMemory;

    WDFREQUEST HidInterruptReadRequest;

    WDFMEMORY HidInterruptReadMemory;

    WDFREQUEST HidInterruptWriteRequest;

    WDFMEMORY HidInterruptWriteMemory;
};

VOID FORCEINLINE DS_DRIVER_CONFIGURATION_INIT_DEFAULTS(
    PDS_DRIVER_CONFIGURATION Configuration
)
{
    Configuration->HidDeviceMode = DsHidMiniDeviceModeSixaxisCompatible;
    Configuration->MuteDigitalPressureButtons = FALSE;
    Configuration->VendorId = 0x054C;
    Configuration->ProductId = 0x0268;
    Configuration->VersionNumber = 0x0101;
}

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

} DEVICE_CONTEXT, *PDEVICE_CONTEXT;

//
// This macro will generate an inline function called DeviceGetContext
// which will be used to get a pointer to the device context memory
// in a type safe manner.
//
WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, DeviceGetContext)

typedef struct
{
    ULONG Dummy;
	
} DMF_CONFIG_DsHidMini;

DECLARE_DMF_MODULE(DsHidMini)

typedef struct
{
    // Underlying VHIDMINI2 support.
    //
    DMFMODULE DmfModuleVirtualHidMini;

	//
	// Input report
	// 
    UCHAR InputReport[DS3_HID_INPUT_REPORT_SIZE];
	
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

//
// Function to initialize the device and its callbacks
//
NTSTATUS
dshidminiCreateDevice(
    _Inout_ PWDFDEVICE_INIT DeviceInit
    );

NTSTATUS DsHidMini_BthConnectionContextInit(
    WDFDEVICE Device
);

EXTERN_C_END
