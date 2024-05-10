#include "Driver.h"
#include "Device.tmh"
#include <DmfModule.h>
#include <devpkey.h>


EVT_DMF_DEVICE_MODULES_ADD DmfDeviceModulesAdd;

EVT_WDF_DEVICE_CONTEXT_CLEANUP DsHidMiniDeviceCleanup;

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
	WDF_OBJECT_ATTRIBUTES deviceAttributes;
	WDFDEVICE device;
	NTSTATUS status;
	PDMFDEVICE_INIT dmfDeviceInit;
	DMF_EVENT_CALLBACKS dmfCallbacks;
	WDF_PNPPOWER_EVENT_CALLBACKS pnpPowerCallbacks;
	PDEVICE_CONTEXT pDevCtx;
	WDFQUEUE queue;
	WDF_IO_QUEUE_CONFIG queueConfig;
	BOOLEAN ret;


	UNREFERENCED_PARAMETER(Driver);

	PAGED_CODE();

	FuncEntry(TRACE_DEVICE);

	dmfDeviceInit = DMF_DmfDeviceInitAllocate(DeviceInit);

	WDF_PNPPOWER_EVENT_CALLBACKS_INIT(&pnpPowerCallbacks);

	//
	// Callbacks only relevant to Bluetooth
	// 
	if ((NT_SUCCESS(DsDevice_IsUsbDevice(DeviceInit, &ret)) && !ret))
	{
		pnpPowerCallbacks.EvtDeviceSelfManagedIoInit = DsHidMini_EvtWdfDeviceSelfManagedIoInit;
		pnpPowerCallbacks.EvtDeviceSelfManagedIoSuspend = DsHidMini_EvtWdfDeviceSelfManagedIoSuspend;
	}

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
	deviceAttributes.EvtCleanupCallback = DsHidMiniDeviceCleanup;

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
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfDeviceCreate", status);
			break;
		}

		//
		// Read device properties
		// 	
		if (!NT_SUCCESS(status = DsDevice_ReadProperties(device)))
		{
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DsDevice_ReadProperties", status);
			break;
		}

		pDevCtx = DeviceGetContext(device);

		//
		// Initialize context
		// 
		if (!NT_SUCCESS(status = DsDevice_InitContext(device)))
		{
			TraceError(
				TRACE_DEVICE,
				"DsDevice_InitContext failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DsDevice_InitContext", status);
			break;
		}

		if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
		{
			//
			// Provide and hook our own default queue to handle weird cases
			//

			WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&queueConfig, WdfIoQueueDispatchParallel);
			queueConfig.PowerManaged = WdfTrue;
			queueConfig.EvtIoDeviceControl = DSHM_EvtWdfIoQueueIoDeviceControl;
			DMF_DmfDeviceInitHookQueueConfig(dmfDeviceInit, &queueConfig);

			if (!NT_SUCCESS(status = WdfIoQueueCreate(
				device,
				&queueConfig,
				WDF_NO_OBJECT_ATTRIBUTES,
				&queue
			)))
			{
				TraceError(
					TRACE_DEVICE,
					"WdfIoQueueCreate failed with status %!STATUS!",
					status
				);
				EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfIoQueueCreate", status);
				break;
			}
		}

		//
		// Expose interface for applications to find us
		// 

		if (!NT_SUCCESS(status = WdfDeviceCreateDeviceInterface(
			device,
			&GUID_DEVINTERFACE_DSHIDMINI,
			NULL
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfDeviceCreateDeviceInterface failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfDeviceCreateDeviceInterface", status);
			break;
		}

		// Create the DMF Modules this Client driver will use.
		//
		dmfCallbacks.EvtDmfDeviceModulesAdd = DmfDeviceModulesAdd;
		DMF_DmfDeviceInitSetEventCallbacks(
			dmfDeviceInit,
			&dmfCallbacks
		);

		if (!NT_SUCCESS(status = DMF_ModulesCreate(
			device,
			&dmfDeviceInit
		)))
		{
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_ModulesCreate", status);
			break;
		}

	} while (FALSE);

	if (dmfDeviceInit != NULL)
	{
		DMF_DmfDeviceInitFree(&dmfDeviceInit);
	}

	if (!NT_SUCCESS(status) && device != NULL)
	{
		WdfObjectDelete(device);
	}

	EventWriteStartEvent(device, status);

	FuncExit(TRACE_DEVICE, "status=%!STATUS!", status);

	return status;
}
#pragma code_seg()

//
// Device context clean-up
//
#pragma code_seg("PAGED")
void DsHidMiniDeviceCleanup(
	WDFOBJECT Object
)
{
	FuncEntry(TRACE_DEVICE);

	PAGED_CODE();

	EventWriteUnloadEvent(Object);

	FuncExitNoReturn(TRACE_DEVICE);
}
#pragma code_seg()

//
// Read device properties available on device creation
// 
NTSTATUS DsDevice_ReadProperties(WDFDEVICE Device)
{
	NTSTATUS status;
	WCHAR enumeratorName[200];
	ULONG bufSize;
	WDF_DEVICE_PROPERTY_DATA devProp;
	DEVPROPTYPE propType;
	ULONG requiredSize = 0;
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
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DevicePropertyEnumeratorName", status);
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

		//
		// Fetch Bluetooth-specific properties
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
					"Requesting DEVPKEY_Bluetooth_DeviceAddress failed with status %!STATUS!",
					status
				);
				EventWriteFailedWithNTStatus(__FUNCTION__, L"DEVPKEY_Bluetooth_DeviceAddress", status);
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

			WDF_DEVICE_PROPERTY_DATA_INIT(&devProp, &DEVPKEY_Bluetooth_DeviceVID);

			status = WdfDeviceQueryPropertyEx(
				Device,
				&devProp,
				sizeof(USHORT),
				&pDevCtx->VendorId,
				&requiredSize,
				&propType
			);
			if (!NT_SUCCESS(status))
			{
				TraceError(
					TRACE_DEVICE,
					"Requesting DEVPKEY_Bluetooth_DeviceVID failed with %!STATUS!",
					status
				);
				EventWriteFailedWithNTStatus(__FUNCTION__, L"DEVPKEY_Bluetooth_DeviceVID", status);
				break;
			}

			TraceVerbose(TRACE_DEVICE, "[BTH] VID: 0x%04X", pDevCtx->VendorId);

			WDF_DEVICE_PROPERTY_DATA_INIT(&devProp, &DEVPKEY_Bluetooth_DevicePID);

			status = WdfDeviceQueryPropertyEx(
				Device,
				&devProp,
				sizeof(USHORT),
				&pDevCtx->ProductId,
				&requiredSize,
				&propType
			);
			if (!NT_SUCCESS(status))
			{
				TraceError(
					TRACE_DEVICE,
					"Requesting DEVPKEY_Bluetooth_DevicePID failed with %!STATUS!",
					status
				);
				EventWriteFailedWithNTStatus(__FUNCTION__, L"DEVPKEY_Bluetooth_DevicePID", status);
				break;
			}

			TraceVerbose(TRACE_DEVICE, "[BTH] PID: 0x%04X", pDevCtx->ProductId);

			DsDevice_RegisterBthDisconnectListener(pDevCtx);

			DsDevice_RegisterHotReloadListener(pDevCtx);

            sprintf_s(
                pDevCtx->DeviceAddressString,
                ARRAYSIZE(pDevCtx->DeviceAddressString),
                "%02X%02X%02X%02X%02X%02X",
                pDevCtx->DeviceAddress.Address[5],
                pDevCtx->DeviceAddress.Address[4],
                pDevCtx->DeviceAddress.Address[3],
                pDevCtx->DeviceAddress.Address[2],
                pDevCtx->DeviceAddress.Address[1],
                pDevCtx->DeviceAddress.Address[0]
            );
		}
	} while (FALSE);

	FuncExit(TRACE_DEVICE, "status=%!STATUS!", status);

	return status;
}

//
// Initialize remaining device context fields
// 
NTSTATUS
DsDevice_InitContext(
	WDFDEVICE Device
)
{
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	NTSTATUS status = STATUS_SUCCESS;
	WDF_OBJECT_ATTRIBUTES attributes;
	PUCHAR outReportBuffer = NULL;
	WDF_TIMER_CONFIG timerCfg;

	FuncEntry(TRACE_DEVICE);

	switch (pDevCtx->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		//
		// Create managed memory object
		// 
		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		if (!NT_SUCCESS(status = WdfMemoryCreate(
			&attributes,
			NonPagedPoolNx,
			DS3_POOL_TAG,
			DS3_USB_HID_OUTPUT_REPORT_SIZE,
			&pDevCtx->OutputReportMemory,
			(PVOID*)&outReportBuffer
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfMemoryCreate failed with %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfMemoryCreate", status);
			break;
		}

		//
		// Fill with default report
		// 
		RtlCopyMemory(
			outReportBuffer,
			G_Ds3UsbHidOutputReport,
			DS3_USB_HID_OUTPUT_REPORT_SIZE
		);

		break;

	case DsDeviceConnectionTypeBth:

		//
		// Create managed memory object
		// 
		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		if (!NT_SUCCESS(status = WdfMemoryCreate(
			&attributes,
			NonPagedPoolNx,
			DS3_POOL_TAG,
			DS3_USB_HID_OUTPUT_REPORT_SIZE,
			&pDevCtx->OutputReportMemory,
			(PVOID*)&outReportBuffer
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfMemoryCreate failed with %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfMemoryCreate", status);
			break;
		}

		//
		// Fill with default report
		// 
		RtlCopyMemory(
			outReportBuffer,
			G_Ds3BthHidOutputReport,
			DS3_BTH_HID_OUTPUT_REPORT_SIZE
		);

		//
		// Turn flashing LEDs off
		// 
		DS3_BTH_SET_LED(outReportBuffer, DS3_LED_OFF);

		//
		// Output Report Delay
		// 

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		WDF_TIMER_CONFIG_INIT(
			&timerCfg,
			DsBth_EvtControlWriteTimerFunc
		);

		if (!NT_SUCCESS(status = WdfTimerCreate(
			&timerCfg,
			&attributes,
			&pDevCtx->Connection.Bth.Timers.HidOutputReport
		)))
		{
			TraceError(
				TRACE_DSBTH,
				"WdfTimerCreate (HidOutputReport) failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfTimerCreate", status);
			break;
		}

		break;
	}

	do
	{
		if (!NT_SUCCESS(status))
		{
			break;
		}

		//
		// Create lock
		// 

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		if (!NT_SUCCESS(status = WdfWaitLockCreate(
			&attributes,
			&pDevCtx->OutputReport.Lock
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfWaitLockCreate failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfWaitLockCreate", status);
			break;
		}

		//
		// Create lock
		// 

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		if (!NT_SUCCESS(status = WdfWaitLockCreate(
			&attributes,
			&pDevCtx->OutputReport.Cache.Lock
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfWaitLockCreate failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfWaitLockCreate", status);
			break;
		}

		//
		// Create lock
		// 

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		if (!NT_SUCCESS(status = WdfWaitLockCreate(
			&attributes,
			&pDevCtx->ConfigurationDirectoryWatcherLock
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfWaitLockCreate failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfWaitLockCreate", status);
			break;
		}

		//
		// Create timer
		// 

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		WDF_TIMER_CONFIG_INIT(
			&timerCfg,
			DSHM_OutputReportDelayTimerElapsed
		);

		if (!NT_SUCCESS(status = WdfTimerCreate(
			&timerCfg,
			&attributes,
			&pDevCtx->OutputReport.Cache.SendDelayTimer
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfTimerCreate failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfTimerCreate", status);
			break;
		}

	} while (FALSE);

	FuncExit(TRACE_DEVICE, "status=%!STATUS!", status);

	return status;
}

//
// Checks if this device is a USB device
// 
NTSTATUS
DsDevice_IsUsbDevice(
	PWDFDEVICE_INIT DeviceInit,
	PBOOLEAN Result
)
{
	NTSTATUS status;
	WCHAR enumeratorName[200];
	ULONG returnSize;
	UNICODE_STRING unicodeEnumName, temp;

	status = WdfFdoInitQueryProperty(
		DeviceInit,
		DevicePropertyEnumeratorName,
		sizeof(enumeratorName),
		enumeratorName,
		&returnSize
	);
	if (!NT_SUCCESS(status))
	{
		return status;
	}

	RtlInitUnicodeString(
		&unicodeEnumName,
		enumeratorName
	);

	RtlInitUnicodeString(
		&temp,
		L"USB"
	);

	if (Result)
		*Result = RtlCompareUnicodeString(&unicodeEnumName, &temp, TRUE) == 0;

	return status;
}

//
// Gets invoked when the hot-reload event got triggered from somewhere
// 
VOID CALLBACK
DsDevice_HotReloadEventCallback(
	_In_ PVOID   lpParameter,
	_In_ BOOLEAN TimerOrWaitFired
)
{
	FuncEntry(TRACE_DEVICE);

	LONGLONG timeout = 0;
	PDEVICE_CONTEXT pDevCtx = (PDEVICE_CONTEXT)lpParameter;
	UNREFERENCED_PARAMETER(TimerOrWaitFired);

	FindNextChangeNotification(pDevCtx->ConfigurationDirectoryWatcherEvent);

	do
	{
		//
		// Protect against parallel reads
		// 
		if (WdfWaitLockAcquire(pDevCtx->ConfigurationDirectoryWatcherLock, &timeout) != STATUS_SUCCESS)
		{
			TraceVerbose(
				TRACE_DEVICE,
				"Couldn't acquire lock, exiting"
			);

			break;
		}

		/*
		 * When this event is fired, the file might still be locked by the application
		 * that's written the change to it, so we simply wait a bit before attempting a read
		 */
		Sleep(100);

		TraceVerbose(
			TRACE_DEVICE,
			"Reloading configuration"
		);

		ConfigLoadForDevice(pDevCtx, TRUE);

		TraceVerbose(
			TRACE_DEVICE,
			"Reloaded configuration"
		);

		WdfWaitLockRelease(pDevCtx->ConfigurationDirectoryWatcherLock);

		//
		// If PairOnHotReload is enabled and not in disabled pairing mode then attempt pairing process followed by requesting currently set host address
		//
		if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb
			&& pDevCtx->Configuration.PairOnHotReload
			&& pDevCtx->Configuration.DevicePairingMode != DsDevicePairingModeDisabled)
		{
			WDFDEVICE wdfDev = DMF_ParentDeviceGet(pDevCtx->DsHidMiniModule);
			DsUsb_Ds3PairToHost(wdfDev);
			DsUsb_Ds3RequestHostAddress(wdfDev);
		}

		//
		// Changes to LED settings need to be pushed to the device
		// 
		(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverHighPriority);

	} while (FALSE);

	FuncExitNoReturn(TRACE_DEVICE);
}

/**
 * Registers an event listener to trigger refreshing runtime properties
 *
 * @author	Benjamin "Nefarius" Höglinger-Stelzer
 * @date	15.04.2021
 *
 * @param 	Context	The context.
 */
void DsDevice_RegisterHotReloadListener(PDEVICE_CONTEXT Context)
{
	CHAR programDataPath[MAX_PATH];
	CHAR configPath[MAX_PATH];

	FuncEntry(TRACE_DEVICE);

	do
	{
		if (Context->ConfigurationDirectoryWatcherEvent)
		{
			FindCloseChangeNotification(Context->ConfigurationDirectoryWatcherEvent);
			Context->ConfigurationDirectoryWatcherEvent = NULL;
		}

		if (GetEnvironmentVariableA(
			CONFIG_ENV_VAR_NAME,
			programDataPath,
			MAX_PATH
		) == 0)
		{
			break;
		}

		if (sprintf_s(
			configPath,
			MAX_PATH / sizeof(WCHAR),
			"%s\\%s",
			programDataPath,
			CONFIG_SUB_DIR_NAME
		) == -1)
		{
			break;
		}

        //
        // Check if file exists
        // 
        if (GetFileAttributesA(configPath) == INVALID_FILE_ATTRIBUTES)
        {
            TraceWarning(
                TRACE_DEVICE,
                "Configuration file %s not found, can't listen for changes",
                configPath
            );
            break;
        }

		Context->ConfigurationDirectoryWatcherEvent = FindFirstChangeNotificationA(
			configPath,
			FALSE,
			FILE_NOTIFY_CHANGE_LAST_WRITE
		);

		if (Context->ConfigurationDirectoryWatcherEvent == NULL)
		{
            const DWORD error = GetLastError();
			TraceError(
				TRACE_DEVICE,
				"FindFirstChangeNotificationA failed with error %!WINERROR!",
                error
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"FindFirstChangeNotificationA", error);
			break;
		}

		const BOOL ret = RegisterWaitForSingleObject(
			&Context->ConfigurationDirectoryWatcherWaitHandle,
			Context->ConfigurationDirectoryWatcherEvent,
			DsDevice_HotReloadEventCallback,
			Context,
			INFINITE,
			WT_EXECUTELONGFUNCTION
		);

		if (!ret)
		{
            const DWORD error = GetLastError();
			TraceError(
				TRACE_DEVICE,
				"RegisterWaitForSingleObject failed with error %!WINERROR!",
                error
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"RegisterWaitForSingleObject", error);
		}
	} while (FALSE);

	FuncExitNoReturn(TRACE_DEVICE);
}

/**
 * Register event to disconnect from Bluetooth, bypassing mshidumdf.sys
 *
 * @author	Benjamin "Nefarius" Höglinger-Stelzer
 * @date	15.04.2021
 *
 * @param 	Context	The context.
 */
void DsDevice_RegisterBthDisconnectListener(PDEVICE_CONTEXT Context)
{
	WCHAR dcEventName[44];
	WCHAR deviceAddress[13];

	FuncEntry(TRACE_DEVICE);

	swprintf_s(
		deviceAddress,
		ARRAYSIZE(deviceAddress),
		L"%012llX",
		*(PULONGLONG)&Context->DeviceAddress
	);

	swprintf_s(
		dcEventName,
		ARRAYSIZE(dcEventName),
		DSHM_NAMED_EVENT_DISCONNECT,
		deviceAddress
	);

	TraceVerbose(
		TRACE_DEVICE,
		"Disconnect event name: %ls",
		dcEventName
	);

	TCHAR* szSD = TEXT("D:(A;;0x001F0003;;;BA)(A;;0x00100002;;;AU)");
	SECURITY_ATTRIBUTES sa;
	sa.nLength = sizeof(SECURITY_ATTRIBUTES);
	sa.bInheritHandle = FALSE;
	ConvertStringSecurityDescriptorToSecurityDescriptor(
		szSD,
		SDDL_REVISION_1,
		&((&sa)->lpSecurityDescriptor),
		NULL
	);

	if (Context->Connection.Bth.DisconnectEvent) {
		CloseHandle(Context->Connection.Bth.DisconnectEvent);
		Context->Connection.Bth.DisconnectEvent = NULL;
	}

	Context->Connection.Bth.DisconnectEvent = CreateEventW(
		&sa,
		FALSE,
		FALSE,
		dcEventName
	);

	if (Context->Connection.Bth.DisconnectEvent == NULL)
	{
		TraceError(
			TRACE_DEVICE,
			"Failed to create disconnect event"
		);
		EventWriteFailedWithWin32Error(__FUNCTION__, L"CreateEventW", GetLastError());
	}

	const BOOL ret = RegisterWaitForSingleObject(
		&Context->Connection.Bth.DisconnectWaitHandle,
		Context->Connection.Bth.DisconnectEvent,
		DsBth_DisconnectEventCallback,
		Context,
		INFINITE,
		WT_EXECUTELONGFUNCTION | WT_EXECUTEONLYONCE
	);

	if (!ret)
	{
		TraceError(
			TRACE_DEVICE,
			"Failed to register wait for disconnect event"
		);
		EventWriteFailedWithWin32Error(__FUNCTION__, L"RegisterWaitForSingleObject", GetLastError());
	}

	FuncExitNoReturn(TRACE_DEVICE);
}

/**
 * Signals existing wireless connection with same device address to terminate. The controller
 * does not disconnect from Bluetooth on its own once connected to USB, so we signal the
 * wireless device object to disconnect itself before continuing with USB initialization.
 *
 * @author	Benjamin "Nefarius" Höglinger-Stelzer
 * @date	15.04.2021
 *
 * @param 	Context	The context.
 */
void DsDevice_InvokeLocalBthDisconnect(PDEVICE_CONTEXT Context)
{
	WCHAR deviceAddress[13];
	WCHAR dcEventName[44];

	//
	// Convert to expected hex string
	// 
	swprintf_s(
		deviceAddress,
		ARRAYSIZE(deviceAddress),
		L"%02X%02X%02X%02X%02X%02X",
		Context->DeviceAddress.Address[0],
		Context->DeviceAddress.Address[1],
		Context->DeviceAddress.Address[2],
		Context->DeviceAddress.Address[3],
		Context->DeviceAddress.Address[4],
		Context->DeviceAddress.Address[5]
	);

	//
	// Disconnect Bluetooth connection, if detected
	//

    (void)swprintf_s(
        dcEventName,
        ARRAYSIZE(dcEventName),
        DSHM_NAMED_EVENT_DISCONNECT,
        deviceAddress
    );

	const HANDLE dcEvent = OpenEventW(
		SYNCHRONIZE | EVENT_MODIFY_STATE,
		FALSE,
		dcEventName
	);

	if (dcEvent != NULL)
	{
		TraceVerbose(
			TRACE_DSUSB,
			"Found existing event %ls, signalling disconnect",
			dcEventName
		);

		SetEvent(dcEvent);
		CloseHandle(dcEvent);
	}
	else
	{
		DWORD error = GetLastError();

        // 
        // Event not present so assume no wireless instance is present
        // 
		if (error == ERROR_NOT_FOUND || error == ERROR_FILE_NOT_FOUND)
		{
			return;
		}

		TraceError(
			TRACE_DSUSB,
			"OpenEventW failed with %!WINERROR!",
			error
		);
	}
}

//
// Bootstrap required DMF modules
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
	DMF_CONFIG_ThreadedBufferQueue dmfBufferCfg;
	DMF_CONFIG_DefaultTarget bthReaderCfg;
	DMF_CONFIG_DefaultTarget bthWriterCfg;

	PAGED_CODE();

	FuncEntry(TRACE_DEVICE);

	pDevCtx = DeviceGetContext(Device);

	//
	// Threaded buffer queue used to serialize output report packets
	// 

	DMF_CONFIG_ThreadedBufferQueue_AND_ATTRIBUTES_INIT(
		&dmfBufferCfg,
		&moduleAttributes
	);
	moduleAttributes.PassiveLevel = TRUE;

	dmfBufferCfg.EvtThreadedBufferQueueWork = DMF_EvtExecuteOutputPacketReceived;
	// Fixed amount of buffers, no auto-grow
	dmfBufferCfg.BufferQueueConfig.SourceSettings.EnableLookAside = FALSE;
	/*
	 * TODO: tune to find good value
	 * - too low: packets might get dropped unintentionally
	 * - too high: user-noticeable delay may build up
	 */
	dmfBufferCfg.BufferQueueConfig.SourceSettings.BufferCount = 10;
	dmfBufferCfg.BufferQueueConfig.SourceSettings.BufferSize = DS3_BTH_HID_OUTPUT_REPORT_SIZE;
	dmfBufferCfg.BufferQueueConfig.SourceSettings.BufferContextSize = sizeof(DS_OUTPUT_REPORT_CONTEXT);
	dmfBufferCfg.BufferQueueConfig.SourceSettings.PoolType = NonPagedPoolNx;

	DMF_DmfModuleAdd(
		DmfModuleInit,
		&moduleAttributes,
		WDF_NO_OBJECT_ATTRIBUTES,
		&pDevCtx->OutputReport.Worker
	);

	//
	// Avoid allocating modules not used on USB
	// 
	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		//
		// Default I/O target request streamer for input reports
		// 

		DMF_CONFIG_DefaultTarget_AND_ATTRIBUTES_INIT(
			&bthReaderCfg,
			&moduleAttributes
		);
		moduleAttributes.PassiveLevel = TRUE;

		bthReaderCfg.ContinuousRequestTargetModuleConfig.BufferCountOutput = 1;
		bthReaderCfg.ContinuousRequestTargetModuleConfig.BufferOutputSize = BTHPS3_SIXAXIS_HID_INPUT_REPORT_SIZE;
		bthReaderCfg.ContinuousRequestTargetModuleConfig.ContinuousRequestCount = 1;
		bthReaderCfg.ContinuousRequestTargetModuleConfig.PoolTypeOutput = NonPagedPoolNx;
		bthReaderCfg.ContinuousRequestTargetModuleConfig.PurgeAndStartTargetInD0Callbacks = FALSE;
		bthReaderCfg.ContinuousRequestTargetModuleConfig.ContinuousRequestTargetIoctl = IOCTL_BTHPS3_HID_INTERRUPT_READ;
		bthReaderCfg.ContinuousRequestTargetModuleConfig.EvtContinuousRequestTargetBufferOutput = DsBth_HidInterruptReadContinuousRequestCompleted;
		bthReaderCfg.ContinuousRequestTargetModuleConfig.RequestType = ContinuousRequestTarget_RequestType_Ioctl;
		bthReaderCfg.ContinuousRequestTargetModuleConfig.ContinuousRequestTargetMode = ContinuousRequestTarget_Mode_Manual;

		DMF_DmfModuleAdd(
			DmfModuleInit,
			&moduleAttributes,
			WDF_NO_OBJECT_ATTRIBUTES,
			&pDevCtx->Connection.Bth.HidInterrupt.InputStreamerModule
		);


		DMF_CONFIG_DefaultTarget_AND_ATTRIBUTES_INIT(
			&bthWriterCfg,
			&moduleAttributes
		);
		moduleAttributes.PassiveLevel = TRUE;

		bthWriterCfg.ContinuousRequestTargetModuleConfig.BufferCountInput = 1;
		bthWriterCfg.ContinuousRequestTargetModuleConfig.BufferInputSize = BTHPS3_SIXAXIS_HID_OUTPUT_REPORT_SIZE;
		bthWriterCfg.ContinuousRequestTargetModuleConfig.ContinuousRequestCount = 1;
		bthWriterCfg.ContinuousRequestTargetModuleConfig.PoolTypeInput = NonPagedPoolNx;
		bthWriterCfg.ContinuousRequestTargetModuleConfig.PurgeAndStartTargetInD0Callbacks = FALSE;
		bthWriterCfg.ContinuousRequestTargetModuleConfig.ContinuousRequestTargetIoctl = IOCTL_BTHPS3_HID_CONTROL_WRITE;
		bthWriterCfg.ContinuousRequestTargetModuleConfig.EvtContinuousRequestTargetBufferInput = DsBth_HidControlWriteContinuousRequestCompleted;
		bthWriterCfg.ContinuousRequestTargetModuleConfig.RequestType = ContinuousRequestTarget_RequestType_Ioctl;
		bthWriterCfg.ContinuousRequestTargetModuleConfig.ContinuousRequestTargetMode = ContinuousRequestTarget_Mode_Manual;

		DMF_DmfModuleAdd(
			DmfModuleInit,
			&moduleAttributes,
			WDF_NO_OBJECT_ATTRIBUTES,
			&pDevCtx->Connection.Bth.HidControl.OutputWriterModule
		);
	}

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

#pragma region I/O Queue Callbacks

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

#pragma endregion
