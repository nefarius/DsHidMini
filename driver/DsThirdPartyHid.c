#include "Driver.h"
#include "DsMotion.h"
#include "DsThirdPartyHid.tmh"


//
// Sticks rest at 0x7F. DS3 consumers treat 0x80 as center, so shift every
// value except 0xFF up by one count.
// 
static
UCHAR
ThirdPartyHid_BiasAxis(
	_In_ UCHAR Value
)
{
	return Value == 0xFF ? 0xFF : (UCHAR)(Value + 1);
}

static
VOID
ThirdPartyHid_ApplyHat(
	_Inout_ PDS3_RAW_INPUT_REPORT Report,
	_In_ UCHAR Hat
)
{
	switch (Hat & 0x0F)
	{
	case 0:
		Report->Buttons.Individual.Up = 1;
		break;
	case 1:
		Report->Buttons.Individual.Up = 1;
		Report->Buttons.Individual.Right = 1;
		break;
	case 2:
		Report->Buttons.Individual.Right = 1;
		break;
	case 3:
		Report->Buttons.Individual.Right = 1;
		Report->Buttons.Individual.Down = 1;
		break;
	case 4:
		Report->Buttons.Individual.Down = 1;
		break;
	case 5:
		Report->Buttons.Individual.Down = 1;
		Report->Buttons.Individual.Left = 1;
		break;
	case 6:
		Report->Buttons.Individual.Left = 1;
		break;
	case 7:
		Report->Buttons.Individual.Left = 1;
		Report->Buttons.Individual.Up = 1;
		break;
	default:
		break;
	}
}

//
// 27-byte ShanWan HID input (no report id on the wire) into the DS3 report
// every HID mode already consumes. Face-button order on this adapter is
// Triangle, Circle, Cross, Square: bit 0 is Triangle, not Square.
// 
VOID
ThirdPartyHid_TranslateInput(
	_In_reads_(Length) const UCHAR* Raw,
	_In_ size_t Length,
	_Out_ PDS3_RAW_INPUT_REPORT Report
)
{
	UCHAR face;
	UCHAR system;

	RtlZeroMemory(Report, sizeof(*Report));
	Report->ReportId = 0x01;

	if (Raw == NULL || Length < 3)
	{
		return;
	}

	face = Raw[0];
	Report->Buttons.Individual.Triangle = (face >> 0) & 0x01;
	Report->Buttons.Individual.Circle = (face >> 1) & 0x01;
	Report->Buttons.Individual.Cross = (face >> 2) & 0x01;
	Report->Buttons.Individual.Square = (face >> 3) & 0x01;
	Report->Buttons.Individual.L1 = (face >> 4) & 0x01;
	Report->Buttons.Individual.R1 = (face >> 5) & 0x01;
	Report->Buttons.Individual.L2 = (face >> 6) & 0x01;
	Report->Buttons.Individual.R2 = (face >> 7) & 0x01;

	//
	// Bits 5-7 are constant padding and read as 1 while idle.
	// 
	system = Raw[1] & 0x1F;
	Report->Buttons.Individual.Select = (system >> 0) & 0x01;
	Report->Buttons.Individual.Start = (system >> 1) & 0x01;
	Report->Buttons.Individual.L3 = (system >> 2) & 0x01;
	Report->Buttons.Individual.R3 = (system >> 3) & 0x01;
	Report->Buttons.Individual.PS = (system >> 4) & 0x01;

	ThirdPartyHid_ApplyHat(Report, Raw[2]);

	if (Length >= 7)
	{
		Report->LeftThumbX = ThirdPartyHid_BiasAxis(Raw[3]);
		Report->LeftThumbY = ThirdPartyHid_BiasAxis(Raw[4]);
		Report->RightThumbX = ThirdPartyHid_BiasAxis(Raw[5]);
		Report->RightThumbY = ThirdPartyHid_BiasAxis(Raw[6]);
	}

	//
	// Vendor bytes 0x20-0x2B, idle 0x00. Order used by this descriptor
	// family: Right, Left, Up, Down, Triangle, Circle, Cross, Square,
	// L1, R1, L2, R2.
	// 
	if (Length >= THIRD_PARTY_HID_INPUT_REPORT_MINIMUM)
	{
		Report->Pressure.Values.Right = Raw[7];
		Report->Pressure.Values.Left = Raw[8];
		Report->Pressure.Values.Up = Raw[9];
		Report->Pressure.Values.Down = Raw[10];
		Report->Pressure.Values.Triangle = Raw[11];
		Report->Pressure.Values.Circle = Raw[12];
		Report->Pressure.Values.Cross = Raw[13];
		Report->Pressure.Values.Square = Raw[14];
		Report->Pressure.Values.L1 = Raw[15];
		Report->Pressure.Values.R1 = Raw[16];
		Report->Pressure.Values.L2 = Raw[17];
		Report->Pressure.Values.R2 = Raw[18];
	}

	Report->BatteryStatus = (UCHAR)DsBatteryStatusCharged;

	//
	// Big-endian, matching DS3_RAW_INPUT_REPORT. DsMotion_ProcessInputReport
	// byteswaps these back to the nominal rest sample (Z at +1 g).
	// 
	Report->AccelerometerX = _byteswap_ushort((USHORT)DS_MOTION_NOMINAL_ZERO);
	Report->AccelerometerY = _byteswap_ushort((USHORT)DS_MOTION_NOMINAL_ZERO);
	Report->AccelerometerZ = _byteswap_ushort(
		(USHORT)(DS_MOTION_NOMINAL_ZERO + DS_MOTION_ACCEL_GAIN)
	);
	Report->Gyroscope = _byteswap_ushort((USHORT)DS_MOTION_NOMINAL_ZERO);
}

//
// Snapshot the right-motor magnitude for the report about to be queued.
// Pass-through copies a DS3 report whose small-motor byte is only on/off
// and does not update LightCache, so an "on" write uses full strength.
// Every other source already stored its magnitude in LightCache before
// the send, and that value is what must travel with this queue entry.
// 
UCHAR
ThirdPartyHid_CaptureRightMotorStrength(
	_In_ DS_OUTPUT_REPORT_SOURCE Source,
	_In_reads_(Ds3ReportLength) const UCHAR* Ds3Report,
	_In_ size_t Ds3ReportLength,
	_In_ UCHAR LightCache
)
{
	if (Ds3Report == NULL || Ds3ReportLength <= 3 || Ds3Report[3] == 0)
	{
		return 0;
	}

	if (Source == Ds3OutputReportSourcePassThrough || LightCache == 0)
	{
		return THIRD_PARTY_HID_SMALL_MOTOR_ON_STRENGTH;
	}

	return LightCache;
}

//
// Large-motor byte 5 already holds the rescaled strength. RightMotorStrength
// is the snapshot taken when this report was queued.
// 
VOID
ThirdPartyHid_BuildOutputReport(
	_In_reads_(Ds3ReportLength) const UCHAR* Ds3Report,
	_In_ size_t Ds3ReportLength,
	_In_ UCHAR RightMotorStrength,
	_Out_writes_(THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH) PUCHAR Output
)
{
	UCHAR right = 0;
	UCHAR left = 0;

	RtlZeroMemory(Output, THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH);

	if (Ds3Report != NULL && Ds3ReportLength > 3 && Ds3Report[3] != 0)
	{
		right = RightMotorStrength;
	}

	if (Ds3Report != NULL && Ds3ReportLength > 5)
	{
		left = Ds3Report[5];
	}

	Output[0] = 0x02;
	Output[1] = 0x08;
	Output[2] = right;
	Output[3] = left;
	//
	// hid-shanwan always writes 0xFF here, including the stop report.
	// 
	Output[4] = 0xFF;
}

C_ASSERT(
	sizeof(((PDEVICE_CONTEXT)0)->Connection.Usb.LastAttemptedAdapterReport)
	== THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH
);

static VOID
ThirdPartyHid_PublishOutputReportStatus(
	_In_ PDEVICE_CONTEXT Context,
	_In_ NTSTATUS ReportStatus
)
{
	WDF_DEVICE_PROPERTY_DATA propertyData;
	const WDFDEVICE device = WdfObjectContextGetObject(Context);
	NTSTATUS status;

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_OutputReportStatus);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	status = WdfDeviceAssignProperty(
		device,
		&propertyData,
		DEVPROP_TYPE_NTSTATUS,
		sizeof(NTSTATUS),
		&ReportStatus
	);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSUSB,
			"Setting DEVPKEY_DsHidMini_RO_OutputReportStatus failed with status %!STATUS!",
			status
		);
	}
}

static
VOID
ThirdPartyHid_ArmOutputStallProbe(
	_In_ PDEVICE_CONTEXT Context
)
{
	if (Context->RumbleControlState.IsTearingDown
		|| Context->ConnectionType != DsDeviceConnectionTypeUsb
		|| Context->Connection.Usb.OutputStallProbeTimer == NULL
		|| (!Context->Connection.Usb.OutputStalled
			&& !Context->Connection.Usb.InitialOutputProbePending))
	{
		return;
	}

	WdfTimerStart(
		Context->Connection.Usb.OutputStallProbeTimer,
		WDF_REL_TIMEOUT_IN_MS(THIRD_PARTY_HID_STALL_PROBE_PERIOD_MS)
	);
}

VOID
ThirdPartyHid_StopOutputStallProbe(
	_In_ PDEVICE_CONTEXT Context,
	_In_ BOOLEAN Wait
)
{
	if (Context->ConnectionType != DsDeviceConnectionTypeUsb)
	{
		return;
	}

	Context->Connection.Usb.InitialOutputProbePending = FALSE;

	if (Context->Connection.Usb.OutputStallProbeTimer == NULL)
	{
		return;
	}

	WdfTimerStop(Context->Connection.Usb.OutputStallProbeTimer, Wait);
}

_Use_decl_annotations_
VOID
ThirdPartyHid_EvtOutputStallProbeTimerFunc(
	WDFTIMER Timer
)
{
	const PDEVICE_CONTEXT context = DeviceGetContext(WdfTimerGetParentObject(Timer));

	//
	// IsTearingDown is set before the timer is stopped on power-down.
	// ThirdPartyHid_ArmOutputStallProbe checks it again, so a failed
	// enqueue below cannot restart the timer after teardown.
	//
	if (context->RumbleControlState.IsTearingDown
		|| context->ConnectionType != DsDeviceConnectionTypeUsb
		|| (!context->Connection.Usb.OutputStalled
			&& !context->Connection.Usb.InitialOutputProbePending))
	{
		return;
	}

	//
	// The worker re-arms this timer when it finishes the send. A full
	// queue never gets that far, so arm it here or probing stops.
	// InitialOutputProbePending covers the power-up report, which has
	// not set OutputStalled yet.
	//
	if (!NT_SUCCESS(DSHM_SendOutputReport(
		context,
		Ds3OutputReportSourceDriverHighPriority)))
	{
		ThirdPartyHid_ArmOutputStallProbe(context);
	}
	else
	{
		context->Connection.Usb.InitialOutputProbePending = FALSE;
	}
}

VOID
ThirdPartyHid_ProbeOutputPath(
	_In_ PDEVICE_CONTEXT Context
)
{
	PUCHAR buffer;

	if (Context->DeviceType != DsDeviceTypeThirdPartyHid
		|| Context->ConnectionType != DsDeviceConnectionTypeUsb
		|| Context->RumbleControlState.IsTearingDown)
	{
		return;
	}

	//
	// The default USB template is already a stop report. Zero the motor
	// bytes anyway so a resume cannot replay rumble left over from the
	// previous power session. The adapter report is then
	// 02 08 00 00 FF 00 00 00.
	//
	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	buffer = (PUCHAR)WdfMemoryGetBuffer(Context->OutputReportMemory, NULL);
	DS3_USB_SET_SMALL_RUMBLE_STRENGTH(buffer, 0);
	DS3_USB_SET_LARGE_RUMBLE_STRENGTH(buffer, 0);
	Context->RumbleControlState.LightCache = 0;
	Context->RumbleControlState.HeavyCache = 0;

	if (NT_SUCCESS(DSHM_SendOutputReportUnlocked(
		Context,
		Ds3OutputReportSourceDriverHighPriority)))
	{
		Context->Connection.Usb.InitialOutputProbePending = FALSE;
	}
	else
	{
		//
		// OutputStalled stays clear until a send actually fails on the
		// wire. Remember this missed queue so the probe timer retries
		// it without treating every clear status as a stall.
		//
		Context->Connection.Usb.InitialOutputProbePending = TRUE;
		ThirdPartyHid_ArmOutputStallProbe(Context);
	}

	WdfWaitLockRelease(Context->OutputReport.Lock);
}

VOID
ThirdPartyHid_ResetOutputStall(
	_In_ PDEVICE_CONTEXT Context
)
{
	ThirdPartyHid_StopOutputStallProbe(Context, FALSE);

	Context->Connection.Usb.OutputStalled = FALSE;
	Context->Connection.Usb.OutputFetchFailureLogged = FALSE;
	Context->Connection.Usb.LastOutputAttemptTimestamp.QuadPart = 0;
	RtlZeroMemory(
		Context->Connection.Usb.LastAttemptedAdapterReport,
		sizeof(Context->Connection.Usb.LastAttemptedAdapterReport)
	);

	ThirdPartyHid_PublishOutputReportStatus(Context, STATUS_SUCCESS);
}

static BOOLEAN
ThirdPartyHid_StallProbeDue(
	_In_ PDEVICE_CONTEXT Context
)
{
	LARGE_INTEGER now;
	LARGE_INTEGER frequency;
	LONGLONG elapsedMs;

	if (Context->Connection.Usb.LastOutputAttemptTimestamp.QuadPart == 0)
	{
		return TRUE;
	}

	QueryPerformanceCounter(&now);
	QueryPerformanceFrequency(&frequency);

	if (frequency.QuadPart == 0)
	{
		return TRUE;
	}

	elapsedMs =
		((now.QuadPart - Context->Connection.Usb.LastOutputAttemptTimestamp.QuadPart) * 1000)
		/ frequency.QuadPart;

	return elapsedMs >= THIRD_PARTY_HID_STALL_PROBE_PERIOD_MS;
}

static BOOLEAN
ThirdPartyHid_InterruptOutTimedOut(
	_In_ NTSTATUS Status
)
{
	//
	// WDF completes a send-option timeout as STATUS_IO_TIMEOUT. STATUS_CANCELLED
	// is a different completion (power-down or an explicit cancel) and must
	// not restart the pipe.
	//
	return Status == STATUS_IO_TIMEOUT;
}

//
// A timed-out write cancels the URB and can leave this pipe's I/O target
// unable to accept another transfer. Stop, abort, reset, then start is the
// documented USB pipe recovery order. The interrupt IN reader uses a
// different pipe and is left running.
//
static NTSTATUS
ThirdPartyHid_RecoverInterruptOutPipe(
	_In_ WDFUSBPIPE Pipe
)
{
	const WDFIOTARGET ioTarget = WdfUsbTargetPipeGetIoTarget(Pipe);
	NTSTATUS status = STATUS_SUCCESS;
	NTSTATUS stepStatus;

	//
	// UMDF's WdfIoTargetStop returns void. It cancels outstanding I/O
	// on this pipe only.
	//
	WdfIoTargetStop(ioTarget, WdfIoTargetCancelSentIo);

	stepStatus = WdfUsbTargetPipeAbortSynchronously(Pipe, NULL, NULL);
	if (!NT_SUCCESS(stepStatus))
	{
		TraceWarning(
			TRACE_DSUSB,
			"WdfUsbTargetPipeAbortSynchronously failed with status %!STATUS!",
			stepStatus
		);
		status = stepStatus;
	}

	stepStatus = WdfUsbTargetPipeResetSynchronously(Pipe, NULL, NULL);
	if (!NT_SUCCESS(stepStatus))
	{
		TraceWarning(
			TRACE_DSUSB,
			"WdfUsbTargetPipeResetSynchronously failed with status %!STATUS!",
			stepStatus
		);
		if (NT_SUCCESS(status))
		{
			status = stepStatus;
		}
	}

	//
	// Start even when abort or reset failed. Stop already succeeded, and
	// leaving the target stopped would fail every later write.
	//
	stepStatus = WdfIoTargetStart(ioTarget);
	if (!NT_SUCCESS(stepStatus))
	{
		TraceWarning(
			TRACE_DSUSB,
			"WdfIoTargetStart on interrupt OUT failed with status %!STATUS!",
			stepStatus
		);
		if (NT_SUCCESS(status))
		{
			status = stepStatus;
		}
	}

	return status;
}

NTSTATUS
ThirdPartyHid_SendOutputReport(
	_In_ PDEVICE_CONTEXT Context,
	_In_reads_(THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH) PUCHAR Output
)
{
	NTSTATUS status;
	WDF_MEMORY_DESCRIPTOR memoryDesc;

	//
	// While the adapter is NAKing, the keep-alive resends the same 8 bytes
	// every 200 ms. Repeating that on the bus only blocks the worker. One
	// identical probe per second is enough to notice when a controller links.
	//
	if (Context->Connection.Usb.OutputStalled
		&& RtlEqualMemory(
			Output,
			Context->Connection.Usb.LastAttemptedAdapterReport,
			THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH
		)
		&& !ThirdPartyHid_StallProbeDue(Context))
	{
		//
		// A probe that arrives a few milliseconds early must not be the
		// last one. Re-arm so the next attempt still happens.
		//
		ThirdPartyHid_ArmOutputStallProbe(Context);
		return STATUS_SUCCESS;
	}

	//
	// Auto resolves to InterruptOut while the pipe exists. ControlEndpoint
	// is the config override used to compare the two transports.
	// 
	if (Context->Connection.Usb.OutputTransport == DsUsbOutputReportTransportControlEndpoint)
	{
		status = USB_SendControlRequest(
			Context,
			BmRequestHostToDevice,
			BmRequestClass,
			SetReport,
			THIRD_PARTY_HID_OUTPUT_REPORT_VALUE,
			0,
			Output,
			THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH,
			NULL
		);
	}
	else
	{
		WDF_MEMORY_DESCRIPTOR_INIT_BUFFER(
			&memoryDesc,
			Output,
			THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH
		);

		status = USB_WriteInterruptOutSync(
			Context,
			&memoryDesc,
			THIRD_PARTY_HID_OUTPUT_TIMEOUT_MS
		);

		//
		// Recover the pipe, then send this report once more. A controller
		// that linked while the first URB was being cancelled can ACK the
		// retry. A second timeout still takes the stall path below.
		//
		if (ThirdPartyHid_InterruptOutTimedOut(status)
			&& NT_SUCCESS(ThirdPartyHid_RecoverInterruptOutPipe(
				Context->Connection.Usb.InterruptOutPipe)))
		{
			status = USB_WriteInterruptOutSync(
				Context,
				&memoryDesc,
				THIRD_PARTY_HID_OUTPUT_TIMEOUT_MS
			);
		}
	}

	RtlCopyMemory(
		Context->Connection.Usb.LastAttemptedAdapterReport,
		Output,
		THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH
	);
	QueryPerformanceCounter(&Context->Connection.Usb.LastOutputAttemptTimestamp);

	if (!NT_SUCCESS(status))
	{
		if (!Context->Connection.Usb.OutputStalled)
		{
			Context->Connection.Usb.OutputStalled = TRUE;

			TraceWarning(
				TRACE_DSUSB,
				"ShanWan output stalled with status %!STATUS!",
				status
			);

			EventWriteFailedWithNTStatus(__FUNCTION__, L"ShanWan rumble", status);
			ThirdPartyHid_PublishOutputReportStatus(Context, status);
		}
		else
		{
			TraceVerbose(
				TRACE_DSUSB,
				"ShanWan output still stalled (%!STATUS!)",
				status
			);
		}

		//
		// Both the first failure and every later one. The keep-alive stops
		// once the motors are quiet, so this timer is what notices a
		// controller linking afterwards.
		//
		ThirdPartyHid_ArmOutputStallProbe(Context);
	}
	else if (Context->Connection.Usb.OutputStalled)
	{
		Context->Connection.Usb.OutputStalled = FALSE;
		Context->Connection.Usb.OutputFetchFailureLogged = FALSE;
		ThirdPartyHid_StopOutputStallProbe(Context, FALSE);

		TraceInformation(
			TRACE_DSUSB,
			"ShanWan output recovered"
		);

		ThirdPartyHid_PublishOutputReportStatus(Context, STATUS_SUCCESS);
	}

	return status;
}
