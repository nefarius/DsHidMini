#include "Driver.h"
#include "Power.tmh"



//
// Start Bluetooth communication here
// 
_Use_decl_annotations_
NTSTATUS
DsHidMini_EvtWdfDeviceSelfManagedIoInit(
	WDFDEVICE  Device
)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx;
	WDF_DEVICE_PROPERTY_DATA propertyData;
	WCHAR deviceAddress[13];
	UINT64 hostAddress = 0;
	size_t friendlyNameSize = 0;
	PWSTR friendlyName;
	WCHAR eventName[49];
	WCHAR dcEventName[44];
	UCHAR identification[64];

	PAGED_CODE();

	FuncEntry(TRACE_POWER);


	pDevCtx = DeviceGetContext(Device);

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		//
		// See https://github.com/ViGEm/DsHidMini/issues/50
		// 
		if (NT_SUCCESS(USB_SendControlRequest(
			pDevCtx,
			BmRequestDeviceToHost,
			BmRequestClass,
			GetReport,
			0x0301,
			0,
			identification,
			ARRAYSIZE(identification)
		)))
		{
			WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_IdentificationData);
			propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
			propertyData.Lcid = LOCALE_NEUTRAL;

			(void)WdfDeviceAssignProperty(
				Device,
				&propertyData,
				DEVPROP_TYPE_BINARY,
				ARRAYSIZE(identification),
				identification
			);
		}
		
		swprintf_s(
			deviceAddress,
			ARRAYSIZE(deviceAddress),
			L"%02X%02X%02X%02X%02X%02X",
			pDevCtx->DeviceAddress.Address[0],
			pDevCtx->DeviceAddress.Address[1],
			pDevCtx->DeviceAddress.Address[2],
			pDevCtx->DeviceAddress.Address[3],
			pDevCtx->DeviceAddress.Address[4],
			pDevCtx->DeviceAddress.Address[5]
		);

		//
		// Set friendly name
		// 

		friendlyName = WdfMemoryGetBuffer(
			pDevCtx->Connection.Usb.ProductString,
			&friendlyNameSize
		);
		
		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_Device_FriendlyName);
		propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
		propertyData.Lcid = LOCALE_NEUTRAL;

		status = WdfDeviceAssignProperty(
			Device,
			&propertyData,
			DEVPROP_TYPE_STRING,
			(ULONG)friendlyNameSize + sizeof(L'\0'),
			friendlyName
		);

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_POWER,
				"Setting DEVPKEY_Device_FriendlyName failed with status %!STATUS!",
				status
			);
		}
		
		//
		// Set device address property
		// 
		
		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_Bluetooth_DeviceAddress);
		propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
		propertyData.Lcid = LOCALE_NEUTRAL;

		status = WdfDeviceAssignProperty(
			Device,
			&propertyData,
			DEVPROP_TYPE_STRING,
			ARRAYSIZE(deviceAddress) * sizeof(WCHAR),
			deviceAddress
		);

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_POWER,
				"Setting DEVPKEY_Bluetooth_DeviceAddress failed with status %!STATUS!",
				status
			);
		}		

		//
		// Attempt automatic pairing
		// 
		if (!pDevCtx->Configuration.DisableAutoPairing)
		{
			//
			// Auto-pair to first found radio
			// 
			status = DsUsb_Ds3PairToFirstRadio(pDevCtx);

			if (!NT_SUCCESS(status))
			{
				TraceError(TRACE_POWER,
					"DsUsb_Ds3PairToFirstRadio failed with status %!STATUS!",
					status);
			}
		}
		else
		{
			TraceInformation(
				TRACE_POWER,
				"Auto-pairing disabled in device configuration");
		}

		//
		// Set host radio address property
		// 

		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_BluetoothRadio_Address);
		propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
		propertyData.Lcid = LOCALE_NEUTRAL;

		hostAddress = (UINT64)(pDevCtx->HostAddress.Address[5]) |
			(UINT64)(pDevCtx->HostAddress.Address[4]) << 8 |
			(UINT64)(pDevCtx->HostAddress.Address[3]) << 16 |
			(UINT64)(pDevCtx->HostAddress.Address[2]) << 24 |
			(UINT64)(pDevCtx->HostAddress.Address[1]) << 32 |
			(UINT64)(pDevCtx->HostAddress.Address[0]) << 40;
		
		status = WdfDeviceAssignProperty(
			Device,
			&propertyData,
			DEVPROP_TYPE_UINT64,
			sizeof(UINT64),
			&hostAddress
		);

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_POWER,
				"Setting DEVPKEY_BluetoothRadio_Address failed with status %!STATUS!",
				status
			);
		}

		status = STATUS_SUCCESS;
	}
	
	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		swprintf_s(
			deviceAddress,
			ARRAYSIZE(deviceAddress),
			L"%012llX",
			*(PULONGLONG)&pDevCtx->DeviceAddress
		);

		//
		// Send magic packet, starts input report sending
		// 
		DsBth_Ds3Init(pDevCtx);

#pragma region HID Control Write

		//
		// Send initial output report
		// 
		status = Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverHighPriority);

		//
		// Send preset output report (delayed)
		// Required for compatibility with some SIXAXIS models
		// 
		WdfTimerStart(
			pDevCtx->Connection.Bth.Timers.HidOutputReport,
			WDF_REL_TIMEOUT_IN_SEC(1)
		);

#pragma endregion

#pragma region Disconnect event handler

		swprintf_s(
			dcEventName,
			ARRAYSIZE(dcEventName),
			L"Global\\DsHidMiniDisconnectEvent%ls",
			deviceAddress
		);

		TraceVerbose(
			TRACE_POWER,
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

		if (pDevCtx->Connection.Bth.DisconnectEvent) {
			CloseHandle(pDevCtx->Connection.Bth.DisconnectEvent);
			pDevCtx->Connection.Bth.DisconnectEvent = NULL;
		}

		pDevCtx->Connection.Bth.DisconnectEvent = CreateEvent(
			&sa,
			FALSE,
			FALSE,
			dcEventName
		);

		if (pDevCtx->Connection.Bth.DisconnectEvent == NULL)
		{
			TraceError(
				TRACE_POWER,
				"Failed to create disconnect event"
			);
		}

		BOOL ret = RegisterWaitForSingleObject(
			&pDevCtx->Connection.Bth.DisconnectWaitHandle,
			pDevCtx->Connection.Bth.DisconnectEvent,
			DsBth_DisconnectEventCallback,
			pDevCtx,
			INFINITE,
			WT_EXECUTELONGFUNCTION | WT_EXECUTEONLYONCE
		);

		if (!ret)
		{
			TraceError(
				TRACE_POWER,
				"Failed to register wait for disconnect event"
			);
		}

#pragma endregion
	}

	//
	// Read hot-reloadable properties
	//
	DsDevice_HotReloadConfiguration(pDevCtx);

	//
	// Register global event to listen for changes on
	//

	swprintf_s(
		eventName,
		ARRAYSIZE(eventName),
		L"Global\\DsHidMiniConfigHotReloadEvent%ls",
		deviceAddress
	);

	TraceVerbose(
		TRACE_POWER,
		"Configuration reload event name: %ls",
		eventName
	);

	TCHAR* szSD = TEXT("D:")        // Discretionary ACL.
		TEXT("(A;OICI;GA;;;BA)");   // Allow full control to administrators.
	TEXT("(A;OICI;GA;;;SY)");   // Allow full control to the local system.
	SECURITY_ATTRIBUTES sa;
	sa.nLength = sizeof(SECURITY_ATTRIBUTES);
	sa.bInheritHandle = FALSE;
	ConvertStringSecurityDescriptorToSecurityDescriptor(
		szSD, 
		SDDL_REVISION_1, 
		&((&sa)->lpSecurityDescriptor), 
		NULL
	);

	if (pDevCtx->ConfigurationReloadEvent) {
		CloseHandle(pDevCtx->ConfigurationReloadEvent);
		pDevCtx->ConfigurationReloadEvent = NULL;
	}

	pDevCtx->ConfigurationReloadEvent = CreateEvent(
		&sa,
		FALSE,
		FALSE,
		eventName
	);

	if (pDevCtx->ConfigurationReloadEvent == NULL)
	{
		TraceError(
			TRACE_POWER,
			"Failed to create reload event"
		);
	}

	BOOL ret = RegisterWaitForSingleObject(
		&pDevCtx->ConfigurationReloadWaitHandle,
		pDevCtx->ConfigurationReloadEvent,
		DsDevice_HotRealodEventCallback,
		pDevCtx,
		INFINITE,
		WT_EXECUTELONGFUNCTION
	);

	if (!ret)
	{
		TraceError(
			TRACE_POWER,
			"Failed to register wait for reload event"
		);
	}

	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}

//
// Stop Bluetooth communication here
// 
NTSTATUS
DsHidMini_EvtWdfDeviceSelfManagedIoSuspend(
	WDFDEVICE Device
)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_POWER);

	//
	// Stop processing received output report packets
	//
	DMF_ThreadedBufferQueue_Stop(pDevCtx->OutputReport.Worker);

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		if (pDevCtx->Connection.Bth.DisconnectWaitHandle)
		{
			UnregisterWait(pDevCtx->Connection.Bth.DisconnectWaitHandle);
			pDevCtx->Connection.Bth.DisconnectWaitHandle = NULL;
		}

		if (pDevCtx->Connection.Bth.DisconnectEvent)
		{
			CloseHandle(pDevCtx->Connection.Bth.DisconnectEvent);
			pDevCtx->Connection.Bth.DisconnectEvent = NULL;
		}

		WdfTimerStop(pDevCtx->Connection.Bth.Timers.HidOutputReport, FALSE);

		DMF_DefaultTarget_StreamStop(pDevCtx->Connection.Bth.HidInterrupt.InputStreamerModule);
		DMF_DefaultTarget_StreamStop(pDevCtx->Connection.Bth.HidControl.OutputWriterModule);

		status = DsBth_SendDisconnectRequest(pDevCtx);

		if (!NT_SUCCESS(status))
		{
			TraceVerbose(
				TRACE_POWER,
				"DsBth_SendDisconnectRequest failed with status %!STATUS!",
				status
			);
		}
	}

	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}

//
// Initialize USB communication here
// 
_Use_decl_annotations_
NTSTATUS
DsHidMini_EvtDevicePrepareHardware(
	WDFDEVICE  Device,
	WDFCMRESLIST  ResourcesRaw,
	WDFCMRESLIST  ResourcesTranslated
)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx;
		
	UNREFERENCED_PARAMETER(ResourcesRaw);
	UNREFERENCED_PARAMETER(ResourcesTranslated);

	FuncEntry(TRACE_POWER);

	pDevCtx = DeviceGetContext(Device);

	//
	// Read common properties
	// 
	DsDevice_ReadConfiguration(Device);

	//
	// Initialize USB
	// 
	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		status = DsUdb_PrepareHardware(Device);
	}

	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}

//
// Power up
// 
NTSTATUS DsHidMini_EvtDeviceD0Entry(
	_In_ WDFDEVICE              Device,
	_In_ WDF_POWER_DEVICE_STATE PreviousState
)
{
	PDEVICE_CONTEXT         pDevCtx;
	NTSTATUS                status = STATUS_SUCCESS;
	BOOLEAN                 isTargetStarted;

	pDevCtx = DeviceGetContext(Device);
	isTargetStarted = FALSE;

	FuncEntry(TRACE_POWER);

	UNREFERENCED_PARAMETER(PreviousState);

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		do {
			//
			// Since continuous reader is configured for this interrupt-pipe, we must explicitly start
			// the I/O target to get the framework to post read requests.
			//
			status = WdfIoTargetStart(WdfUsbTargetPipeGetIoTarget(pDevCtx->Connection.Usb.InterruptInPipe));
			if (!NT_SUCCESS(status)) {
				TraceError(TRACE_POWER, "Failed to start interrupt read pipe %!STATUS!", status);
				break;
			}

			isTargetStarted = TRUE;
		} while (FALSE);

		if (!NT_SUCCESS(status)) {
			//
			// Failure in D0Entry will lead to device being removed. So let us stop the continuous
			// reader in preparation for the ensuing remove.
			//
			if (isTargetStarted) {
				WdfIoTargetStop(
					WdfUsbTargetPipeGetIoTarget(pDevCtx->Connection.Usb.InterruptInPipe),
					WdfIoTargetCancelSentIo
				);
			}
		}

		//
		// Instruct pad to send input reports
		// 
		status = DsUsb_Ds3Init(pDevCtx);

		if (!NT_SUCCESS(status))
		{
			TraceError( TRACE_POWER,
				"DsUsb_Ds3Init failed with status %!STATUS!",
				status);
		}
	}

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		if (PreviousState == WdfPowerDeviceD3Final)
		{
			DMF_DefaultTarget_Get(
				pDevCtx->Connection.Bth.HidInterrupt.InputStreamerModule,
				&pDevCtx->Connection.Bth.HidInterrupt.InputStreamerIoTarget
			);
			DMF_DefaultTarget_Get(
				pDevCtx->Connection.Bth.HidControl.OutputWriterModule,
				&pDevCtx->Connection.Bth.HidControl.OutputWriterIoTarget
			);
		}
		else
		{
			// Targets are started by default.
			//
			WdfIoTargetStart(pDevCtx->Connection.Bth.HidInterrupt.InputStreamerIoTarget);
			WdfIoTargetStart(pDevCtx->Connection.Bth.HidControl.OutputWriterIoTarget);
		}
	}
	
	//
	// Start processing received output report packets
	//
	DMF_ThreadedBufferQueue_Start(pDevCtx->OutputReport.Worker);
	
	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}

//
// Power down
// 
NTSTATUS DsHidMini_EvtDeviceD0Exit(
	_In_ WDFDEVICE Device,
	_In_ WDF_POWER_DEVICE_STATE TargetState
)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx;

	UNREFERENCED_PARAMETER(TargetState);

	FuncEntry(TRACE_POWER);

	pDevCtx = DeviceGetContext(Device);

	if (pDevCtx->ConfigurationReloadWaitHandle)
	{
		UnregisterWait(pDevCtx->ConfigurationReloadWaitHandle);
		pDevCtx->ConfigurationReloadWaitHandle = NULL;
	}

	if (pDevCtx->ConfigurationReloadEvent)
	{
		CloseHandle(pDevCtx->ConfigurationReloadEvent);
		pDevCtx->ConfigurationReloadEvent = NULL;
	}

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		WdfIoTargetStop(
			WdfUsbTargetPipeGetIoTarget(
				pDevCtx->Connection.Usb.InterruptInPipe),
			WdfIoTargetCancelSentIo
		);
	}

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		WdfIoTargetPurge(
			pDevCtx->Connection.Bth.HidInterrupt.InputStreamerIoTarget,
			WdfIoTargetPurgeIoAndWait
		);
		WdfIoTargetPurge(
			pDevCtx->Connection.Bth.HidControl.OutputWriterIoTarget,
			WdfIoTargetPurgeIoAndWait
		);
	}

	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}
