#include "Driver.h"
#include "Device.tmh"
#include <DmfModule.h>
#include <devpkey.h>
#include <sddl.h>
#include <cfgmgr32.h>


EVT_DMF_DEVICE_MODULES_ADD DmfDeviceModulesAdd;

EVT_WDF_DEVICE_CONTEXT_CLEANUP DsHidMini_DeviceCleanup;

#pragma code_seg("PAGED")

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
	pnpPowerCallbacks.EvtDeviceReleaseHardware = DsHidMini_EvtDeviceReleaseHardware;
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
	deviceAttributes.EvtCleanupCallback = DsHidMini_DeviceCleanup;

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

		const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(device);

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
void DsHidMini_DeviceCleanup(
	WDFOBJECT Object
)
{
	FuncEntry(TRACE_DEVICE);

	PAGED_CODE();

	const WDFDEVICE device = Object;
	const PDEVICE_CONTEXT deviceContext = DeviceGetContext(device);
	const WDFDRIVER driver = WdfGetDriver();
	const PDSHM_DRIVER_CONTEXT driverContext = DriverGetContext(driver);

	WdfWaitLockAcquire(driverContext->SlotsLock, NULL);
	{
		CLEAR_SLOT(driverContext, deviceContext->SlotIndex);
		if (driverContext->IPC.IsEnabled)
		{
			driverContext->IPC.DeviceDispatchers.Callbacks[deviceContext->SlotIndex] = NULL;
			driverContext->IPC.DeviceDispatchers.Contexts[deviceContext->SlotIndex] = NULL;

			const size_t offset = (sizeof(IPC_HID_INPUT_REPORT_MESSAGE) * (deviceContext->SlotIndex - 1));
			const PIPC_HID_INPUT_REPORT_MESSAGE pHIDBuffer = (PIPC_HID_INPUT_REPORT_MESSAGE)(
				driverContext->IPC.SharedRegions.HID.Buffer + offset);

			//
			// Seqlock: odd generation while the payload is cleared, then the
			// next even generation. SequenceNumber is left intact so a later
			// occupant of this slot continues the counter.
			// 
			InterlockedIncrement(&pHIDBuffer->SequenceNumber);
			pHIDBuffer->SlotIndex = 0;
			RtlZeroMemory(&pHIDBuffer->InputReport, sizeof(DS3_RAW_INPUT_REPORT));
			RtlZeroMemory(pHIDBuffer->AlignmentPadding, sizeof(pHIDBuffer->AlignmentPadding));
			InterlockedIncrement(&pHIDBuffer->SequenceNumber);

			if (deviceContext->IPC.InputReportWaitHandle != NULL)
			{
				SetEvent(deviceContext->IPC.InputReportWaitHandle);
				CloseHandle(deviceContext->IPC.InputReportWaitHandle);
				deviceContext->IPC.InputReportWaitHandle = NULL;
			}
		}
	}
	WdfWaitLockRelease(driverContext->SlotsLock);

	if (deviceContext->ConnectionType == DsDeviceConnectionTypeBth)
	{
		if (deviceContext->Connection.Bth.DisconnectWaitHandle)
		{
			UnregisterWaitEx(deviceContext->Connection.Bth.DisconnectWaitHandle, INVALID_HANDLE_VALUE);
			deviceContext->Connection.Bth.DisconnectWaitHandle = NULL;
		}

		if (deviceContext->Connection.Bth.DisconnectEvent)
		{
			CloseHandle(deviceContext->Connection.Bth.DisconnectEvent);
			deviceContext->Connection.Bth.DisconnectEvent = NULL;
		}
	}

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
			if (!NT_SUCCESS(status = WdfDeviceAllocAndQueryPropertyEx(
				Device,
				&devProp,
				NonPagedPoolNx,
				WDF_NO_OBJECT_ATTRIBUTES,
				&deviceAddressMemory,
				&propType
			)))
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
			const UINT64 hostAddress = wcstoull(
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
			
			if (!NT_SUCCESS(status = WdfDeviceQueryPropertyEx(
				Device,
				&devProp,
				sizeof(USHORT),
				&pDevCtx->VendorId,
				&requiredSize,
				&propType
			)))
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

			if (!NT_SUCCESS(status = WdfDeviceQueryPropertyEx(
				Device,
				&devProp,
				sizeof(USHORT),
				&pDevCtx->ProductId,
				&requiredSize,
				&propType
			)))
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

			DsDevice_AssignDeviceType(Device);
		}
	} while (FALSE);

	FuncExit(TRACE_DEVICE, "status=%!STATUS!", status);

	return status;
}

//
// Classify the hardware family from VID/PID and publish it as a read-only
// property so ControlApp does not infer capabilities from display names.
// Sony Navigation is VID_054C / PID_042F (issue #48).
// 
VOID
DsDevice_AssignDeviceType(
	WDFDEVICE Device
)
{
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	WDF_DEVICE_PROPERTY_DATA propertyData;
	UCHAR deviceType;

	if (pDevCtx->VendorId == DS_SONY_VENDOR_ID &&
		pDevCtx->ProductId == DS_SONY_PID_NAVIGATION)
	{
		pDevCtx->DeviceType = DsDeviceTypeNavigation;
	}
	else if (pDevCtx->VendorId == DS_SONY_VENDOR_ID &&
		pDevCtx->ProductId == DS_SONY_PID_SIXAXIS)
	{
		pDevCtx->DeviceType = DsDeviceTypeSixaxis;
	}
	else
	{
		pDevCtx->DeviceType = DsDeviceTypeUnknown;
	}

	deviceType = (UCHAR)pDevCtx->DeviceType;

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_DeviceType);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	if (!NT_SUCCESS(WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_BYTE,
		sizeof(deviceType),
		&deviceType
	)))
	{
		TraceError(
			TRACE_DEVICE,
			"Setting DEVPKEY_DsHidMini_RO_DeviceType failed"
		);
	}

	TraceVerbose(
		TRACE_DEVICE,
		"DeviceType=%u (VID=0x%04X PID=0x%04X)",
		pDevCtx->DeviceType,
		pDevCtx->VendorId,
		pDevCtx->ProductId
	);
}

//
// Deferred callback that requests a self re-enumeration when
// DMF_DsHidMini_Open detected that the HID mode loaded from configuration
// differs from the mode already exposed via
// DEVPKEY_DsHidMini_RW_HidDeviceMode (see issue #374).
//
// Running this from a timer rather than inline in DMF_DsHidMini_Open keeps
// WdfDeviceSetFailed off the power-up call stack, mirroring the approach
// already used for the failed-resume restart in DsUsb.c (issue #311).
// 
_Use_decl_annotations_
VOID
DsDevice_EvtHidModeRestartTimerFunc(
	WDFTIMER Timer
)
{
	FuncEntry(TRACE_DEVICE);

	const WDFDEVICE device = WdfTimerGetParentObject(Timer);
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(device);

	TraceWarning(
		TRACE_DEVICE,
		"Requesting a device restart, HID mode from configuration mismatched the mode already probed by PnP"
	);

	EventWriteRequestingDeviceRestartOnHidModeMismatch(pDevCtx->DeviceAddressString);

	WdfDeviceSetFailed(device, WdfDeviceFailedAttemptRestart);

	FuncExitNoReturn(TRACE_DEVICE);
}

//
// Initialize remaining device context fields
// 
NTSTATUS
DsDevice_InitContext(
	WDFDEVICE Device
)
{
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	const PDSHM_DRIVER_CONTEXT pDrvCtx = DriverGetContext(WdfGetDriver());
	NTSTATUS status = STATUS_INSUFFICIENT_RESOURCES;
	WDF_OBJECT_ATTRIBUTES attributes;
	PUCHAR outReportBuffer = NULL;
	WDF_TIMER_CONFIG timerCfg;

	FuncEntry(TRACE_DEVICE);

	WdfWaitLockAcquire(pDrvCtx->SlotsLock, NULL);
	{
		//
		// Get next free slot
		// 
		for (UINT32 slotIndex = 1; slotIndex <= DSHM_MAX_DEVICES; slotIndex++)
		{
			if (!TEST_SLOT(pDrvCtx, slotIndex))
			{
				SET_SLOT(pDrvCtx, slotIndex);
				status = STATUS_SUCCESS;

				TraceVerbose(
					TRACE_DEVICE,
					"Claimed device slot: %d",
					slotIndex
				);

				pDevCtx->SlotIndex = slotIndex;
				if (pDrvCtx->IPC.IsEnabled)
				{
					pDrvCtx->IPC.DeviceDispatchers.Callbacks[slotIndex] = DSHM_EvtDispatchDeviceMessage;
					pDrvCtx->IPC.DeviceDispatchers.Contexts[slotIndex] = pDevCtx;
				}
				break;
			}
		}
	}
	WdfWaitLockRelease(pDrvCtx->SlotsLock);

	if (!NT_SUCCESS(status))
	{
		FuncExit(TRACE_DEVICE, "status=%!STATUS!", status);

		return status;
	}

	{
		WDF_DEVICE_PROPERTY_DATA propertyData;
		NTSTATUS propStatus;

		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_IpcSlotIndex);
		propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
		propertyData.Lcid = LOCALE_NEUTRAL;

		propStatus = WdfDeviceAssignProperty(
			Device,
			&propertyData,
			DEVPROP_TYPE_UINT32,
			sizeof(UINT32),
			&pDevCtx->SlotIndex
		);

		if (!NT_SUCCESS(propStatus))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfDeviceAssignProperty(DEVPKEY_DsHidMini_RO_IpcSlotIndex) failed with %!STATUS! (SlotIndex=%u)",
				propStatus,
				pDevCtx->SlotIndex
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfDeviceAssignProperty(IpcSlotIndex)", propStatus);

			if (pDrvCtx->IPC.IsEnabled)
			{
				FuncExit(TRACE_DEVICE, "status=%!STATUS!", propStatus);

				return propStatus;
			}
		}
	}

	// ReSharper disable once CppIncompleteSwitchStatement
	// ReSharper disable once CppDefaultCaseNotHandledInSwitchStatement
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

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		WDF_TIMER_CONFIG_INIT(
			&timerCfg,
			DsDevice_EvtBthDisconnectRetryTimerFunc
		);

		if (!NT_SUCCESS(status = WdfTimerCreate(
			&timerCfg,
			&attributes,
			&pDevCtx->Connection.Usb.DisconnectRetryTimer
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfTimerCreate (DisconnectRetryTimer) failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfTimerCreate (DisconnectRetryTimer)", status);
			break;
		}

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
		// Turn flashing LEDs off. Uses the connection-agnostic setter
		// (ConnectionType is already assigned above) instead of the
		// Bluetooth-specific macro, so this matches the USB path and
		// DsLed's own primitives.
		// 
		DsLed_SetFlags(pDevCtx, DS3_LED_OFF);

#pragma region StartupDelay

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		WDF_TIMER_CONFIG_INIT(
			&timerCfg,
			DsBth_EvtStartupDelayTimerFunc
		);

		if (!NT_SUCCESS(status = WdfTimerCreate(
			&timerCfg,
			&attributes,
			&pDevCtx->Connection.Bth.Timers.StartupDelay
		)))
		{
			TraceError(
				TRACE_DSBTH,
				"WdfTimerCreate (StartupDelay) failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfTimerCreate (StartupDelay)", status);
			break;
		}

#pragma endregion

#pragma region PostStartupTasks

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		WDF_TIMER_CONFIG_INIT(
			&timerCfg,
			DsBth_EvtPostStartupTimerFunc
		);

		if (!NT_SUCCESS(status = WdfTimerCreate(
			&timerCfg,
			&attributes,
			&pDevCtx->Connection.Bth.Timers.PostStartupTasks
		)))
		{
			TraceError(
				TRACE_DSBTH,
				"WdfTimerCreate (PostStartupTasks) failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfTimerCreate (PostStartupTasks)", status);
			break;
		}

#pragma endregion

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

		//
		// Create timer used to defer a self re-enumeration request when a
		// HID mode mismatch is detected (see issue #374)
		// 

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		WDF_TIMER_CONFIG_INIT(
			&timerCfg,
			DsDevice_EvtHidModeRestartTimerFunc
		);

		if (!NT_SUCCESS(status = WdfTimerCreate(
			&timerCfg,
			&attributes,
			&pDevCtx->HidModeRestartTimer
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfTimerCreate (HidModeRestartTimer) failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfTimerCreate (HidModeRestartTimer)", status);
			break;
		}

		//
		// Create periodic timer used to keep active rumble alive against
		// the finite motor duration now written by
		// DS3_PROCESS_RUMBLE_STRENGTH (see issue #356). This is the
		// driver's only periodic timer; every other one here is one-shot.
		// 

		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = Device;

		WDF_TIMER_CONFIG_INIT_PERIODIC(
			&timerCfg,
			DS3_EvtRumbleKeepAliveTimerFunc,
			DS3_RUMBLE_KEEPALIVE_PERIOD_MS
		);

		if (!NT_SUCCESS(status = WdfTimerCreate(
			&timerCfg,
			&attributes,
			&pDevCtx->RumbleControlState.RumbleKeepAliveTimer
		)))
		{
			TraceError(
				TRACE_DEVICE,
				"WdfTimerCreate (RumbleKeepAliveTimer) failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfTimerCreate (RumbleKeepAliveTimer)", status);
			break;
		}

#pragma region IPC

		SECURITY_DESCRIPTOR sd = { 0 };

		if (!InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION))
		{
			TraceError(
				TRACE_IPC,
				"InitializeSecurityDescriptor failed with error: %!WINERROR!",
				GetLastError()
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"InitializeSecurityDescriptor", GetLastError());
			break;
		}

		SECURITY_ATTRIBUTES sa = { 0 };
		sa.nLength = sizeof(sa);
		sa.bInheritHandle = TRUE;
		sa.lpSecurityDescriptor = &sd;

		CHAR* szSD = "D:" // Discretionary ACL
		"(D;OICI;GA;;;BG)" // Deny access to Built-in Guests
		"(D;OICI;GA;;;AN)" // Deny access to Anonymous Logon
		"(A;OICI;GRGWGX;;;AU)" // Allow read/write/execute to Authenticated Users
		"(A;OICI;GA;;;BA)"; // Allow full control to Administrators

		if (!ConvertStringSecurityDescriptorToSecurityDescriptorA(
			szSD,
			SDDL_REVISION_1,
			&sa.lpSecurityDescriptor,
			NULL
		))
		{
			TraceError(
				TRACE_IPC,
				"ConvertStringSecurityDescriptorToSecurityDescriptor failed with error: %!WINERROR!",
				GetLastError()
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"ConvertStringSecurityDescriptorToSecurityDescriptor", GetLastError());
			break;
		}

		CHAR hidEventName[DSHM_IPC_HID_REPORT_EVENT_NAME_CCH];

		if (sprintf_s(
			hidEventName,
			ARRAYSIZE(hidEventName),
			"%s%u",
			DSHM_IPC_HID_REPORT_EVENT_PREFIX,
			pDevCtx->SlotIndex
		) < 0)
		{
			TraceError(
				TRACE_IPC,
				"sprintf_s failed formatting HID report wait event name"
			);
			LocalFree(sa.lpSecurityDescriptor);
			sa.lpSecurityDescriptor = NULL;
			status = STATUS_INVALID_PARAMETER;
			break;
		}

		pDevCtx->IPC.InputReportWaitHandle = CreateEventA(&sa, TRUE, FALSE, hidEventName);

		LocalFree(sa.lpSecurityDescriptor);
		sa.lpSecurityDescriptor = NULL;

		if (pDevCtx->IPC.InputReportWaitHandle == NULL)
		{
			TraceError(
				TRACE_IPC,
				"CreateEventA failed with error: %!WINERROR!",
				GetLastError()
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"CreateEventA", GetLastError());
			break;
		}

#pragma endregion

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

		const NTSTATUS reloadStatus = ConfigLoadForDevice(pDevCtx, TRUE);
		if (!NT_SUCCESS(reloadStatus))
		{
			TraceWarning(
				TRACE_DEVICE,
				"Configuration hot-reload failed with status %!STATUS!, keeping the previous configuration",
				reloadStatus
			);
			WdfWaitLockRelease(pDevCtx->ConfigurationDirectoryWatcherLock);
			break;
		}

		TraceVerbose(
			TRACE_DEVICE,
			"Reloaded configuration"
		);

		WdfWaitLockRelease(pDevCtx->ConfigurationDirectoryWatcherLock);

		//
		// Restore the Automatic authority hand-off for this reload before
		// recomputing LED state: without this, OutputReport.Mode would stay
		// latched at whatever an application last wrote (or its power-up
		// default), and DsLed_IsDriverInCharge would never allow the new
		// LED settings below to actually apply under Automatic authority
		// (issue #349/#351).
		// 
		pDevCtx->OutputReport.Mode = Ds3OutputReportModeDriverHandled;

		//
		// Changes to LED settings need to be pushed to the device. Unlike
		// the previous plain DSHM_SendOutputReport call, DsLed_Refresh
		// recomputes flags/effects from the newly loaded configuration and
		// current battery status first, so a hot-reload into e.g. a
		// different LED mode actually takes effect immediately instead of
		// re-sending the stale pattern (issue #349).
		// 
		(void)DsLed_Refresh(pDevCtx, Ds3OutputReportSourceDriverHighPriority);

	} while (FALSE);

	FuncExitNoReturn(TRACE_DEVICE);
}

//
// Registers an event listener to trigger refreshing runtime properties
// 
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
			ARRAYSIZE(configPath),
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

//
// Formats the controller MAC in display order for both transports. Bluetooth
// stores DeviceAddress LSB-first; USB stores the feature-report (MSB-first)
// layout. The resulting 12-hex-digit string is what both sides of the
// USB/wireless handshake must use as the named-event suffix.
// 
void
DsDevice_FormatCanonicalAddress(
	_In_ PDEVICE_CONTEXT Context,
	_Out_writes_(BufferChars) PWCHAR Buffer,
	_In_ size_t BufferChars
)
{
	if (BufferChars < DSHM_DEVICE_ADDRESS_CCH)
	{
		if (BufferChars > 0)
		{
			Buffer[0] = L'\0';
		}
		return;
	}

	if (Context->ConnectionType == DsDeviceConnectionTypeBth)
	{
		swprintf_s(
			Buffer,
			BufferChars,
			L"%02X%02X%02X%02X%02X%02X",
			Context->DeviceAddress.Address[5],
			Context->DeviceAddress.Address[4],
			Context->DeviceAddress.Address[3],
			Context->DeviceAddress.Address[2],
			Context->DeviceAddress.Address[1],
			Context->DeviceAddress.Address[0]
		);
	}
	else
	{
		swprintf_s(
			Buffer,
			BufferChars,
			L"%02X%02X%02X%02X%02X%02X",
			Context->DeviceAddress.Address[0],
			Context->DeviceAddress.Address[1],
			Context->DeviceAddress.Address[2],
			Context->DeviceAddress.Address[3],
			Context->DeviceAddress.Address[4],
			Context->DeviceAddress.Address[5]
		);
	}
}

static BOOLEAN
DsDevice_BuildNamedEventName(
	_In_ PDEVICE_CONTEXT Context,
	_In_ PCWSTR Format,
	_Out_writes_(EventNameChars) PWCHAR EventName,
	_In_ size_t EventNameChars
)
{
	WCHAR deviceAddress[DSHM_DEVICE_ADDRESS_CCH];

	DsDevice_FormatCanonicalAddress(Context, deviceAddress, ARRAYSIZE(deviceAddress));

	return swprintf_s(EventName, EventNameChars, Format, deviceAddress) > 0;
}

static BOOLEAN
DsDevice_CreateHostSecurityAttributes(
	_Out_ PSECURITY_ATTRIBUTES SecurityAttributes
)
{
	SecurityAttributes->nLength = sizeof(*SecurityAttributes);
	SecurityAttributes->bInheritHandle = FALSE;
	SecurityAttributes->lpSecurityDescriptor = NULL;

	if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
		DSHM_HOST_NAMED_OBJECT_SDDL,
		SDDL_REVISION_1,
		&SecurityAttributes->lpSecurityDescriptor,
		NULL
	))
	{
		EventWriteFailedWithWin32Error(__FUNCTION__, L"ConvertStringSecurityDescriptorToSecurityDescriptor", GetLastError());
		return FALSE;
	}

	return TRUE;
}

static HANDLE
DsDevice_CreateSharedNamedEvent(
	_In_ PCWSTR EventName,
	_In_ BOOLEAN ManualReset,
	_In_ BOOLEAN InitialState,
	_Out_opt_ PDWORD CreateError
)
{
	SECURITY_ATTRIBUTES sa;
	HANDLE event;

	if (CreateError)
	{
		*CreateError = 0;
	}

	if (!DsDevice_CreateHostSecurityAttributes(&sa))
	{
		return NULL;
	}

	event = CreateEventW(
		&sa,
		ManualReset,
		InitialState,
		EventName
	);

	const DWORD createError = GetLastError();

	LocalFree(sa.lpSecurityDescriptor);

	if (CreateError)
	{
		*CreateError = createError;
	}

	if (event == NULL)
	{
		SetLastError(createError);
		return NULL;
	}

	return event;
}

static HANDLE
DsDevice_AcquireDisconnectHandshake(
	_In_ PDEVICE_CONTEXT Context
)
{
	WCHAR mutexName[DSHM_NAMED_EVENT_NAME_CCH];
	SECURITY_ATTRIBUTES sa;
	HANDLE mutex;
	DWORD waitResult;

	if (!DsDevice_BuildNamedEventName(
		Context,
		DSHM_NAMED_MUTEX_DISCONNECT,
		mutexName,
		ARRAYSIZE(mutexName)
	))
	{
		return NULL;
	}

	if (!DsDevice_CreateHostSecurityAttributes(&sa))
	{
		return NULL;
	}

	mutex = CreateMutexW(&sa, FALSE, mutexName);
	LocalFree(sa.lpSecurityDescriptor);

	if (mutex == NULL)
	{
		EventWriteFailedWithWin32Error(__FUNCTION__, L"CreateMutexW", GetLastError());
		return NULL;
	}

	waitResult = WaitForSingleObject(mutex, DSHM_BTH_DISCONNECT_LOCK_TIMEOUT_MS);
	if (waitResult != WAIT_OBJECT_0 && waitResult != WAIT_ABANDONED)
	{
		CloseHandle(mutex);
		return NULL;
	}

	return mutex;
}

static void
DsDevice_ReleaseDisconnectHandshake(
	_In_opt_ HANDLE Mutex
)
{
	if (Mutex)
	{
		ReleaseMutex(Mutex);
		CloseHandle(Mutex);
	}
}

static BOOLEAN
DsDevice_TrySignalBthDisconnect(
	_In_ PDEVICE_CONTEXT Context
)
{
	WCHAR dcEventName[DSHM_NAMED_EVENT_NAME_CCH];
	BOOLEAN signaled = FALSE;

	if (!DsDevice_BuildNamedEventName(
		Context,
		DSHM_NAMED_EVENT_DISCONNECT,
		dcEventName,
		ARRAYSIZE(dcEventName)
	))
	{
		return FALSE;
	}

	const HANDLE handshake = DsDevice_AcquireDisconnectHandshake(Context);

	const HANDLE dcEvent = OpenEventW(
		SYNCHRONIZE | EVENT_MODIFY_STATE,
		FALSE,
		dcEventName
	);

	if (dcEvent != NULL)
	{
		TraceInformation(
			TRACE_DSUSB,
			"Found existing event %ls, signalling disconnect",
			dcEventName
		);

		EventWriteWirelessDisconnectSignaled(Context->DeviceAddressString);

		SetEvent(dcEvent);
		CloseHandle(dcEvent);
		signaled = TRUE;
	}
	else
	{
		const DWORD error = GetLastError();

		if (error != ERROR_NOT_FOUND && error != ERROR_FILE_NOT_FOUND)
		{
			TraceError(
				TRACE_DSUSB,
				"OpenEventW failed with %!WINERROR!",
				error
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"OpenEventW", error);
		}
	}

	DsDevice_ReleaseDisconnectHandshake(handshake);

	return signaled;
}

//
// Register event to disconnect from Bluetooth, bypassing mshidumdf.sys
// 
void DsDevice_RegisterBthDisconnectListener(PDEVICE_CONTEXT Context)
{
	WCHAR dcEventName[DSHM_NAMED_EVENT_NAME_CCH];

	FuncEntry(TRACE_DEVICE);

	if (!DsDevice_BuildNamedEventName(
		Context,
		DSHM_NAMED_EVENT_DISCONNECT,
		dcEventName,
		ARRAYSIZE(dcEventName)
	))
	{
		TraceError(
			TRACE_DEVICE,
			"Failed to build disconnect event name"
		);
		FuncExitNoReturn(TRACE_DEVICE);
		return;
	}

	TraceInformation(
		TRACE_DEVICE,
		"Disconnect event name: %ls",
		dcEventName
	);

	if (Context->Connection.Bth.DisconnectWaitHandle)
	{
		UnregisterWaitEx(Context->Connection.Bth.DisconnectWaitHandle, INVALID_HANDLE_VALUE);
		Context->Connection.Bth.DisconnectWaitHandle = NULL;
	}

	if (Context->Connection.Bth.DisconnectEvent)
	{
		CloseHandle(Context->Connection.Bth.DisconnectEvent);
		Context->Connection.Bth.DisconnectEvent = NULL;
	}

	const HANDLE handshake = DsDevice_AcquireDisconnectHandshake(Context);
	DWORD createError = 0;

	Context->Connection.Bth.DisconnectEvent = DsDevice_CreateSharedNamedEvent(
		dcEventName,
		FALSE,
		FALSE,
		&createError
	);

	if (Context->Connection.Bth.DisconnectEvent == NULL)
	{
		DsDevice_ReleaseDisconnectHandshake(handshake);
		TraceError(
			TRACE_DEVICE,
			"Failed to create disconnect event"
		);
		EventWriteFailedWithWin32Error(__FUNCTION__, L"CreateEventW", createError);
		FuncExitNoReturn(TRACE_DEVICE);
		return;
	}

	//
	// CreateEventW ignores bInitialState when the named object already
	// exists. Reset under the same lock as SetEvent so a concurrent USB
	// signal cannot be cleared before the waiter is registered.
	// 
	if (createError == ERROR_ALREADY_EXISTS)
	{
		ResetEvent(Context->Connection.Bth.DisconnectEvent);
	}

	const BOOL ret = RegisterWaitForSingleObject(
		&Context->Connection.Bth.DisconnectWaitHandle,
		Context->Connection.Bth.DisconnectEvent,
		DsBth_DisconnectEventCallback,
		Context,
		INFINITE,
		WT_EXECUTELONGFUNCTION
	);

	DsDevice_ReleaseDisconnectHandshake(handshake);

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

//
// Signals existing wireless connection with same device address to terminate. The controller
// does not disconnect from Bluetooth on its own once connected to USB, so we signal the
// wireless device object to disconnect itself before continuing with USB initialization.
// 
void DsDevice_InvokeLocalBthDisconnect(PDEVICE_CONTEXT Context)
{
	if (DsDevice_TrySignalBthDisconnect(Context))
	{
		Context->Connection.Usb.DisconnectRetryRemaining = 0;
		return;
	}

	if (Context->Connection.Usb.DisconnectRetryTimer == NULL)
	{
		EventWriteWirelessDisconnectEventNotFound(Context->DeviceAddressString);
		return;
	}

	Context->Connection.Usb.DisconnectRetryRemaining = DSHM_BTH_DISCONNECT_RETRY_COUNT;

	WdfTimerStart(
		Context->Connection.Usb.DisconnectRetryTimer,
		WDF_REL_TIMEOUT_IN_MS(DSHM_BTH_DISCONNECT_RETRY_DELAY_MS)
	);
}

_Use_decl_annotations_
VOID
DsDevice_EvtBthDisconnectRetryTimerFunc(
	WDFTIMER Timer
)
{
	FuncEntry(TRACE_DEVICE);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(WdfTimerGetParentObject(Timer));

	if (DsDevice_TrySignalBthDisconnect(pDevCtx))
	{
		pDevCtx->Connection.Usb.DisconnectRetryRemaining = 0;
		FuncExitNoReturn(TRACE_DEVICE);
		return;
	}

	if (pDevCtx->Connection.Usb.DisconnectRetryRemaining > 0)
	{
		pDevCtx->Connection.Usb.DisconnectRetryRemaining--;

		if (pDevCtx->Connection.Usb.DisconnectRetryRemaining > 0)
		{
			WdfTimerStart(
				pDevCtx->Connection.Usb.DisconnectRetryTimer,
				WDF_REL_TIMEOUT_IN_MS(DSHM_BTH_DISCONNECT_RETRY_DELAY_MS)
			);

			FuncExitNoReturn(TRACE_DEVICE);
			return;
		}
	}

	TraceInformation(
		TRACE_DSUSB,
		"No wireless instance found to disconnect for %s",
		pDevCtx->DeviceAddressString
	);
	EventWriteWirelessDisconnectEventNotFound(pDevCtx->DeviceAddressString);

	FuncExitNoReturn(TRACE_DEVICE);
}

BOOLEAN
DsDevice_IsWiredInstancePresent(
	_In_ PDEVICE_CONTEXT Context
)
{
	WCHAR expectedAddress[DSHM_DEVICE_ADDRESS_CCH];
	ULONG listChars = 0;
	PWSTR list = NULL;
	BOOLEAN present = FALSE;

	DsDevice_FormatCanonicalAddress(Context, expectedAddress, ARRAYSIZE(expectedAddress));
	if (expectedAddress[0] == L'\0')
	{
		return FALSE;
	}

	if (CM_Get_Device_Interface_List_SizeW(
		&listChars,
		(LPGUID)&GUID_DEVINTERFACE_DSHIDMINI,
		NULL,
		CM_GET_DEVICE_INTERFACE_LIST_PRESENT) != CR_SUCCESS
		|| listChars <= 1)
	{
		return FALSE;
	}

	list = (PWSTR)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, listChars * sizeof(WCHAR));
	if (list == NULL)
	{
		return FALSE;
	}

	if (CM_Get_Device_Interface_ListW(
		(LPGUID)&GUID_DEVINTERFACE_DSHIDMINI,
		NULL,
		list,
		listChars,
		CM_GET_DEVICE_INTERFACE_LIST_PRESENT) != CR_SUCCESS)
	{
		HeapFree(GetProcessHeap(), 0, list);
		return FALSE;
	}

	for (PWSTR iface = list; *iface; iface += wcslen(iface) + 1)
	{
		DEVPROPTYPE propType;
		WCHAR instanceId[MAX_DEVICE_ID_LEN];
		WCHAR enumerator[32];
		WCHAR address[DSHM_DEVICE_ADDRESS_CCH];
		ULONG size;
		DEVINST devInst;

		size = sizeof(instanceId);
		if (CM_Get_Device_Interface_PropertyW(
			iface,
			&DEVPKEY_Device_InstanceId,
			&propType,
			(PBYTE)instanceId,
			&size,
			0) != CR_SUCCESS)
		{
			continue;
		}

		if (CM_Locate_DevNodeW(&devInst, instanceId, CM_LOCATE_DEVNODE_NORMAL) != CR_SUCCESS)
		{
			continue;
		}

		size = sizeof(enumerator);
		if (CM_Get_DevNode_PropertyW(
			devInst,
			&DEVPKEY_Device_EnumeratorName,
			&propType,
			(PBYTE)enumerator,
			&size,
			0) != CR_SUCCESS
			|| propType != DEVPROP_TYPE_STRING
			|| _wcsicmp(enumerator, L"USB") != 0)
		{
			continue;
		}

		size = sizeof(address);
		if (CM_Get_DevNode_PropertyW(
			devInst,
			&DEVPKEY_Bluetooth_DeviceAddress,
			&propType,
			(PBYTE)address,
			&size,
			0) != CR_SUCCESS
			|| propType != DEVPROP_TYPE_STRING)
		{
			continue;
		}

		if (_wcsicmp(address, expectedAddress) == 0)
		{
			present = TRUE;
			break;
		}
	}

	HeapFree(GetProcessHeap(), 0, list);
	return present;
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
	DMF_MODULE_ATTRIBUTES moduleAttributes;
	DMF_CONFIG_ThreadedBufferQueue dmfBufferCfg;
	DMF_CONFIG_DefaultTarget bthReaderCfg;
	DMF_CONFIG_DefaultTarget bthWriterCfg;

	PAGED_CODE();

	FuncEntry(TRACE_DEVICE);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	//
	// Threaded buffer queue used to serialize output report packets
	// 

	DMF_CONFIG_ThreadedBufferQueue_AND_ATTRIBUTES_INIT(
		&dmfBufferCfg,
		&moduleAttributes
	);
	moduleAttributes.PassiveLevel = TRUE;

	dmfBufferCfg.EvtThreadedBufferQueueWork = DSHM_EvtExecuteOutputPacketReceived;
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

	DMF_DsHidMini_ATTRIBUTES_INIT(&moduleAttributes);

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
