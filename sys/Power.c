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
	NTSTATUS				status = STATUS_SUCCESS;
	PDEVICE_CONTEXT			pDeviceContext;

	UNREFERENCED_PARAMETER(Device);

	PAGED_CODE();

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER, "%!FUNC! Entry");

	//
	// TODO: can be moved to D0Entry?
	// 

	pDeviceContext = DeviceGetContext(Device);

	if (pDeviceContext->ConnectionType == DsDeviceConnectionTypeBth)
	{
		//
		// Send magic packet, starts input report sending
		// 
		DsBth_Ds3Init(pDeviceContext);

#pragma region HID Interrupt Read
		
		status = WdfIoTargetFormatRequestForIoctl(
			pDeviceContext->Connection.Bth.BthIoTarget,
			pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest,
			IOCTL_BTHPS3_HID_INTERRUPT_READ,
			NULL,
			NULL,
			pDeviceContext->Connection.Bth.HidInterrupt.ReadMemory,
			NULL
		);
		if (!NT_SUCCESS(status))
		{
			TraceEvents(TRACE_LEVEL_ERROR,
				TRACE_POWER,
				"WdfIoTargetFormatRequestForIoctl failed with status %!STATUS!",
				status
			);
		}

		WdfRequestSetCompletionRoutine(
			pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest,
			DsBth_HidInterruptReadRequestCompletionRoutine,
			pDeviceContext
		);

		if (WdfRequestSend(
			pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest,
			pDeviceContext->Connection.Bth.BthIoTarget,
			WDF_NO_SEND_OPTIONS
		) == FALSE) {
			status = WdfRequestGetStatus(pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest);
		}
		if (!NT_SUCCESS(status))
		{
			TraceEvents(TRACE_LEVEL_ERROR,
				TRACE_POWER,
				"WdfRequestSend failed with status %!STATUS!",
				status
			);
		}

#pragma endregion

#pragma region HID Control Write
		
		status = WdfIoTargetFormatRequestForIoctl(
			pDeviceContext->Connection.Bth.BthIoTarget,
			pDeviceContext->Connection.Bth.HidControl.WriteRequest,
			IOCTL_BTHPS3_HID_CONTROL_WRITE,
			pDeviceContext->Connection.Bth.HidControl.WriteMemory,
			NULL,
			NULL,
			NULL
		);
		if (!NT_SUCCESS(status))
		{
			TraceEvents(TRACE_LEVEL_ERROR,
				TRACE_POWER,
				"WdfIoTargetFormatRequestForIoctl failed with status %!STATUS!",
				status
			);
		}

		WdfRequestSetCompletionRoutine(
			pDeviceContext->Connection.Bth.HidControl.WriteRequest,
			DsBth_HidControlWriteRequestCompletionRoutine,
			pDeviceContext
		);

		if (WdfRequestSend(
			pDeviceContext->Connection.Bth.HidControl.WriteRequest,
			pDeviceContext->Connection.Bth.BthIoTarget,
			WDF_NO_SEND_OPTIONS
		) == FALSE) {
			status = WdfRequestGetStatus(pDeviceContext->Connection.Bth.HidControl.WriteRequest);
		}
		if (!NT_SUCCESS(status))
		{
			TraceEvents(TRACE_LEVEL_ERROR,
				TRACE_POWER,
				"WdfRequestSend failed with status %!STATUS!",
				status
			);
		}

#pragma endregion
	}

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER, "%!FUNC! Exit");

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
	PDEVICE_CONTEXT                         pDeviceContext;
	WDF_USB_DEVICE_SELECT_CONFIG_PARAMS     configParams;
	UCHAR                                   index;
	WDFUSBPIPE                              pipe;
	WDF_USB_PIPE_INFORMATION                pipeInfo;
	UCHAR									controlTransferBuffer[CONTROL_TRANSFER_BUFFER_LENGTH];

	UNREFERENCED_PARAMETER(ResourcesRaw);
	UNREFERENCED_PARAMETER(ResourcesTranslated);

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER, "%!FUNC! Entry");

	pDeviceContext = DeviceGetContext(Device);

	//
	// Initialize USB framework object
	// 
	if (pDeviceContext->ConnectionType == DsDeviceConnectionTypeUsb
		&& pDeviceContext->Connection.Usb.UsbDevice == NULL)
	{
		status = WdfUsbTargetDeviceCreate(Device,
			WDF_NO_OBJECT_ATTRIBUTES,
			&pDeviceContext->Connection.Usb.UsbDevice
		);

		if (!NT_SUCCESS(status)) {
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_POWER,
				"WdfUsbTargetDeviceCreateWithParameters failed %!STATUS!", status);
			return status;
		}
	}

	//
	// Grab pipes and meta information
	// 
	if (pDeviceContext->ConnectionType == DsDeviceConnectionTypeUsb)
	{
#pragma region USB Interface & Pipe settings

		WDF_USB_DEVICE_SELECT_CONFIG_PARAMS_INIT_SINGLE_INTERFACE(&configParams);

		status = WdfUsbTargetDeviceSelectConfig(pDeviceContext->Connection.Usb.UsbDevice,
			WDF_NO_OBJECT_ATTRIBUTES,
			&configParams
		);

		if (!NT_SUCCESS(status)) {
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_POWER,
				"WdfUsbTargetDeviceSelectConfig failed %!STATUS!", status);
			return status;
		}

		pDeviceContext->Connection.Usb.UsbInterface = configParams.Types.SingleInterface.ConfiguredUsbInterface;

		//
		// Get pipe handles
		//
		for (index = 0; index < WdfUsbInterfaceGetNumConfiguredPipes(pDeviceContext->Connection.Usb.UsbInterface); index++) {

			WDF_USB_PIPE_INFORMATION_INIT(&pipeInfo);

			pipe = WdfUsbInterfaceGetConfiguredPipe(
				pDeviceContext->Connection.Usb.UsbInterface,
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
				TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER,
					"InterruptReadPipe is 0x%p\n", pipe);
				pDeviceContext->Connection.Usb.InterruptInPipe = pipe;
			}

			if (WdfUsbPipeTypeInterrupt == pipeInfo.PipeType &&
				WdfUsbTargetPipeIsOutEndpoint(pipe)) {
				TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER,
					"InterruptWritePipe is 0x%p\n", pipe);
				pDeviceContext->Connection.Usb.InterruptOutPipe = pipe;
			}
		}

		if (!pDeviceContext->Connection.Usb.InterruptInPipe || !pDeviceContext->Connection.Usb.InterruptOutPipe)
		{
			status = STATUS_INVALID_DEVICE_STATE;
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_POWER,
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
			status = SendControlRequest(
				pDeviceContext,
				BmRequestDeviceToHost,
				BmRequestClass,
				GetReport,
				Ds3FeatureDeviceAddress,
				0,
				controlTransferBuffer,
				CONTROL_TRANSFER_BUFFER_LENGTH);

			if (!NT_SUCCESS(status))
			{
				TraceEvents(TRACE_LEVEL_ERROR, TRACE_POWER,
					"Requesting device address failed with %!STATUS!", status);
				return status;
			}

			RtlCopyMemory(
				&pDeviceContext->DeviceAddress,
				&controlTransferBuffer[4],
				sizeof(BD_ADDR));

			//
			// Request host BTH address
			// 
			status = SendControlRequest(
				pDeviceContext,
				BmRequestDeviceToHost,
				BmRequestClass,
				GetReport,
				Ds3FeatureHostAddress,
				0,
				controlTransferBuffer,
				CONTROL_TRANSFER_BUFFER_LENGTH);

			if (!NT_SUCCESS(status))
			{
				TraceEvents(TRACE_LEVEL_ERROR, TRACE_POWER,
					"Requesting host address failed with %!STATUS!", status);
				return status;
			}

			RtlCopyMemory(
				&pDeviceContext->HostAddress,
				&controlTransferBuffer[2],
				sizeof(BD_ADDR));

			//
			// Send initial output report
			// 
			status = SendControlRequest(
				pDeviceContext,
				BmRequestHostToDevice,
				BmRequestClass,
				SetReport,
				USB_SETUP_VALUE(HidReportRequestTypeOutput, HidReportRequestIdOne),
				0,
				(PVOID)G_Ds3UsbHidOutputReport,
				DS3_USB_HID_OUTPUT_REPORT_SIZE);

			//
			// Auto-pair to first found radio
			// TODO: add option to disable
			// 
			(void)DsUsb_Ds3PairToFirstRadio(pDeviceContext);
		}
	}

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER, "%!FUNC! Exit (%!STATUS!)", status);

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
	PDEVICE_CONTEXT         pDeviceContext;
	NTSTATUS                status = STATUS_SUCCESS;
	BOOLEAN                 isTargetStarted;

	pDeviceContext = DeviceGetContext(Device);
	isTargetStarted = FALSE;

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER, "%!FUNC! Entry");

	UNREFERENCED_PARAMETER(PreviousState);

	if (pDeviceContext->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		//
		// Since continuous reader is configured for this interrupt-pipe, we must explicitly start
		// the I/O target to get the framework to post read requests.
		//
		status = WdfIoTargetStart(WdfUsbTargetPipeGetIoTarget(pDeviceContext->Connection.Usb.InterruptInPipe));
		if (!NT_SUCCESS(status)) {
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_POWER, "Failed to start interrupt read pipe %!STATUS!", status);
			goto End;
		}

		status = WdfIoTargetStart(WdfUsbTargetPipeGetIoTarget(pDeviceContext->Connection.Usb.InterruptOutPipe));
		if (!NT_SUCCESS(status)) {
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_POWER, "Failed to start interrupt write pipe %!STATUS!", status);
			goto End;
		}

		isTargetStarted = TRUE;

	End:

		if (!NT_SUCCESS(status)) {
			//
			// Failure in D0Entry will lead to device being removed. So let us stop the continuous
			// reader in preparation for the ensuing remove.
			//
			if (isTargetStarted) {
				WdfIoTargetStop(
					WdfUsbTargetPipeGetIoTarget(pDeviceContext->Connection.Usb.InterruptInPipe), 
					WdfIoTargetCancelSentIo
				);
				WdfIoTargetStop(WdfUsbTargetPipeGetIoTarget(
					pDeviceContext->Connection.Usb.InterruptOutPipe), 
					WdfIoTargetCancelSentIo
				);
			}
		}

		//
		// Instruct pad to send input reports
		// 
		status = DsUsb_Ds3Init(pDeviceContext);

		if (!NT_SUCCESS(status))
		{
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_POWER,
				"DsUsb_Ds3Init failed with status %!STATUS!",
				status);
		}
	}

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER, "%!FUNC! Exit");

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
	PDEVICE_CONTEXT pDeviceContext;

	UNREFERENCED_PARAMETER(TargetState);

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER, "%!FUNC! Entry");

	pDeviceContext = DeviceGetContext(Device);

	if (pDeviceContext->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		WdfIoTargetStop(WdfUsbTargetPipeGetIoTarget(
			pDeviceContext->Connection.Usb.InterruptInPipe),
			WdfIoTargetCancelSentIo
		);
		WdfIoTargetStop(WdfUsbTargetPipeGetIoTarget(
			pDeviceContext->Connection.Usb.InterruptOutPipe),
			WdfIoTargetCancelSentIo
		);
	}

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_POWER, "%!FUNC! Exit");

	return STATUS_SUCCESS;
}
