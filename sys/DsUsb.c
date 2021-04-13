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
		DEFAULT_CONTROL_TRANSFER_TIMEOUT
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
	ULONG requiredSize = 0;
	DEVPROPTYPE propertyType;
	WCHAR deviceAddress[13];
	WCHAR dcEventName[44];

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

		status = WdfUsbTargetDeviceSelectConfig(pDevCtx->Connection.Usb.UsbDevice,
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

#pragma endregion

		//
		// Disconnect Bluetooth connection, if detected
		//

		swprintf_s(
			dcEventName,
			ARRAYSIZE(dcEventName),
			L"Global\\DsHidMiniDisconnectEvent%ls",
			deviceAddress
		);

		HANDLE dcEvent = OpenEvent(
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
			TraceError(
				TRACE_DSUSB,
				"GetLastError: %d",
				GetLastError()
			);
		}

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

#pragma endregion
		
		//
		// Send initial output report
		// 
		(void)USB_WriteInterruptPipeAsync(
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

	FuncEntry(TRACE_DSUSB);

	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);

	return status;
}

NTSTATUS DsUdb_D0Exit(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;

	FuncEntry(TRACE_DSUSB);

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
