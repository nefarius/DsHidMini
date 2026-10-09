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
	_Inout_ PVOID Buffer,
	_In_ ULONG BufferLength,
	_Out_opt_ PULONG BytesTransferred
)
{
	NTSTATUS status;
	WDF_USB_CONTROL_SETUP_PACKET controlSetupPacket;
	WDF_REQUEST_SEND_OPTIONS sendOptions;
	WDF_MEMORY_DESCRIPTOR memDesc;
	ULONG bytesTransferred = 0;

	FuncEntry(TRACE_DSUSB);

	if (BytesTransferred != NULL)
	{
		*BytesTransferred = 0;
	}

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
		WDF_USB_CONTROL_SETUP_PACKET_INIT_CLASS(
			&controlSetupPacket,
			Direction,
			BmRequestToInterface,
			Request,
			Value,
			Index
		);
		break;

	default:
		return STATUS_INVALID_PARAMETER;
	}

	WDF_MEMORY_DESCRIPTOR_INIT_BUFFER(
		&memDesc,
		Buffer,
		BufferLength
	);

	if (!NT_SUCCESS(status = WdfUsbTargetDeviceSendControlTransferSynchronously(
		Context->Connection.Usb.UsbDevice,
		WDF_NO_HANDLE,
		&sendOptions,
		&controlSetupPacket,
		&memDesc,
		&bytesTransferred
	)))
	{
		TraceError(
			TRACE_DSUSB,
			"WdfUsbTargetDeviceSendControlTransferSynchronously failed with status %!STATUS! (%d)",
			status,
			bytesTransferred
		);
	}

	if (BytesTransferred != NULL)
	{
		*BytesTransferred = bytesTransferred;
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

	FuncEntry(TRACE_DSUSB);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	WDF_USB_CONTINUOUS_READER_CONFIG_INIT(
		&contReaderConfig,
		DsUsb_EvtUsbInterruptPipeReadComplete,
		Device, // Context
		INTERRUPT_IN_BUFFER_LENGTH // TransferLength
	);

	contReaderConfig.EvtUsbTargetPipeReadersFailed = DsUsbEvtUsbInterruptReadersFailed;

	//
	// Reader requests are not posted to the target automatically.
	// Driver must explicitly call WdfIoTargetStart to kick start the
	// reader.  In this sample, it's done in D0Entry.
	// By default, framework queues two requests to the target
	// endpoint. Driver can configure up to 10 requests with CONFIG macro.
	//
	if (!NT_SUCCESS(status = WdfUsbTargetPipeConfigContinuousReader(
		pDevCtx->Connection.Usb.InterruptInPipe,
		&contReaderConfig
	)))
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
	NTSTATUS status;
	WDFREQUEST request;
	WDF_OBJECT_ATTRIBUTES attributes;
	WDFMEMORY memory;
	PVOID writeBufferPointer;

	WDF_OBJECT_ATTRIBUTES_INIT(&attributes);

	if (!NT_SUCCESS(status = WdfRequestCreate(
		&attributes,
		IoTarget,
		&request
	)))
	{
		TraceError(
			TRACE_DSUSB,
			"WdfRequestCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
	attributes.ParentObject = request;

	if (!NT_SUCCESS(status = WdfMemoryCreate(
		&attributes,
		NonPagedPoolNx,
		DS3_POOL_TAG,
		BufferLength,
		&memory,
		&writeBufferPointer
	)))
	{
		TraceError(
			TRACE_DSUSB,
			"WdfMemoryCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	RtlCopyMemory(writeBufferPointer, Buffer, BufferLength);

	if (!NT_SUCCESS(status = WdfUsbTargetPipeFormatRequestForWrite(
		Pipe,
		request,
		memory,
		NULL
	)))
	{
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

	if (!NT_SUCCESS(status))
	{
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
	_In_ PWDF_MEMORY_DESCRIPTOR Memory,
	_In_ ULONG TimeoutMs
)
{
	ULONG bytesWritten;
	NTSTATUS status;
	WDF_REQUEST_SEND_OPTIONS sendOptions;

	FuncEntry(TRACE_DSUSB);

	//
	// A stalled interrupt OUT would otherwise hold the output worker until
	// the pipe is cancelled. DualShock 3 keeps the 3 second bound shared
	// with USB_SendControlRequest; third-party HID passes a shorter one.
	// 
	WDF_REQUEST_SEND_OPTIONS_INIT(
		&sendOptions,
		WDF_REQUEST_SEND_OPTION_TIMEOUT
	);

	WDF_REQUEST_SEND_OPTIONS_SET_TIMEOUT(
		&sendOptions,
		WDF_REL_TIMEOUT_IN_MS(TimeoutMs)
	);

	status = WdfUsbTargetPipeWriteSynchronously(
		Context->Connection.Usb.InterruptOutPipe,
		NULL,
		&sendOptions,
		Memory,
		&bytesWritten
	);

	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);

	return status;
}

NTSTATUS DsUsb_PrepareHardware(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	WDF_USB_DEVICE_SELECT_CONFIG_PARAMS configParams;
	WDF_USB_PIPE_INFORMATION pipeInfo;
	WDF_DEVICE_PROPERTY_DATA propertyData;
	WCHAR friendlyName[128];
	size_t friendlyNameSize = 0;
	UCHAR identification[64];

	FuncEntry(TRACE_DSUSB);

	do
	{
		//
		// Initialize USB framework object
		// 
		if (pDevCtx->Connection.Usb.UsbDevice == NULL)
		{
			if (!NT_SUCCESS(status = WdfUsbTargetDeviceCreate(
				Device,
				WDF_NO_OBJECT_ATTRIBUTES,
				&pDevCtx->Connection.Usb.UsbDevice
			)))
			{
				TraceError(
					TRACE_DSUSB,
					"WdfUsbTargetDeviceCreate failed with status %!STATUS!",
					status
				);
				EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfUsbTargetDeviceCreate", status);
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
		DsDevice_AssignDeviceType(Device);

#pragma region USB Interface & Pipe settings

		WDF_USB_DEVICE_SELECT_CONFIG_PARAMS_INIT_SINGLE_INTERFACE(&configParams);

		if (!NT_SUCCESS(status = WdfUsbTargetDeviceSelectConfig(
			pDevCtx->Connection.Usb.UsbDevice,
			WDF_NO_OBJECT_ATTRIBUTES,
			&configParams
		)))
		{
			TraceError(
				TRACE_DSUSB,
				"WdfUsbTargetDeviceSelectConfig failed %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfUsbTargetDeviceSelectConfig", status);
			break;
		}

		pDevCtx->Connection.Usb.UsbInterface = configParams.Types.SingleInterface.ConfiguredUsbInterface;

		//
		// Grab product name to use as FriendlyName
		// 
		if (!NT_SUCCESS(status = WdfUsbTargetDeviceAllocAndQueryString(
			pDevCtx->Connection.Usb.UsbDevice,
			WDF_NO_OBJECT_ATTRIBUTES,
			&pDevCtx->Connection.Usb.ProductString,
			NULL,
			pDevCtx->Connection.Usb.UsbDeviceDescriptor.iProduct,
			0x0409
		)))
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
		for (UCHAR index = 0; index < WdfUsbInterfaceGetNumConfiguredPipes(pDevCtx->Connection.Usb.UsbInterface); index++)
		{
			WDF_USB_PIPE_INFORMATION_INIT(&pipeInfo);

			const WDFUSBPIPE pipe = WdfUsbInterfaceGetConfiguredPipe(
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
		// Interrupt IN is required to receive input reports at all.
		// Interrupt OUT is optional (see issue #321): some aftermarket pads
		// (e.g. the Retro Fighters Defender in its original USB mode) only
		// implement the pipe the console actually reads from and never
		// expose a writable one. Default the output transport purely from
		// pipe availability here; DMF_DsHidMini_Open may override it once
		// configuration has been loaded (UsbOutputReportTransport), see
		// Configuration.c.
		// 
		if (!pDevCtx->Connection.Usb.InterruptInPipe)
		{
			status = STATUS_INVALID_DEVICE_STATE;
			TraceError(
				TRACE_DSUSB,
				"Device is not configured properly %!STATUS!\n",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"Pipe enumeration", status);
			break;
		}

		if (!pDevCtx->Connection.Usb.InterruptOutPipe)
		{
			TraceWarning(
				TRACE_DSUSB,
				"Device has no interrupt OUT pipe; output reports (LEDs/rumble) will be sent over the control endpoint instead"
			);

			pDevCtx->Connection.Usb.OutputTransport = DsUsbOutputReportTransportControlEndpoint;
		}
		else
		{
			pDevCtx->Connection.Usb.OutputTransport = DsUsbOutputReportTransportInterruptOut;
		}

		//
		// Readers of DEVPKEY_DsHidMini_RO_OutputReportStatus always see a
		// defined value. Only a third-party HID stall changes it afterwards.
		//
		ThirdPartyHid_ResetOutputStall(pDevCtx);

#pragma endregion

		if (!NT_SUCCESS(status = DsUsbConfigContReaderForInterruptEndPoint(Device)))
		{
			TraceError(
				TRACE_DSUSB,
				"DsUsbConfigContReaderForInterruptEndPoint failed with %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DsUsbConfigContReaderForInterruptEndPoint", status);
			break;
		}

#pragma region Request Model Identification

		//
		// See https://github.com/nefarius/DsHidMini/issues/50
		// Moved ahead of MAC discovery to mirror the order the PS3 itself
		// queries a freshly plugged-in pad (GET Feature 0x01 before 0xF2).
		// Third-party HID adapters do not implement this report. Asking
		// them can drop ShanWan firmware into its fallback identity.
		// 
		ULONG identificationLength = 0;

		RtlZeroMemory(identification, sizeof(identification));
		DsIdentification_Clear(Device);

		if (pDevCtx->DeviceType != DsDeviceTypeThirdPartyHid &&
			NT_SUCCESS(USB_SendControlRequest(
			pDevCtx,
			BmRequestDeviceToHost,
			BmRequestClass,
			GetReport,
			0x0301,
			0,
			identification,
			ARRAYSIZE(identification),
			&identificationLength
		)))
		{
			DsIdentification_PublishFromReport(
				Device,
				identification,
				identificationLength
			);
		}

#pragma endregion

#pragma region Request device MAC address

		//
		// Request device MAC address. Retried internally; on persistent
		// failure this synthesizes a deterministic fallback address instead
		// of failing PrepareHardware (see issue #321).
		// 
		DsUsb_Ds3RequestDeviceAddress(Device);

#pragma endregion

		if (pDevCtx->SupportsBluetoothAddressReports)
		{
			//
			// Ask any existing wireless instance of this MAC to disconnect
			// (issue #330). Wired presence is discovered via PnP, not a
			// named event.
			// 
			DsDevice_InvokeLocalBthDisconnect(pDevCtx);

#pragma region Request host BTH address

			//
			// Request host BTH address
			// 
			if (!NT_SUCCESS(DsUsb_Ds3RequestHostAddress(Device)))
			{
				TraceError(
					TRACE_DSUSB,
					"Setting DsUsb_Ds3RequestHostAddress failed with status %!STATUS!",
					status
				);
			}

#pragma endregion
		}
		else
		{
			//
			// This device never reported its own Bluetooth MAC, so asking
			// it (or any paired host radio) about pairing state is
			// meaningless. Keep the host address zeroed and record why via
			// the same property a genuine failure would have used, so
			// ControlApp can show a clear "not supported" reason instead of
			// a bogus 00:00:00:00:00:00 read failure.
			// 
			const BD_ADDR zeroHostAddress = { 0 };
			NTSTATUS notSupportedStatus = STATUS_NOT_SUPPORTED;

			RtlCopyMemory(&pDevCtx->HostAddress, &zeroHostAddress, sizeof(BD_ADDR));

			WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_LastHostRequestStatus);
			propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
			propertyData.Lcid = LOCALE_NEUTRAL;

			(void)WdfDeviceAssignProperty(
				Device,
				&propertyData,
				DEVPROP_TYPE_NTSTATUS,
				sizeof(NTSTATUS),
				&notSupportedStatus
			);
		}

		//
		// Soft-read EEPROM page 0xA0 (Feature 0xEF) after address/pairing
		// discovery and before the first output report. Failure must not
		// abort PrepareHardware; DsMotion keeps the documented nominal
		// fallback in that case (issue #217).
		// 
		if (pDevCtx->DeviceType == DsDeviceTypeThirdPartyHid)
		{
			//
			// No EEPROM. Keep the nominal rest sample and do not send a
			// hardware cal byte (that write shares the large-motor slots).
			// 
			pDevCtx->Motion.Path = DsIdentificationMotionPathPlainZero;
			pDevCtx->Motion.Fallback = TRUE;
			pDevCtx->Motion.SendHardwareCal = FALSE;
		}
		else
		{
			DsMotion_TryLoadUsbCalibration(Device);
		}

		//
		// Send initial output report. The PS3 itself sends an all-zero,
		// 48-byte report over the control endpoint before it ever enables
		// streaming (Feature 0xF4) - mirrored here (instead of the historical
		// interrupt-OUT write) so devices without an OUT pipe get exactly the
		// same treatment a genuine pad already receives from a real console
		// (see issue #321 and docs/PS3_USB_STARTUP.md). Overlay the Sony
		// gyro cal byte when this pad's path uses hardware trim.
		// 
		if (pDevCtx->DeviceType != DsDeviceTypeThirdPartyHid)
		{
			UCHAR zeroOutputReport[48] = { 0 };

			DsMotion_OverlayCalByteOnUnified(
				pDevCtx,
				zeroOutputReport,
				ARRAYSIZE(zeroOutputReport)
			);

			if (!NT_SUCCESS(status = DsUsb_Ds3SendOutputReportControl(
				pDevCtx,
				zeroOutputReport,
				ARRAYSIZE(zeroOutputReport)
			)))
			{
				EventWriteFailedWithNTStatus(__FUNCTION__, L"Sending initial output report", status);
			}

			//
			// Soft-fail: must never abort PrepareHardware over this.
			// 
			status = STATUS_SUCCESS;
		}

	} while (FALSE);

	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);

	return status;
}

//
// Maximum number of attempts to get the DS3 to (re-)enter "streaming" mode
// during a D0Entry. See issue #311: on some systems the device is briefly
// unresponsive to control transfers immediately after a USB bus resume, and
// a single failure used to be fatal, leaving the device stuck in an error
// state until it was physically replugged.
// 
#define DS3_INIT_D0ENTRY_MAX_ATTEMPTS      5

//
// Delay between DsUsb_Ds3Init D0Entry retry attempts, in milliseconds.
// 
#define DS3_INIT_D0ENTRY_RETRY_DELAY_MS    100

NTSTATUS DsUsb_D0Entry(WDFDEVICE Device, WDF_POWER_DEVICE_STATE PreviousState)
{
	NTSTATUS status = STATUS_SUCCESS;
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	ULONG attempt;

	FuncEntryArguments(
		TRACE_DSUSB,
		"PreviousState=%d",
		PreviousState
	);

	//
	// A replug or bus resume starts with a clean output path. A stall that
	// belonged to the previous power session must not keep dropping reports
	// or leave the ControlApp warning up.
	//
	ThirdPartyHid_ResetOutputStall(pDevCtx);

	do
	{
		//
		// Instruct pad to (re-)enter streaming mode. This is done *before*
		// starting the continuous reader below, on purpose: it guarantees no
		// input report completion can race with this function while it may
		// still fail and unwind (see DMF_HandleValidate_IsOpened crash
		// reported in issue #311).
		//
		// Retried a bounded number of times because a single control
		// transfer failure right after a bus resume is not necessarily
		// terminal.
		// 
		if (pDevCtx->DeviceType == DsDeviceTypeThirdPartyHid)
		{
			//
			// Already streaming. Feature 0xF4 is a DS3 command and can
			// knock ShanWan firmware into its fallback identity.
			// 
			TraceInformation(
				TRACE_DSUSB,
				"Skipping DsUsb_Ds3Init for third-party HID (PreviousState=%d)",
				PreviousState
			);
		}
		else for (attempt = 1; attempt <= DS3_INIT_D0ENTRY_MAX_ATTEMPTS; attempt++)
		{
			TraceInformation(
				TRACE_DSUSB,
				"Attempting DsUsb_Ds3Init, attempt %d of %d (PreviousState=%d)",
				attempt,
				DS3_INIT_D0ENTRY_MAX_ATTEMPTS,
				PreviousState
			);

			status = DsUsb_Ds3Init(pDevCtx);

			if (NT_SUCCESS(status))
			{
				break;
			}

			if (attempt < DS3_INIT_D0ENTRY_MAX_ATTEMPTS)
			{
				TraceWarning(
					TRACE_DSUSB,
					"DsUsb_Ds3Init attempt %d of %d failed with %!STATUS!, retrying in %d ms",
					attempt,
					DS3_INIT_D0ENTRY_MAX_ATTEMPTS,
					status,
					DS3_INIT_D0ENTRY_RETRY_DELAY_MS
				);

				Sleep(DS3_INIT_D0ENTRY_RETRY_DELAY_MS);
			}
		}

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSUSB,
				"DsUsb_Ds3Init failed with %!STATUS! after %d attempts, giving up",
				status,
				DS3_INIT_D0ENTRY_MAX_ATTEMPTS
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DsUsb_Ds3Init", status);
			break;
		}

		//
		// The PS3 sends the current LED state over the control endpoint
		// (twice) right after enabling streaming (Feature 0xF4), before any
		// interrupt OUT traffic occurs. Mirror that single report here so a
		// device without a usable interrupt OUT pipe still gets an initial
		// LED state; genuine controllers accept this unconditionally too
		// (see issue #321 and docs/PS3_USB_STARTUP.md). Soft-fail only.
		// 
		if (pDevCtx->DeviceType != DsDeviceTypeThirdPartyHid)
		{
			PUCHAR outputReportBuffer = NULL;
			SIZE_T outputReportBufferLength = 0;

			Ds3_GetUnifiedOutputReportBuffer(
				pDevCtx,
				&outputReportBuffer,
				&outputReportBufferLength
			);

			(void)DsUsb_Ds3SendOutputReportControl(
				pDevCtx,
				outputReportBuffer,
				(ULONG)outputReportBufferLength
			);
		}

		//
		// Since continuous reader is configured for this interrupt-pipe, we must explicitly start
		// the I/O target to get the framework to post read requests.
		//
		if (!NT_SUCCESS(status = WdfIoTargetStart(
			WdfUsbTargetPipeGetIoTarget(pDevCtx->Connection.Usb.InterruptInPipe)
		)))
		{
			TraceError(
				TRACE_DSUSB,
				"Failed to start interrupt read pipe %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"Starting interrupt reader", status);
			break;
		}
	} while (FALSE);

	if (!NT_SUCCESS(status))
	{
		//
		// Only reachable after retries are exhausted or the reader failed to
		// start, i.e. an unrecoverable failure for this power-up attempt.
		// Rather than leaving the device stuck in a permanent error state
		// until the user physically replugs it (issue #311), ask WDF/PnP to
		// restart (re-enumerate) it instead. WDF caps the number of
		// consecutive automatic restart attempts on its own, so this cannot
		// loop forever.
		//
		// Follow-up: the traced HID-mode mismatch path (issue #374) showed
		// that on this UMDF stack WdfDeviceSetFailed(AttemptRestart) never
		// reaches usbhub3 and ends in Code 43. This resume path is left
		// unchanged here and most likely has the same defect.
		// 
		TraceWarning(
			TRACE_DSUSB,
			"Requesting a device restart after a failed power-up, PreviousState=%d, status=%!STATUS!",
			PreviousState,
			status
		);

		EventWriteRequestingDeviceRestartAfterResume(pDevCtx->DeviceAddressString);

		WdfDeviceSetFailed(Device, WdfDeviceFailedAttemptRestart);
	}

	FuncExit(TRACE_DSUSB, "status=%!STATUS!", status);

	return status;
}

NTSTATUS DsUdb_D0Exit(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

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

//
// Reads one feature report into a diagnostics report slot.
// 
static
VOID
DsUsb_DiagReadFeature(
	_In_ PDEVICE_CONTEXT DeviceContext,
	_In_ UCHAR ReportId,
	_Inout_ PDSHM_IPC_DIAG_REPORT Slot
)
{
	UCHAR buffer[DSHM_IPC_DIAG_REPORT_LEN];
	ULONG transferred = 0;

	RtlZeroMemory(buffer, sizeof(buffer));

	Slot->Id = ReportId;
	Slot->GetStatus = USB_SendControlRequest(
		DeviceContext,
		BmRequestDeviceToHost,
		BmRequestClass,
		GetReport,
		(USHORT)(0x0300 | ReportId),
		0,
		buffer,
		sizeof(buffer),
		&transferred
	);

	if (NT_SUCCESS(Slot->GetStatus))
	{
		if (transferred > sizeof(buffer))
		{
			transferred = sizeof(buffer);
		}

		Slot->Length = (USHORT)transferred;
		RtlCopyMemory(Slot->Data, buffer, transferred);
	}
}

//
// Selects a 0xEF EEPROM page. Returns the SET status.
// 
static
NTSTATUS
DsUsb_DiagSelectEepromPage(
	_In_ PDEVICE_CONTEXT DeviceContext,
	_In_ UCHAR Page
)
{
	UCHAR select[48] = { 0 };

	select[4] = 0x03;
	select[5] = 0x01;
	select[6] = Page;

	return USB_SendControlRequest(
		DeviceContext,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		Ds3FeatureEeprom,
		0,
		select,
		sizeof(select),
		NULL
	);
}

//
// Bounded, read-only diagnostic sweep of a USB DS3. Never fails as a whole once
// started: each item records its own NTSTATUS so partial data is still useful.
// The only stateful device access is the 0xEF page select, and page 0xA0 (the
// motion calibration page the driver itself relies on) is always re-selected
// before returning.
// 
_Use_decl_annotations_
NTSTATUS
DsUsb_CollectDiagnostics(
	PDEVICE_CONTEXT DeviceContext,
	PDSHM_IPC_MSG_COLLECT_DIAGNOSTICS_REPLY Reply
)
{
	FuncEntry(TRACE_DSUSB);

	const struct USB_DEVICE_CONTEXT* usb = &DeviceContext->Connection.Usb;
	const WDFUSBDEVICE usbDevice = usb->UsbDevice;
	ULONG i;

	Reply->Version = DSHM_IPC_DIAG_VERSION;
	Reply->NtStatus = STATUS_SUCCESS;

	const ULONGLONG deadlineMs = GetTickCount64() + DSHM_IPC_DIAG_SWEEP_BUDGET_MS;

	//
	// Descriptors
	// 
	RtlCopyMemory(
		Reply->DeviceDescriptor,
		&usb->UsbDeviceDescriptor,
		min(sizeof(usb->UsbDeviceDescriptor), sizeof(Reply->DeviceDescriptor))
	);

	{
		USHORT configLength = 0;
		NTSTATUS cfgStatus = WdfUsbTargetDeviceRetrieveConfigDescriptor(usbDevice, NULL, &configLength);

		if (cfgStatus == STATUS_BUFFER_TOO_SMALL && configLength > 0)
		{
			UCHAR configBuffer[DSHM_IPC_DIAG_CONFIG_DESCRIPTOR_MAX];

			//
			// The framework requires the exact size; fetch into a stack copy
			// only when it fits, otherwise record the overflow.
			// 
			if (configLength <= sizeof(configBuffer))
			{
				cfgStatus = WdfUsbTargetDeviceRetrieveConfigDescriptor(usbDevice, configBuffer, &configLength);

				if (NT_SUCCESS(cfgStatus))
				{
					Reply->ConfigDescriptorLength = configLength;
					RtlCopyMemory(Reply->ConfigDescriptor, configBuffer, configLength);
				}
			}
			else
			{
				cfgStatus = STATUS_BUFFER_OVERFLOW;
				Reply->ConfigDescriptorLength = 0;
			}
		}

		Reply->ConfigDescriptorStatus = cfgStatus;
	}

	//
	// Endpoints of the configured interface
	// 
	if (usb->UsbInterface)
	{
		const UCHAR pipeCount = WdfUsbInterfaceGetNumConfiguredPipes(usb->UsbInterface);

		for (i = 0; i < pipeCount && i < DSHM_IPC_DIAG_MAX_PIPES; i++)
		{
			WDF_USB_PIPE_INFORMATION info;
			WDF_USB_PIPE_INFORMATION_INIT(&info);

			const WDFUSBPIPE pipe = WdfUsbInterfaceGetConfiguredPipe(usb->UsbInterface, (UCHAR)i, &info);

			if (!pipe)
			{
				continue;
			}

			Reply->Pipes[Reply->PipeCount].PipeType = (UCHAR)info.PipeType;
			Reply->Pipes[Reply->PipeCount].EndpointAddress = info.EndpointAddress;
			Reply->Pipes[Reply->PipeCount].MaximumPacketSize = (USHORT)info.MaximumPacketSize;
			Reply->Pipes[Reply->PipeCount].Interval = info.Interval;
			Reply->PipeCount++;
		}
	}

	//
	// Manufacturer, product and serial number strings (US English)
	// 
	{
		const UCHAR indices[DSHM_IPC_DIAG_STRING_COUNT] = {
			usb->UsbDeviceDescriptor.iManufacturer,
			usb->UsbDeviceDescriptor.iProduct,
			usb->UsbDeviceDescriptor.iSerialNumber
		};

		for (i = 0; i < DSHM_IPC_DIAG_STRING_COUNT; i++)
		{
			PDSHM_IPC_DIAG_STRING slot = &Reply->Strings[i];

			slot->Index = indices[i];

			if (indices[i] == 0)
			{
				slot->Status = STATUS_NOT_FOUND;
				continue;
			}

			//
			// Leave room for a terminator; Length counts characters.
			// 
			USHORT cch = DSHM_IPC_DIAG_STRING_CCH - 1;

			slot->Status = WdfUsbTargetDeviceQueryString(
				usbDevice,
				NULL,
				NULL,
				slot->Text,
				&cch,
				indices[i],
				0x0409
			);

			if (NT_SUCCESS(slot->Status))
			{
				slot->Length = cch;
			}
			else
			{
				RtlZeroMemory(slot->Text, sizeof(slot->Text));
			}
		}
	}

	//
	// Plain feature reports
	// 
	{
		static const UCHAR featureIds[DSHM_IPC_DIAG_FEATURE_COUNT] = { 0x01, 0xF2, 0xF5, 0xF7, 0xF8 };

		for (i = 0; i < DSHM_IPC_DIAG_FEATURE_COUNT; i++)
		{
			if (GetTickCount64() >= deadlineMs)
			{
				Reply->Features[i].Id = featureIds[i];
				Reply->Features[i].GetStatus = STATUS_CANCELLED;
				continue;
			}

			DsUsb_DiagReadFeature(DeviceContext, featureIds[i], &Reply->Features[i]);
		}
	}

	//
	// EEPROM pages: SET page select, then GET. Abort remaining pages after
	// three total transfer failures or when the sweep budget expires so a
	// non-responsive clone cannot stall the dispatch thread past the SDK wait.
	// 
	{
		ULONG totalFailures = 0;

		for (i = 0; i < DSHM_IPC_DIAG_EEPROM_PAGE_COUNT; i++)
		{
			PDSHM_IPC_DIAG_REPORT slot = &Reply->EepromPages[i];
			const UCHAR page = (UCHAR)(i * 0x10);

			slot->Id = page;

			if (totalFailures >= 3 || GetTickCount64() >= deadlineMs)
			{
				slot->SetStatus = STATUS_CANCELLED;
				slot->GetStatus = STATUS_CANCELLED;
				continue;
			}

			slot->SetStatus = DsUsb_DiagSelectEepromPage(DeviceContext, page);

			if (!NT_SUCCESS(slot->SetStatus))
			{
				slot->GetStatus = STATUS_CANCELLED;
				totalFailures++;
				continue;
			}

			if (GetTickCount64() >= deadlineMs)
			{
				slot->GetStatus = STATUS_CANCELLED;
				continue;
			}

			DsUsb_DiagReadFeature(DeviceContext, Ds3FeatureEeprom & 0xFF, slot);
			slot->Id = page;

			if (!NT_SUCCESS(slot->GetStatus))
			{
				totalFailures++;
			}
		}
	}

	//
	// Always leave the controller on the calibration page the driver expects
	// 
	Reply->RestoreStatus = DsUsb_DiagSelectEepromPage(DeviceContext, DS_MOTION_EEPROM_PAGE);

	FuncExit(TRACE_DSUSB, "restore=%!STATUS!", Reply->RestoreStatus);

	return STATUS_SUCCESS;
}
