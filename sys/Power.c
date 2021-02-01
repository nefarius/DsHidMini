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

	PAGED_CODE();

	FuncEntry(TRACE_POWER);


	pDevCtx = DeviceGetContext(Device);

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		//
		// Set device address property
		// 
		
		if(swprintf_s(
			deviceAddress, 
			ARRAYSIZE(deviceAddress) * sizeof(WCHAR), 
			L"%02X%02X%02X%02X%02X%02X",
			pDevCtx->DeviceAddress.Address[0],
			pDevCtx->DeviceAddress.Address[1],
			pDevCtx->DeviceAddress.Address[2],
			pDevCtx->DeviceAddress.Address[3],
			pDevCtx->DeviceAddress.Address[4],
			pDevCtx->DeviceAddress.Address[5]
		) != -1)
		{
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
		//
		// Send magic packet, starts input report sending
		// 
		DsBth_Ds3Init(pDevCtx);

#pragma region HID Interrupt Read

		status = WdfIoTargetFormatRequestForIoctl(
			pDevCtx->Connection.Bth.BthIoTarget,
			pDevCtx->Connection.Bth.HidInterrupt.ReadRequest,
			IOCTL_BTHPS3_HID_INTERRUPT_READ,
			NULL,
			NULL,
			pDevCtx->Connection.Bth.HidInterrupt.ReadMemory,
			NULL
		);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_POWER,
				"WdfIoTargetFormatRequestForIoctl failed with status %!STATUS!",
				status
			);
		}

		WdfRequestSetCompletionRoutine(
			pDevCtx->Connection.Bth.HidInterrupt.ReadRequest,
			DsBth_HidInterruptReadRequestCompletionRoutine,
			pDevCtx
		);

		if (WdfRequestSend(
			pDevCtx->Connection.Bth.HidInterrupt.ReadRequest,
			pDevCtx->Connection.Bth.BthIoTarget,
			WDF_NO_SEND_OPTIONS
		) == FALSE) {
			status = WdfRequestGetStatus(pDevCtx->Connection.Bth.HidInterrupt.ReadRequest);
		}
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_POWER,
				"WdfRequestSend failed with status %!STATUS!",
				status
			);
		}

#pragma endregion

#pragma region HID Control Write

		//
		// Send initial output report
		// 
		status = DsBth_SendHidControlWriteRequestAsync(pDevCtx);

		//
		// Send preset output report (delayed)
		// 
		WdfTimerStart(
			pDevCtx->Connection.Bth.Timers.HidOutputReport,
			WDF_REL_TIMEOUT_IN_SEC(1)
		);

#pragma endregion
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
	NTSTATUS                                status = STATUS_SUCCESS;
	PDEVICE_CONTEXT                         pCtx;
	WDF_USB_DEVICE_SELECT_CONFIG_PARAMS     configParams;
	UCHAR                                   index;
	WDFUSBPIPE                              pipe;
	WDF_USB_PIPE_INFORMATION                pipeInfo;
	UCHAR									controlTransferBuffer[CONTROL_TRANSFER_BUFFER_LENGTH];
	WDF_DEVICE_PROPERTY_DATA				propertyData;
	ULONG									requiredSize = 0;
	DEVPROPTYPE								propertyType;

	UNREFERENCED_PARAMETER(ResourcesRaw);
	UNREFERENCED_PARAMETER(ResourcesTranslated);

	FuncEntry(TRACE_POWER);

	pCtx = DeviceGetContext(Device);

	//
	// Initialize USB framework object
	// 
	if (pCtx->ConnectionType == DsDeviceConnectionTypeUsb
		&& pCtx->Connection.Usb.UsbDevice == NULL)
	{
		status = WdfUsbTargetDeviceCreate(Device,
			WDF_NO_OBJECT_ATTRIBUTES,
			&pCtx->Connection.Usb.UsbDevice
		);

		if (!NT_SUCCESS(status)) {
			TraceError( 
				TRACE_POWER,
				"WdfUsbTargetDeviceCreate failed with status %!STATUS!", 
				status
			);
			return status;
		}
	}

	//
	// Grab pipes and meta information
	// 
	if (pCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		WdfUsbTargetDeviceGetDeviceDescriptor(
			pCtx->Connection.Usb.UsbDevice,
			&pCtx->Connection.Usb.UsbDeviceDescriptor
		);

		pCtx->VendorId = pCtx->Connection.Usb.UsbDeviceDescriptor.idVendor;
		TraceVerbose(TRACE_POWER, "[USB] VID: 0x%04X", pCtx->VendorId);
		pCtx->ProductId = pCtx->Connection.Usb.UsbDeviceDescriptor.idProduct;
		TraceVerbose(TRACE_POWER, "[USB] PID: 0x%04X", pCtx->ProductId);
		
#pragma region USB Interface & Pipe settings

		WDF_USB_DEVICE_SELECT_CONFIG_PARAMS_INIT_SINGLE_INTERFACE(&configParams);

		status = WdfUsbTargetDeviceSelectConfig(pCtx->Connection.Usb.UsbDevice,
			WDF_NO_OBJECT_ATTRIBUTES,
			&configParams
		);

		if (!NT_SUCCESS(status)) {
			TraceError( TRACE_POWER,
				"WdfUsbTargetDeviceSelectConfig failed %!STATUS!", status);
			return status;
		}

		pCtx->Connection.Usb.UsbInterface = configParams.Types.SingleInterface.ConfiguredUsbInterface;

		//
		// Get pipe handles
		//
		for (index = 0; index < WdfUsbInterfaceGetNumConfiguredPipes(pCtx->Connection.Usb.UsbInterface); index++) {

			WDF_USB_PIPE_INFORMATION_INIT(&pipeInfo);

			pipe = WdfUsbInterfaceGetConfiguredPipe(
				pCtx->Connection.Usb.UsbInterface,
				index, //PipeIndex,
				&pipeInfo
			);
			//
			// Tell the framework that it's okay to read less than
			// MaximumPacketSize
			//
			WdfUsbTargetPipeSetNoMaximumPacketSizeCheck(pipe);

			if (WdfUsbPipeTypeInterrupt == pipeInfo.PipeType &&
				WdfUsbTargetPipeIsInEndpoint(pipe)) {
				TraceInformation(TRACE_POWER,
					"InterruptReadPipe is 0x%p\n", pipe);
				pCtx->Connection.Usb.InterruptInPipe = pipe;
			}
		}

		if (!pCtx->Connection.Usb.InterruptInPipe)
		{
			status = STATUS_INVALID_DEVICE_STATE;
			TraceError( TRACE_POWER,
				"Device is not configured properly %!STATUS!\n",
				status);

			return status;
		}

#pragma endregion

		status = DsUsbConfigContReaderForInterruptEndPoint(Device);

		if (NT_SUCCESS(status))
		{
			//
			// Request device MAC address
			// 
			status = USB_SendControlRequest(
				pCtx,
				BmRequestDeviceToHost,
				BmRequestClass,
				GetReport,
				Ds3FeatureDeviceAddress,
				0,
				controlTransferBuffer,
				CONTROL_TRANSFER_BUFFER_LENGTH);

			if (!NT_SUCCESS(status))
			{
				TraceError( TRACE_POWER,
					"Requesting device address failed with %!STATUS!", status);
				return status;
			}

			RtlCopyMemory(
				&pCtx->DeviceAddress,
				&controlTransferBuffer[4],
				sizeof(BD_ADDR));

			TraceEvents(TRACE_LEVEL_INFORMATION,
			            TRACE_POWER,
			            "Device address: %02X:%02X:%02X:%02X:%02X:%02X",
			            pCtx->DeviceAddress.Address[0],
			            pCtx->DeviceAddress.Address[1],
			            pCtx->DeviceAddress.Address[2],
			            pCtx->DeviceAddress.Address[3],
			            pCtx->DeviceAddress.Address[4],
			            pCtx->DeviceAddress.Address[5]
			);
			
			//
			// Request host BTH address
			// 
			status = USB_SendControlRequest(
				pCtx,
				BmRequestDeviceToHost,
				BmRequestClass,
				GetReport,
				Ds3FeatureHostAddress,
				0,
				controlTransferBuffer,
				CONTROL_TRANSFER_BUFFER_LENGTH);

			if (!NT_SUCCESS(status))
			{
				TraceError( TRACE_POWER,
					"Requesting host address failed with %!STATUS!", status);
				return status;
			}

			RtlCopyMemory(
				&pCtx->HostAddress,
				&controlTransferBuffer[2],
				sizeof(BD_ADDR));

			//
			// Send initial output report
			// 
			status = USB_SendControlRequest(
				pCtx,
				BmRequestHostToDevice,
				BmRequestClass,
				SetReport,
				USB_SETUP_VALUE(HidReportRequestTypeOutput, HidReportRequestIdOne),
				0,
				(PVOID)G_Ds3UsbHidOutputReport,
				DS3_USB_HID_OUTPUT_REPORT_SIZE);

			//
			// Copy output report for later modification and reuse
			// 
			if (!pCtx->Connection.Usb.OutputReport)
			{
				pCtx->Connection.Usb.OutputReport = malloc(DS3_USB_HID_OUTPUT_REPORT_SIZE);

				RtlCopyMemory(
					pCtx->Connection.Usb.OutputReport,
					G_Ds3UsbHidOutputReport,
					DS3_USB_HID_OUTPUT_REPORT_SIZE
				);
			}
		}
	}

	//
	// Grab device properties exposed via BthPS3
	// 
	if (pCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_Bluetooth_DeviceVID);

		status = WdfDeviceQueryPropertyEx(
			Device,
			&propertyData,
			sizeof(USHORT),
			&pCtx->VendorId,
			&requiredSize,
			&propertyType
		);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_POWER,
				"Requesting DEVPKEY_Bluetooth_DeviceVID failed with %!STATUS!", 
				status
			);
			return status;
		}

		TraceVerbose(TRACE_POWER, "[BTH] VID: 0x%04X", pCtx->VendorId);

		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_Bluetooth_DevicePID);

		status = WdfDeviceQueryPropertyEx(
			Device,
			&propertyData,
			sizeof(USHORT),
			&pCtx->ProductId,
			&requiredSize,
			&propertyType
		);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_POWER,
				"Requesting DEVPKEY_Bluetooth_DevicePID failed with %!STATUS!",
				status
			);
			return status;
		}

		TraceVerbose(TRACE_POWER, "[BTH] PID: 0x%04X", pCtx->ProductId);
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
				TraceError( TRACE_POWER,
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
	
	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}

//
// Power down
// 
NTSTATUS DsHidMini_EvtDeviceD0Exit(
	_In_ WDFDEVICE              Device,
	_In_ WDF_POWER_DEVICE_STATE TargetState
)
{
	PDEVICE_CONTEXT pDevCtx;

	UNREFERENCED_PARAMETER(TargetState);

	FuncEntry(TRACE_POWER);

	pDevCtx = DeviceGetContext(Device);

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		WdfIoTargetStop(WdfUsbTargetPipeGetIoTarget(
			pDevCtx->Connection.Usb.InterruptInPipe),
			WdfIoTargetCancelSentIo
		);
	}

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		WdfTimerStop(pDevCtx->Connection.Bth.Timers.HidOutputReport, FALSE);
		WdfTimerStop(pDevCtx->Connection.Bth.Timers.HidControlConsume, FALSE);

		WdfIoTargetStop(pDevCtx->Connection.Bth.BthIoTarget, WdfIoTargetCancelSentIo);
	}

	FuncExitNoReturn(TRACE_POWER);

	return STATUS_SUCCESS;
}
