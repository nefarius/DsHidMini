#include "Driver.h"
#include "OutputReport.tmh"


//
// Selects the Bluetooth HID channel and matching report prefix immediately
// before a send. Control (0x52 / CONTROL_WRITE) is the historical default;
// Interrupt (0xA2 / INTERRUPT_WRITE) is the PR 460 rumble candidate.
// The 0x53 Sixaxis init write stays on the control channel in Ds3.c.
// 
static
VOID
DSHM_BthPrepareOutputReport(
	_In_ const PDEVICE_CONTEXT DeviceContext,
	_Inout_updates_(BufferSize) PUCHAR Buffer,
	_In_ size_t BufferSize,
	_Out_ PULONG Ioctl
)
{
	const BOOLEAN useInterrupt =
		DeviceContext->Configuration.BluetoothOutputReportTransport ==
		DsBluetoothOutputReportTransportInterrupt;

	if (BufferSize > 0)
	{
		Buffer[0] = useInterrupt
			? DS3_BTH_HID_OUTPUT_REPORT_INTERRUPT_PREFIX
			: DS3_BTH_HID_OUTPUT_REPORT_CONTROL_PREFIX;
	}

	*Ioctl = useInterrupt
		? IOCTL_BTHPS3_HID_INTERRUPT_WRITE
		: IOCTL_BTHPS3_HID_CONTROL_WRITE;
}

//
// Enqueues current output report buffer to get sent to device. Takes
// Context->OutputReport.Lock; see DSHM_SendOutputReportUnlocked for callers
// that already hold it.
//
_Use_decl_annotations_
NTSTATUS
DSHM_SendOutputReport(
	_In_ const PDEVICE_CONTEXT Context,
	_In_ const DS_OUTPUT_REPORT_SOURCE Source
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	const NTSTATUS status = DSHM_SendOutputReportUnlocked(Context, Source);

	WdfWaitLockRelease(Context->OutputReport.Lock);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}

//
// Same as DSHM_SendOutputReport, but assumes Context->OutputReport.Lock is
// already held by the caller (see DsLed.c and DsBth.Timers.c). Applies the
// driver-owned custom LED pattern immediately before the report copy, so a
// concurrently issued rumble-only or HID application send can never drop it
// (issue #350) - unlike the previous unconditional re-apply, this now only
// happens while the driver is actually in charge of LEDs.
//
_Use_decl_annotations_
NTSTATUS
DSHM_SendOutputReportUnlocked(
	_In_ const PDEVICE_CONTEXT Context,
	_In_ const DS_OUTPUT_REPORT_SOURCE Source
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

	NTSTATUS status;
	PUCHAR sourceBuffer, sendBuffer;
	size_t sourceBufferLength;
	PDS_OUTPUT_REPORT_CONTEXT sendContext;
	const PDS_DRIVER_CONFIGURATION pConfig = &Context->Configuration;

	do
	{
		//
		// Grab new buffer to send
		//
		if (!NT_SUCCESS(status = DMF_ThreadedBufferQueue_Fetch(
			Context->OutputReport.Worker,
			(PVOID*)&sendBuffer,
			(PVOID*)&sendContext
		)))
		{
			TraceError(
				TRACE_DSHIDMINIDRV,
				"DMF_ThreadedBufferQueue_Fetch failed with status %!STATUS!",
				status
			);

			EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_ThreadedBufferQueue_Fetch", status);

			break;
		}

		//
		// Re-apply the driver-owned custom LED pattern immediately before
		// the copy below, so it survives sends triggered by rumble-only or
		// HID application writes that did not go through DsLed_Apply
		// (issue #350). Gated by DsLed_IsDriverInCharge so an application
		// or Automatic hand-off is never overridden here.
		// 
		if (pConfig->LEDSettings.Mode == DsLEDModeCustomPattern && DsLed_IsDriverInCharge(Context))
		{
			DsLed_ApplyCustomPatternLocked(Context);
		}

		//
		// Get full report (including IDs etc.)
		//
		Ds3_GetRawOutputReportBuffer(
			Context,
			&sourceBuffer,
			&sourceBufferLength
		);

		//
		// Navigation has no motors. Clear duration/strength at the send
		// boundary so HID-application and FFB rumble writes cannot leak
		// onto the wire while LED bytes stay intact (issue #48).
		// 
		if (Context->DeviceType == DsDeviceTypeNavigation)
		{
			if (Context->ConnectionType == DsDeviceConnectionTypeUsb
				&& sourceBufferLength > 5)
			{
				sourceBuffer[2] = 0x00;
				sourceBuffer[3] = 0x00;
				sourceBuffer[4] = 0x00;
				sourceBuffer[5] = 0x00;
			}
			else if (Context->ConnectionType == DsDeviceConnectionTypeBth
				&& sourceBufferLength > 6)
			{
				sourceBuffer[3] = 0x00;
				sourceBuffer[4] = 0x00;
				sourceBuffer[5] = 0x00;
				sourceBuffer[6] = 0x00;
			}
		}

		// 
		// Timestamp arrival
		//
		QueryPerformanceCounter(&sendContext->ReceivedTimestamp);
		// 
		// Real buffer length
		// 
		sendContext->BufferSize = sourceBufferLength;
		//
		// Store origin
		//
		sendContext->ReportSource = Source;

		//
		// Copy current report to buffer
		//
		RtlCopyMemory(sendBuffer, sourceBuffer, sourceBufferLength);

		//
		// Enqueue current report
		//
		DMF_ThreadedBufferQueue_Enqueue(
			Context->OutputReport.Worker,
			sendBuffer
		);

	} while (FALSE);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}

//
// Callback invoked when new output report packet is due to being processed
// 
_Use_decl_annotations_
ThreadedBufferQueue_BufferDisposition
DSHM_EvtExecuteOutputPacketReceived(
	_In_ DMFMODULE DmfModule,
	_In_ UCHAR* ClientWorkBuffer,
	_In_ ULONG ClientWorkBufferSize,
	_In_ VOID* ClientWorkBufferContext,
	_Out_ NTSTATUS* NtStatus
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

	ThreadedBufferQueue_BufferDisposition retval = ThreadedBufferQueue_BufferDisposition_WorkComplete;
	NTSTATUS status = STATUS_UNSUCCESSFUL;
	const WDFDEVICE device = DMF_ParentDeviceGet(DmfModule);
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(device);
	const PDS_OUTPUT_REPORT_CONTEXT pRepCtx = (PDS_OUTPUT_REPORT_CONTEXT)ClientWorkBufferContext;
	const size_t bufferSize = pRepCtx->BufferSize;

	WDF_MEMORY_DESCRIPTOR memoryDesc;
	LARGE_INTEGER freq;
	LONGLONG ms;
	ULONGLONG timeout;
	size_t bytesWritten;

	UNREFERENCED_PARAMETER(ClientWorkBufferSize);
	UNREFERENCED_PARAMETER(NtStatus);	

	QueryPerformanceFrequency(&freq);

	//
	// Last successful send timestamp
	// 
	LARGE_INTEGER* t1 = &pDevCtx->OutputReport.Cache.LastSentTimestamp;

	//
	// Current request received timestamp
	// 
	const LARGE_INTEGER* t2 = &pRepCtx->ReceivedTimestamp;

	WDF_MEMORY_DESCRIPTOR_INIT_BUFFER(
		&memoryDesc,
		ClientWorkBuffer,
		(ULONG)bufferSize
	);

	switch (pDevCtx->ConnectionType)
	{
#pragma region DsDeviceConnectionTypeUsb

	case DsDeviceConnectionTypeUsb:

		//
		// Devices without a usable interrupt OUT pipe (or explicitly
		// configured to do so, see UsbOutputReportTransport) get their
		// output reports over the control endpoint instead - the same
		// mechanism the PS3 itself falls back to for its very first report
		// (see issue #321).
		// 
		if (pDevCtx->Connection.Usb.OutputTransport == DsUsbOutputReportTransportControlEndpoint)
		{
			status = DsUsb_Ds3SendOutputReportControl(
				pDevCtx,
				ClientWorkBuffer + 1,
				(ULONG)(bufferSize - 1)
			);
		}
		else
		{
			status = USB_WriteInterruptOutSync(
				pDevCtx,
				&memoryDesc
			);
		}

		if (NT_SUCCESS(status))
		{
			RtlCopyMemory(
				pDevCtx->OutputReport.Cache.LastReport,
				ClientWorkBuffer,
				bufferSize
			);
		}

		break;

#pragma endregion

#pragma region DsDeviceConnectionTypeBth

	case DsDeviceConnectionTypeBth:

		//
		// Calculate delay, the smaller the more frequent packets are sent
		// 
		ms = (t2->QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"Time span since last packet was sent: %I64d ms",
			ms
		);

	//
	// Rate limit condition has been detected
	// 
		if (pRepCtx->ReportSource > Ds3OutputReportSourceDriverHighPriority
			&& pDevCtx->Configuration.IsOutputRateControlEnabled > 0
			&& ms < pDevCtx->Configuration.OutputRateControlPeriodMs)
		{
			timeout = pDevCtx->Configuration.OutputRateControlPeriodMs - ms;

			TraceVerbose(
				TRACE_DSHIDMINIDRV,
				"Rate control triggered, delaying buffer 0x%p for %I64u ms",
				ClientWorkBuffer,
				timeout
			);

			//
			// Protect, must not run in parallel
			// 
			WdfWaitLockAcquire(pDevCtx->OutputReport.Cache.Lock, NULL);
			{
				//
				// We're still in overload condition, drop previous, if any
				// 
				if (pDevCtx->OutputReport.Cache.PendingClientBuffer)
				{
					TraceVerbose(
						TRACE_DSHIDMINIDRV,
						"Rate control still engaged, replacing buffer 0x%p with 0x%p",
						pDevCtx->OutputReport.Cache.PendingClientBuffer,
						ClientWorkBuffer
					);

					DMF_ThreadedBufferQueue_WorkCompleted(
						pDevCtx->OutputReport.Worker,
						pDevCtx->OutputReport.Cache.PendingClientBuffer,
						STATUS_INVALID_DEVICE_REQUEST // Has no impact
					);
				}

				//
				// Overwrite after old one has been cancelled
				// 
				pDevCtx->OutputReport.Cache.PendingClientBuffer = ClientWorkBuffer;
				pDevCtx->OutputReport.Cache.PendingClientBufferContext = ClientWorkBufferContext;

				//
				// Kick off delay send timer callback
				// 
				if (!pDevCtx->OutputReport.Cache.IsScheduled)
				{
					pDevCtx->OutputReport.Cache.IsScheduled = WdfTimerStart(
						pDevCtx->OutputReport.Cache.SendDelayTimer,
						WDF_REL_TIMEOUT_IN_MS(timeout)
					);
				}
			}
			WdfWaitLockRelease(pDevCtx->OutputReport.Cache.Lock);

			status = STATUS_PENDING; // Has no impact, just for trace

			//
			// Keep buffer slot occupied
			// 
			retval = ThreadedBufferQueue_BufferDisposition_WorkPending;

			break;
		}

			ULONG bthOutputIoctl = IOCTL_BTHPS3_HID_CONTROL_WRITE;

			DSHM_BthPrepareOutputReport(
				pDevCtx,
				ClientWorkBuffer,
				bufferSize,
				&bthOutputIoctl
			);

			status = DMF_DefaultTarget_SendSynchronously(
				pDevCtx->Connection.Bth.HidControl.OutputWriterModule,
				ClientWorkBuffer,
				bufferSize,
				NULL,
				0,
				ContinuousRequestTarget_RequestType_Ioctl,
				bthOutputIoctl,
				0,
				&bytesWritten
			);

		if (NT_SUCCESS(status))
		{
			// 
			// Store last successful send
			// 

			QueryPerformanceCounter(t1);

			RtlCopyMemory(
				pDevCtx->OutputReport.Cache.LastReport,
				ClientWorkBuffer,
				bufferSize
			);
		}

		break;

#pragma endregion

	default:
		status = STATUS_INVALID_PARAMETER;
	}

	*NtStatus = status;

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return retval;
}

//
// Callback invoked after cache cooldown delay timer elapsed
// 
_Use_decl_annotations_
void
DSHM_OutputReportDelayTimerElapsed(
	WDFTIMER Timer
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

	const WDFDEVICE device = WdfTimerGetParentObject(Timer);
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(device);
	const PUCHAR sourceBuffer = pDevCtx->OutputReport.Cache.PendingClientBuffer;
	const PDS_OUTPUT_REPORT_CONTEXT pRepCtx = pDevCtx->OutputReport.Cache.PendingClientBufferContext;
	PUCHAR targetBuffer;
	PDS_OUTPUT_REPORT_CONTEXT targetBufferContext;	

	//
	// Protected region
	// 
	WdfWaitLockAcquire(pDevCtx->OutputReport.Cache.Lock, NULL);
	{
		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"Processing delayed buffer 0x%p",
			sourceBuffer
		);

		//
		// Re-queue last cached buffer with high priority
		// 

		const NTSTATUS status = DMF_ThreadedBufferQueue_Fetch(
			pDevCtx->OutputReport.Worker,
			(PVOID*)&targetBuffer,
			(PVOID*)&targetBufferContext
		);

		if (NT_SUCCESS(status))
		{
			RtlCopyMemory(targetBuffer, sourceBuffer, pRepCtx->BufferSize);

			targetBufferContext->BufferSize = pRepCtx->BufferSize;
			targetBufferContext->ReceivedTimestamp = pRepCtx->ReceivedTimestamp;

			//
			// Set to high priority, bypasses rate control for this buffer
			// 
			targetBufferContext->ReportSource = Ds3OutputReportSourceDriverHighPriority;

			DMF_ThreadedBufferQueue_Enqueue(
				pDevCtx->OutputReport.Worker,
				targetBuffer
			);
		}

		//
		// Mark original buffer as completed
		// 
		DMF_ThreadedBufferQueue_WorkCompleted(
			pDevCtx->OutputReport.Worker,
			pDevCtx->OutputReport.Cache.PendingClientBuffer,
			status
		);

		pDevCtx->OutputReport.Cache.PendingClientBuffer = NULL;

		pDevCtx->OutputReport.Cache.IsScheduled = FALSE;
	}
	WdfWaitLockRelease(pDevCtx->OutputReport.Cache.Lock);

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}
