#include "Driver.h"
#include "DsUsb.tmh"


//
// Sends a custom buffer to the device's control endpoint.
// 
NTSTATUS
USB_SendControlRequest(
	_In_ PDEVICE_CONTEXT Context,
	_In_ WDF_USB_BMREQUEST_DIRECTION Direction,
	_In_ WDF_USB_BMREQUEST_TYPE Type,
	_In_ BYTE Request,
	_In_ USHORT Value,
	_In_ USHORT Index,
	_In_ PVOID Buffer,
	_In_ ULONG BufferLength)
{
	NTSTATUS                        status;
	WDF_USB_CONTROL_SETUP_PACKET    controlSetupPacket;
	WDF_REQUEST_SEND_OPTIONS        sendOptions;
	WDF_MEMORY_DESCRIPTOR           memDesc;
	ULONG                           bytesTransferred;

	FuncEntry(TRACE_DSUSB);
	
	WDF_REQUEST_SEND_OPTIONS_INIT(
		&sendOptions,
		WDF_REQUEST_SEND_OPTION_TIMEOUT
	);

	WDF_REQUEST_SEND_OPTIONS_SET_TIMEOUT(
		&sendOptions,
		WDF_REL_TIMEOUT_IN_SEC(3)
	);

	switch (Type)
	{
	case BmRequestClass:
		WDF_USB_CONTROL_SETUP_PACKET_INIT_CLASS(&controlSetupPacket,
			Direction,
			BmRequestToInterface,
			Request,
			Value,
			Index);
		break;

	default:
		return STATUS_INVALID_PARAMETER;
	}

	WDF_MEMORY_DESCRIPTOR_INIT_BUFFER(&memDesc,
		Buffer,
		BufferLength);

	status = WdfUsbTargetDeviceSendControlTransferSynchronously(
		Context->Connection.Usb.UsbDevice,
		WDF_NO_HANDLE,
		&sendOptions,
		&controlSetupPacket,
		&memDesc,
		&bytesTransferred);

	if (!NT_SUCCESS(status))
	{
		TraceError(TRACE_DSUSB,
			"WdfUsbTargetDeviceSendControlTransferSynchronously failed with status %!STATUS! (%d)\n",
			status, bytesTransferred);
	}

	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);
	
	return status;
}

//
// Prepare continuous reader
// 
NTSTATUS
DsUsbConfigContReaderForInterruptEndPoint(
	_In_ WDFDEVICE Device
)
{
	WDF_USB_CONTINUOUS_READER_CONFIG contReaderConfig;
	NTSTATUS status;
	PDEVICE_CONTEXT pDevCtx;

	FuncEntry(TRACE_DSUSB);

	pDevCtx = DeviceGetContext(Device);

	WDF_USB_CONTINUOUS_READER_CONFIG_INIT(&contReaderConfig,
		DsUsb_EvtUsbInterruptPipeReadComplete,
		Device,    // Context
		INTERRUPT_IN_BUFFER_LENGTH);   // TransferLength

	contReaderConfig.EvtUsbTargetPipeReadersFailed = DsUsbEvtUsbInterruptReadersFailed;

	//
	// Reader requests are not posted to the target automatically.
	// Driver must explicitly call WdfIoTargetStart to kick start the
	// reader.  In this sample, it's done in D0Entry.
	// By default, framework queues two requests to the target
	// endpoint. Driver can configure up to 10 requests with CONFIG macro.
	//
	status = WdfUsbTargetPipeConfigContinuousReader(
		pDevCtx->Connection.Usb.InterruptInPipe,
		&contReaderConfig
	);

	if (!NT_SUCCESS(status))
	{
		TraceError(TRACE_DSUSB,
		           "WdfUsbTargetPipeConfigContinuousReader failed %x\n",
		           status);
	}

	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);

	return status;
}

//
// Send buffer to Interrupt OUT endpoint asynchronously
// 
NTSTATUS
USB_WriteInterruptPipeAsync(
	WDFIOTARGET IoTarget, 
	WDFUSBPIPE Pipe, 
	PVOID Buffer, 
	size_t BufferLength
)
{
	NTSTATUS                        status;
	WDFREQUEST                      request;
	WDF_OBJECT_ATTRIBUTES           attribs;
	WDFMEMORY                       memory;
	PVOID                           writeBufferPointer;

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);

	status = WdfRequestCreate(&attribs,
		IoTarget,
		&request);
	if (!NT_SUCCESS(status)) {
		TraceError(
			TRACE_DSUSB,
			"WdfRequestCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = request;

	status = WdfMemoryCreate(&attribs,
		NonPagedPoolNx,
		DS3_POOL_TAG,
		BufferLength,
		&memory,
		&writeBufferPointer);
	if (!NT_SUCCESS(status)) {
		TraceError(
			TRACE_DSUSB,
			"WdfMemoryCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	RtlCopyMemory(writeBufferPointer, Buffer, BufferLength);

	status = WdfUsbTargetPipeFormatRequestForWrite(
		Pipe,
		request,
		memory,
		NULL
	);
	if (!NT_SUCCESS(status)) {
		TraceError(
			TRACE_DSUSB,
			"WdfUsbTargetPipeFormatRequestForWrite failed with status %!STATUS!",
			status
		);
		return status;
	}

	WdfRequestSetCompletionRoutine(
		request,
		EvtUsbRequestCompletionRoutine,
		NULL
	);

	if (WdfRequestSend(request,
		IoTarget,
		NULL) == FALSE)
	{
		status = WdfRequestGetStatus(request);
	}

	if (!NT_SUCCESS(status)) {
		TraceError(
			TRACE_DSUSB,
			"WdfRequestSend failed with status %!STATUS!",
			status
		);
	}

	return status;
}

//
// Send the Output Report buffer content to the Interrupt OUT endpoint and wait for completion
// 
NTSTATUS
USB_WriteInterruptOutSync(
	_In_ PDEVICE_CONTEXT Context,
	_In_ PWDF_MEMORY_DESCRIPTOR Memory
)
{
	ULONG bytesWritten;
	NTSTATUS status;

	FuncEntry(TRACE_DSUSB);
		
	status = WdfUsbTargetPipeWriteSynchronously(
		Context->Connection.Usb.InterruptOutPipe,
		NULL,
		NULL,
		Memory,
		&bytesWritten
	);

	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);

	return status;
}

NTSTATUS DsUdb_PrepareHardware(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	WDF_USB_DEVICE_SELECT_CONFIG_PARAMS configParams;
	UCHAR index;
	WDFUSBPIPE pipe;
	WDF_USB_PIPE_INFORMATION pipeInfo;
	UCHAR controlTransferBuffer[CONTROL_TRANSFER_BUFFER_LENGTH];
	WDF_DEVICE_PROPERTY_DATA propertyData;
	WCHAR deviceAddress[13];
	WCHAR friendlyName[128];
	size_t friendlyNameSize = 0;
	UCHAR identification[64];
	UINT64 hostAddress;

	FuncEntry(TRACE_DSUSB);

	do
	{
		//
		// Initialize USB framework object
		// 
		if (pDevCtx->Connection.Usb.UsbDevice == NULL)
		{
			status = WdfUsbTargetDeviceCreate(
				Device,
				WDF_NO_OBJECT_ATTRIBUTES,
				&pDevCtx->Connection.Usb.UsbDevice
			);

			if (!NT_SUCCESS(status))
			{
				TraceError(
					TRACE_DSUSB,
					"WdfUsbTargetDeviceCreate failed with status %!STATUS!",
					status
				);
				break;
			}
		}

		//
		// Grab details from embedded descriptor
		// 
		WdfUsbTargetDeviceGetDeviceDescriptor(
			pDevCtx->Connection.Usb.UsbDevice,
			&pDevCtx->Connection.Usb.UsbDeviceDescriptor
		);

		pDevCtx->VendorId = pDevCtx->Connection.Usb.UsbDeviceDescriptor.idVendor;
		TraceVerbose(TRACE_DSUSB, "[USB] VID: 0x%04X", pDevCtx->VendorId);
		pDevCtx->ProductId = pDevCtx->Connection.Usb.UsbDeviceDescriptor.idProduct;
		TraceVerbose(TRACE_DSUSB, "[USB] PID: 0x%04X", pDevCtx->ProductId);

#pragma region USB Interface & Pipe settings

		WDF_USB_DEVICE_SELECT_CONFIG_PARAMS_INIT_SINGLE_INTERFACE(&configParams);

		status = WdfUsbTargetDeviceSelectConfig(
			pDevCtx->Connection.Usb.UsbDevice,
			WDF_NO_OBJECT_ATTRIBUTES,
			&configParams
		);

		if (!NT_SUCCESS(status))
		{
			TraceError(TRACE_DSUSB,
			           "WdfUsbTargetDeviceSelectConfig failed %!STATUS!",
			           status
			);
			break;
		}

		pDevCtx->Connection.Usb.UsbInterface = configParams.Types.SingleInterface.ConfiguredUsbInterface;

		//
		// Grab product name to use as FriendlyName
		// 
		status = WdfUsbTargetDeviceAllocAndQueryString(
			pDevCtx->Connection.Usb.UsbDevice,
			WDF_NO_OBJECT_ATTRIBUTES,
			&pDevCtx->Connection.Usb.ProductString,
			NULL,
			pDevCtx->Connection.Usb.UsbDeviceDescriptor.iProduct,
			0x0409
		);
		if (!NT_SUCCESS(status))
		{
			TraceEvents(
				TRACE_LEVEL_WARNING,
				TRACE_DEVICE,
				"Requesting iProduct failed (status: 0x%x), device might not support this string",
				status
			);
		}
		else
		{
			//
			// Set friendly name
			// 

			RtlZeroMemory(
				friendlyName, 
				ARRAYSIZE(friendlyName) * sizeof(WCHAR)
			);
			RtlCopyMemory(
				friendlyName,
				WdfMemoryGetBuffer(
					pDevCtx->Connection.Usb.ProductString,
					&friendlyNameSize
				),
				friendlyNameSize
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
					TRACE_DSUSB,
					"Setting DEVPKEY_Device_FriendlyName failed with status %!STATUS!",
					status
				);
			}
		}

		//
		// Get pipe handles
		//
		for (index = 0; index < WdfUsbInterfaceGetNumConfiguredPipes(pDevCtx->Connection.Usb.UsbInterface); index++)
		{
			WDF_USB_PIPE_INFORMATION_INIT(&pipeInfo);

			pipe = WdfUsbInterfaceGetConfiguredPipe(
				pDevCtx->Connection.Usb.UsbInterface,
				index, //PipeIndex,
				&pipeInfo
			);
			//
			// Tell the framework that it's okay to read less than
			// MaximumPacketSize
			//
			WdfUsbTargetPipeSetNoMaximumPacketSizeCheck(pipe);

			if (WdfUsbPipeTypeInterrupt == pipeInfo.PipeType &&
				WdfUsbTargetPipeIsInEndpoint(pipe))
			{
				TraceInformation(TRACE_DSUSB,
				                 "InterruptReadPipe is 0x%p", pipe);
				pDevCtx->Connection.Usb.InterruptInPipe = pipe;
			}

			if (WdfUsbPipeTypeInterrupt == pipeInfo.PipeType &&
				WdfUsbTargetPipeIsOutEndpoint(pipe))
			{
				TraceInformation(TRACE_DSUSB,
				                 "InterruptWritePipe is 0x%p", pipe);
				pDevCtx->Connection.Usb.InterruptOutPipe = pipe;
			}
		}

		//
		// Validate that we got both required pipes
		// 
		if (!pDevCtx->Connection.Usb.InterruptInPipe
			|| !pDevCtx->Connection.Usb.InterruptOutPipe)
		{
			status = STATUS_INVALID_DEVICE_STATE;
			TraceError(
				TRACE_DSUSB,
				"Device is not configured properly %!STATUS!\n",
				status
			);
			break;
		}

#pragma endregion

		status = DsUsbConfigContReaderForInterruptEndPoint(Device);

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSUSB,
				"DsUsbConfigContReaderForInterruptEndPoint failed with %!STATUS!",
				status
			);
			break;
		}

#pragma region Request device MAC address
		
		//
		// Request device MAC address
		// 
		status = USB_SendControlRequest(
			pDevCtx,
			BmRequestDeviceToHost,
			BmRequestClass,
			GetReport,
			Ds3FeatureDeviceAddress,
			0,
			controlTransferBuffer,
			CONTROL_TRANSFER_BUFFER_LENGTH);

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSUSB,
				"Requesting device address failed with %!STATUS!",
				status
			);
			break;
		}

		RtlCopyMemory(
			&pDevCtx->DeviceAddress,
			&controlTransferBuffer[4],
			sizeof(BD_ADDR));

		TraceEvents(TRACE_LEVEL_INFORMATION,
		            TRACE_DSUSB,
		            "Device address: %02X:%02X:%02X:%02X:%02X:%02X",
		            pDevCtx->DeviceAddress.Address[0],
		            pDevCtx->DeviceAddress.Address[1],
		            pDevCtx->DeviceAddress.Address[2],
		            pDevCtx->DeviceAddress.Address[3],
		            pDevCtx->DeviceAddress.Address[4],
		            pDevCtx->DeviceAddress.Address[5]
		);

		//
		// Convert to expected hex string
		// 
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
				TRACE_DSUSB,
				"Setting DEVPKEY_Bluetooth_DeviceAddress failed with status %!STATUS!",
				status
			);
		}
		
#pragma endregion

		//
		// Disconnect Bluetooth connection, if detected
		//

		DsDevice_InvokeLocalBthDisconnect(pDevCtx);

#pragma region Request host BTH address
		
		//
		// Request host BTH address
		// 
		status = USB_SendControlRequest(
			pDevCtx,
			BmRequestDeviceToHost,
			BmRequestClass,
			GetReport,
			Ds3FeatureHostAddress,
			0,
			controlTransferBuffer,
			CONTROL_TRANSFER_BUFFER_LENGTH
		);

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSUSB,
				"Requesting host address failed with %!STATUS!",
				status
			);
			return status;
		}

		RtlCopyMemory(
			&pDevCtx->HostAddress,
			&controlTransferBuffer[2],
			sizeof(BD_ADDR));

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
				TRACE_DSUSB,
				"Setting DEVPKEY_BluetoothRadio_Address failed with status %!STATUS!",
				status
			);
		}

		status = STATUS_SUCCESS;
		
#pragma endregion

#pragma region Request Model Identification
		
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

#pragma endregion

		//
		// Attempt automatic pairing
		// 
		if (!pDevCtx->Configuration.DisableAutoPairing)
		{
			//
			// Auto-pair to first found radio
			// 
			status = DsUsb_Ds3PairToFirstRadio(Device);

			if (!NT_SUCCESS(status))
			{
				TraceError(
					TRACE_DSUSB,
					"DsUsb_Ds3PairToFirstRadio failed with status %!STATUS!",
					status
				);
			}
		}
		else
		{
			TraceInformation(
				TRACE_DSUSB,
				"Auto-pairing disabled in device configuration");
		}
		
		//
		// Send initial output report
		// 
		status = USB_WriteInterruptPipeAsync(
			WdfUsbTargetDeviceGetIoTarget(pDevCtx->Connection.Usb.UsbDevice),
			pDevCtx->Connection.Usb.InterruptOutPipe,
			(PVOID)G_Ds3UsbHidOutputReport,
			DS3_USB_HID_OUTPUT_REPORT_SIZE
		);
	}
	while (FALSE);
	
	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);

	return status;
}

NTSTATUS DsUsb_D0Entry(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DSUSB);

	do
	{
		//
		// Since continuous reader is configured for this interrupt-pipe, we must explicitly start
		// the I/O target to get the framework to post read requests.
		//
		status = WdfIoTargetStart(WdfUsbTargetPipeGetIoTarget(pDevCtx->Connection.Usb.InterruptInPipe));
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSUSB,
				"Failed to start interrupt read pipe %!STATUS!",
				status
			);
			break;
		}

		//
		// Instruct pad to send input reports
		// 
		status = DsUsb_Ds3Init(pDevCtx);

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSUSB,
				"DsUsb_Ds3Init failed with status %!STATUS!",
				status);
			break;
		}
	}
	while (FALSE);

	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);

	return status;
}

NTSTATUS DsUdb_D0Exit(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DSUSB);

	WdfIoTargetStop(
		WdfUsbTargetPipeGetIoTarget(
			pDevCtx->Connection.Usb.InterruptInPipe),
		WdfIoTargetCancelSentIo
	);

	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);

	return status;
}

//
// Reader failed for some reason
// 
BOOLEAN
DsUsbEvtUsbInterruptReadersFailed(
	_In_ WDFUSBPIPE Pipe,
	_In_ NTSTATUS Status,
	_In_ USBD_STATUS UsbdStatus
)
{
	UNREFERENCED_PARAMETER(UsbdStatus);
	UNREFERENCED_PARAMETER(Pipe);

	TraceError(
		TRACE_DSUSB,
		"%!FUNC! called with status %!STATUS!",
		Status
	);

	return TRUE;
}

void EvtUsbRequestCompletionRoutine(
	WDFREQUEST Request,
	WDFIOTARGET Target,
	PWDF_REQUEST_COMPLETION_PARAMS Params,
	WDFCONTEXT Context
)
{
	UNREFERENCED_PARAMETER(Target);
#if !DBG
	UNREFERENCED_PARAMETER(Params);
#endif
	UNREFERENCED_PARAMETER(Context);

	TraceVerbose(
		TRACE_DSUSB,
		"%!FUNC! completed with status %!STATUS!",
		Params->IoStatus.Status
	);

	WdfObjectDelete(Request);
}
