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
	_In_ PDEVICE_CONTEXT Context
)
{
	WDF_MEMORY_DESCRIPTOR  writeBufDesc;
	ULONG bytesWritten;
	NTSTATUS status;

	FuncEntry(TRACE_DSUSB);
	
	WDF_MEMORY_DESCRIPTOR_INIT_HANDLE(
		&writeBufDesc,
		Context->Connection.Usb.OutputReportMemory,
		NULL
	);
	
	status = WdfUsbTargetPipeWriteSynchronously(
		Context->Connection.Usb.InterruptOutPipe,
		NULL,
		NULL,
		&writeBufDesc,
		&bytesWritten
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
