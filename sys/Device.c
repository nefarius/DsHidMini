
#include "Driver.h"
#include "Device.tmh"
#include <DmfModule.h>
#include <devpkey.h>


EVT_DMF_DEVICE_MODULES_ADD DmfDeviceModulesAdd;

#pragma code_seg("PAGED")
DMF_DEFAULT_DRIVERCLEANUP(dshidminiEvtDriverContextCleanup)

//
// Bootstrap device
// 
NTSTATUS
dshidminiEvtDeviceAdd(
	_In_    WDFDRIVER       Driver,
	_Inout_ PWDFDEVICE_INIT DeviceInit
)
{
	WDF_OBJECT_ATTRIBUTES			deviceAttributes;
	WDFDEVICE						device;
	NTSTATUS						status;
	PDMFDEVICE_INIT					dmfDeviceInit;
	DMF_EVENT_CALLBACKS				dmfCallbacks;
	WDF_PNPPOWER_EVENT_CALLBACKS	pnpPowerCallbacks;
	PDEVICE_CONTEXT					pDevCtx;
	WDFQUEUE						queue;
	WDF_IO_QUEUE_CONFIG				queueConfig;	

	UNREFERENCED_PARAMETER(Driver);

	FuncEntry(TRACE_DEVICE);

	dmfDeviceInit = DMF_DmfDeviceInitAllocate(DeviceInit);

	WDF_PNPPOWER_EVENT_CALLBACKS_INIT(&pnpPowerCallbacks);
	pnpPowerCallbacks.EvtDeviceSelfManagedIoInit = DsHidMini_EvtWdfDeviceSelfManagedIoInit;
	pnpPowerCallbacks.EvtDevicePrepareHardware = DsHidMini_EvtDevicePrepareHardware;
	pnpPowerCallbacks.EvtDeviceD0Entry = DsHidMini_EvtDeviceD0Entry;
	pnpPowerCallbacks.EvtDeviceD0Exit = DsHidMini_EvtDeviceD0Exit;

	// All DMF drivers must call this function even if they do not support PnP Power callbacks.
	// (In this case, this driver does support a PnP Power callback.)
	//
	DMF_DmfDeviceInitHookPnpPowerEventCallbacks(dmfDeviceInit,
		&pnpPowerCallbacks);

	// All DMF drivers must call this function even if they do not support File Object callbacks.
	//
	DMF_DmfDeviceInitHookFileObjectConfig(dmfDeviceInit,
		NULL);

	// All DMF drivers must call this function even if they do not support Power Policy callbacks.
	//
	DMF_DmfDeviceInitHookPowerPolicyEventCallbacks(dmfDeviceInit,
		NULL);

	// This is a filter driver that loads on MSHIDUMDF driver.
	//
	WdfFdoInitSetFilter(DeviceInit);
	// DMF Client drivers that are filter drivers must also make this call.
	//
	DMF_DmfFdoSetFilter(dmfDeviceInit);

	WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&deviceAttributes, DEVICE_CONTEXT);
	deviceAttributes.EvtCleanupCallback = DsHidMini_EvtDeviceContextCleanup;

	status = WdfDeviceCreate(&DeviceInit, &deviceAttributes, &device);

	do
	{
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfDeviceCreate failed with status %!STATUS!",
				status
			);
			break;
		}

		//
		// Read device properties
		// 	
		status = DsDevice_ReadProperties(device);
		if (!NT_SUCCESS(status))
		{
			break;
		}

		pDevCtx = DeviceGetContext(device);

		//
		// Bluetooth-specific initialization
		// 
		if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
		{
			pDevCtx->Connection.Bth.BthIoTarget = WdfDeviceGetIoTarget(device);

			//
			// Initialize all necessary BTH-specific objects
			// 
			status = DsHidMini_BthConnectionContextInit(device);

			if (!NT_SUCCESS(status))
			{
				TraceError(
					TRACE_DEVICE,
					"DsHidMini_BthConnectionContextInit failed with status %!STATUS!",
					status
				);
				break;
			}
		}

		//
		// Provide and hook our own default queue to handle weird cases
		//

		WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&queueConfig, WdfIoQueueDispatchParallel);
		queueConfig.PowerManaged = WdfTrue;
		queueConfig.EvtIoDeviceControl = DSHM_EvtWdfIoQueueIoDeviceControl;
		DMF_DmfDeviceInitHookQueueConfig(dmfDeviceInit, &queueConfig);

		status = WdfIoQueueCreate(
			device,
			&queueConfig,
			WDF_NO_OBJECT_ATTRIBUTES,
			&queue
		);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfIoQueueCreate failed with status %!STATUS!",
				status
			);
			break;
		}

		//
		// Create lock
		// 
				
		WDF_OBJECT_ATTRIBUTES_INIT(&deviceAttributes);
		deviceAttributes.ParentObject = device;
		
		status = WdfWaitLockCreate(
			&deviceAttributes,
			&pDevCtx->OutputReport.Lock
		);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfWaitLockCreate failed with status %!STATUS!",
				status
			);
			break;
		}
		
		//
		// Expose interface for applications to find us
		// 

		status = WdfDeviceCreateDeviceInterface(
			device,
			&GUID_DEVINTERFACE_DSHIDMINI,
			NULL
		);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfDeviceCreateDeviceInterface failed with status %!STATUS!",
				status
			);
			break;
		}

		// Create the DMF Modules this Client driver will use.
		//
		dmfCallbacks.EvtDmfDeviceModulesAdd = DmfDeviceModulesAdd;
		DMF_DmfDeviceInitSetEventCallbacks(
			dmfDeviceInit,
			&dmfCallbacks
		);

		status = DMF_ModulesCreate(
			device,
			&dmfDeviceInit
		);
		if (!NT_SUCCESS(status))
		{
			break;
		}
	} while (FALSE);

	if (dmfDeviceInit != NULL)
	{
		DMF_DmfDeviceInitFree(&dmfDeviceInit);
	}

	FuncExit(TRACE_DEVICE, "status=%!STATUS!", status);

	return status;
}
#pragma code_seg()

//
// Read device properties
// 
NTSTATUS DsDevice_ReadProperties(WDFDEVICE Device)
{
	NTSTATUS status;
	WCHAR enumeratorName[200];
	ULONG bufSize;
	WDF_DEVICE_PROPERTY_DATA devProp;
	DEVPROPTYPE propType;
	WDF_OBJECT_ATTRIBUTES attributes;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DEVICE);
	
	do
	{
		//
		// Query enumerator name to discover connection type
		// 
		status = WdfDeviceQueryProperty(
			Device,
			DevicePropertyEnumeratorName,
			ARRAYSIZE(enumeratorName),
			(PVOID)enumeratorName,
			&bufSize
		);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfDeviceQueryProperty failed with status %!STATUS!",
				status
			);
			break;
		}
		
		//
		// Early device type detection, using enumerator name
		// 
		if (_wcsicmp(L"USB", enumeratorName) == 0)
		{
			pDevCtx->ConnectionType = DsDeviceConnectionTypeUsb;
		}
		else
		{
			pDevCtx->ConnectionType = DsDeviceConnectionTypeBth;
		}

		WDF_DEVICE_PROPERTY_DATA_INIT(&devProp, &DEVPKEY_Device_InstanceId);
		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		//
		// Query device instance string for configuration
		// TODO: deprecated
		// 
		status = WdfDeviceAllocAndQueryPropertyEx(
			Device,
			&devProp,
			NonPagedPoolNx,
			&attributes,
			&pDevCtx->InstanceId,
			&propType
		);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfDeviceAllocAndQueryPropertyEx failed with status %!STATUS!",
				status
			);
			break;
		}

		TraceVerbose(
			TRACE_DEVICE,
			"DEVPKEY_Device_InstanceId: %ws",
			WdfMemoryGetBuffer(pDevCtx->InstanceId, NULL)
		);
		
		//
		// Fetch and convert device address from device property
		// 
		if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
		{
			WDFMEMORY deviceAddressMemory;
			WDF_DEVICE_PROPERTY_DATA_INIT(&devProp, &DEVPKEY_Bluetooth_DeviceAddress);

			//
			// Get device property (returns wide hex string)
			// 
			status = WdfDeviceAllocAndQueryPropertyEx(
				Device,
				&devProp,
				NonPagedPoolNx,
				WDF_NO_OBJECT_ATTRIBUTES,
				&deviceAddressMemory,
				&propType
			);
			if (!NT_SUCCESS(status))
			{
				TraceError(
					TRACE_DEVICE,
					"WdfDeviceAllocAndQueryPropertyEx failed with status %!STATUS!",
					status
				);
				break;
			}

			//
			// Convert hex string into UINT64
			// 
			UINT64 hostAddress = wcstoull(
				WdfMemoryGetBuffer(deviceAddressMemory, NULL),
				L'\0',
				16
			);

			WdfObjectDelete(deviceAddressMemory);

			//
			// Convert to MAC address type
			// 

			pDevCtx->DeviceAddress.Address[0] = (UCHAR)((hostAddress >> (8 * 0)) & 0xFF);
			pDevCtx->DeviceAddress.Address[1] = (UCHAR)((hostAddress >> (8 * 1)) & 0xFF);
			pDevCtx->DeviceAddress.Address[2] = (UCHAR)((hostAddress >> (8 * 2)) & 0xFF);
			pDevCtx->DeviceAddress.Address[3] = (UCHAR)((hostAddress >> (8 * 3)) & 0xFF);
			pDevCtx->DeviceAddress.Address[4] = (UCHAR)((hostAddress >> (8 * 4)) & 0xFF);
			pDevCtx->DeviceAddress.Address[5] = (UCHAR)((hostAddress >> (8 * 5)) & 0xFF);

			TraceVerbose(
				TRACE_DEVICE,
				"Device address: %012llX",
				*(PULONGLONG)&pDevCtx->DeviceAddress
			);
		}
	} while (FALSE);

	FuncExit(TRACE_DEVICE, "status=%!STATUS!", status);
	
	return status;
}

VOID CALLBACK 
DsDevice_HotRealodEventCallback(
	_In_ PVOID   lpParameter,
	_In_ BOOLEAN TimerOrWaitFired
) 
{
	PDEVICE_CONTEXT pDevCtx = (PDEVICE_CONTEXT)lpParameter;
	UNREFERENCED_PARAMETER(TimerOrWaitFired);

	DsDevice_HotReloadConfiguration(pDevCtx);
}

//
// Read device properties which can be refreshed during runtime
//
VOID DsDevice_HotReloadConfiguration(PDEVICE_CONTEXT Context)
{
	WDF_DEVICE_PROPERTY_DATA propData;
	DEVPROPTYPE propType;
	ULONG requiredSize = 0;
	WDFDEVICE device = WdfObjectContextGetObject(Context);

	TraceVerbose(
		TRACE_DEVICE,
		"Hot-reload triggered"
	);

	WDF_DEVICE_PROPERTY_DATA_INIT(&propData, &DEVPKEY_DsHidMini_MuteDigitalPressureButtons);

	(void)WdfDeviceQueryPropertyEx(
		device,
		&propData,
		sizeof(UCHAR),
		&Context->Configuration.MuteDigitalPressureButtons,
		&requiredSize,
		&propType
	);
}	

//
// Free context memory
// 
void DsHidMini_EvtDeviceContextCleanup(
	WDFOBJECT Object
)
{
	FuncEntry(TRACE_DEVICE);
	
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Object);

	UNREFERENCED_PARAMETER(pDevCtx);

	FuncExitNoReturn(TRACE_DEVICE);
}

//
// Bootstrap our own module
// 
#pragma code_seg("PAGED")
_IRQL_requires_max_(PASSIVE_LEVEL)
VOID
DmfDeviceModulesAdd(
	_In_ WDFDEVICE Device,
	_In_ PDMFMODULE_INIT DmfModuleInit
)
{
	PDEVICE_CONTEXT pDevCtx;
	DMF_MODULE_ATTRIBUTES moduleAttributes;
	DMF_CONFIG_DsHidMini dsHidMiniCfg;
	DMF_CONFIG_ScheduledTask dmfSchedulerCfg;

	PAGED_CODE();

	FuncEntry(TRACE_DEVICE);

	pDevCtx = DeviceGetContext(Device);

	//
	// Scheduler for serialized periodic output report dispatcher
	// 

	DMF_CONFIG_ScheduledTask_AND_ATTRIBUTES_INIT(
		&dmfSchedulerCfg,
		&moduleAttributes
	);

	dmfSchedulerCfg.EvtScheduledTaskCallback = DMF_OutputReportScheduledTaskCallback;
	dmfSchedulerCfg.CallbackContext = pDevCtx;
	dmfSchedulerCfg.PersistenceType = ScheduledTask_Persistence_NotPersistentAcrossReboots;
	dmfSchedulerCfg.ExecutionMode = ScheduledTask_ExecutionMode_Deferred;
	dmfSchedulerCfg.ExecuteWhen = ScheduledTask_ExecuteWhen_Other; // Don't use timer
	dmfSchedulerCfg.TimeMsBeforeInitialCall = 1000;
	
	DMF_DmfModuleAdd(
		DmfModuleInit,
		&moduleAttributes,
		WDF_NO_OBJECT_ATTRIBUTES,
		&pDevCtx->OutputReport.Scheduler
	);

	//
	// Virtual HID Mini Module
	// 
	
	DMF_CONFIG_DsHidMini_AND_ATTRIBUTES_INIT(
		&dsHidMiniCfg,
		&moduleAttributes
	);

	DMF_DmfModuleAdd(
		DmfModuleInit,
		&moduleAttributes,
		WDF_NO_OBJECT_ATTRIBUTES,
		&pDevCtx->DsHidMiniModule
	);

	FuncExitNoReturn(TRACE_DEVICE);
}
#pragma code_seg()

void DSHM_EvtWdfIoQueueIoDeviceControl(
	WDFQUEUE Queue,
	WDFREQUEST Request,
	size_t OutputBufferLength,
	size_t InputBufferLength,
	ULONG IoControlCode
)
{
	NTSTATUS status = STATUS_NOT_IMPLEMENTED;

	UNREFERENCED_PARAMETER(Queue);
	UNREFERENCED_PARAMETER(OutputBufferLength);
	UNREFERENCED_PARAMETER(InputBufferLength);

	FuncEntry(TRACE_DEVICE);

	switch (IoControlCode)
	{
	case IOCTL_HID_DEVICERESET_NOTIFICATION:
		TraceVerbose(
			TRACE_DEVICE,
			"IOCTL_HID_DEVICERESET_NOTIFICATION not supported"
		);
		status = STATUS_NOT_SUPPORTED;
		break;
	default:
		TraceVerbose(TRACE_DEVICE, "Unhandled I/O control code 0x%X", IoControlCode);
		break;
	}

	FuncExitNoReturn(TRACE_DEVICE);

	WdfRequestComplete(Request, status);
}
