#include "Driver.h"
#include "OutputReport.tmh"


//
// Enqueues current output report buffer to get sent to device.
//
NTSTATUS
DSHM_SendOutputReport(
	PDEVICE_CONTEXT Context,
	DS_OUTPUT_REPORT_SOURCE Source
)
{
	NTSTATUS status;
	PUCHAR sourceBuffer, sendBuffer;
	size_t sourceBufferLength;
	PDS_OUTPUT_REPORT_CONTEXT sendContext;
	const PDS_DRIVER_CONFIGURATION pConfig = &Context->Configuration;

	FuncEntry(TRACE_DSHIDMINIDRV);

	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

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
		// Override LED pattern
		// 
		if (pConfig->LEDSettings.Mode == DsLEDModeCustomPattern)
		{
			DS3_SET_LED_FLAGS(Context, pConfig->LEDSettings.CustomPatterns.LEDFlags);

			DS3_SET_LED_DURATION(
				Context,
				0,
				pConfig->LEDSettings.CustomPatterns.Player1.TotalDuration,
				pConfig->LEDSettings.CustomPatterns.Player1.BasePortionDuration,
				pConfig->LEDSettings.CustomPatterns.Player1.OffPortionMultiplier,
				pConfig->LEDSettings.CustomPatterns.Player1.OnPortionMultiplier
			);
			DS3_SET_LED_DURATION(
				Context,
				1,
				pConfig->LEDSettings.CustomPatterns.Player2.TotalDuration,
				pConfig->LEDSettings.CustomPatterns.Player2.BasePortionDuration,
				pConfig->LEDSettings.CustomPatterns.Player2.OffPortionMultiplier,
				pConfig->LEDSettings.CustomPatterns.Player2.OnPortionMultiplier
			);
			DS3_SET_LED_DURATION(
				Context,
				2,
				pConfig->LEDSettings.CustomPatterns.Player3.TotalDuration,
				pConfig->LEDSettings.CustomPatterns.Player3.BasePortionDuration,
				pConfig->LEDSettings.CustomPatterns.Player3.OffPortionMultiplier,
				pConfig->LEDSettings.CustomPatterns.Player3.OnPortionMultiplier
			);
			DS3_SET_LED_DURATION(
				Context,
				3,
				pConfig->LEDSettings.CustomPatterns.Player4.TotalDuration,
				pConfig->LEDSettings.CustomPatterns.Player4.BasePortionDuration,
				pConfig->LEDSettings.CustomPatterns.Player4.OffPortionMultiplier,
				pConfig->LEDSettings.CustomPatterns.Player4.OnPortionMultiplier
			);
		}

		//
		// Get full report (including IDs etc.)
		//
		DS3_GET_RAW_OUTPUT_REPORT_BUFFER(
			Context,
			&sourceBuffer,
			&sourceBufferLength
		);

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

	WdfWaitLockRelease(Context->OutputReport.Lock);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}

//
// Callback invoked when new output report packet is due to being processed
// 
_IRQL_requires_max_(PASSIVE_LEVEL)
_IRQL_requires_same_
ThreadedBufferQueue_BufferDisposition
DSHM_EvtExecuteOutputPacketReceived(
	_In_ DMFMODULE DmfModule,
	_In_ UCHAR* ClientWorkBuffer,
	_In_ ULONG ClientWorkBufferSize,
	_In_ VOID* ClientWorkBufferContext,
	_Out_ NTSTATUS* NtStatus
)
{
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

	FuncEntry(TRACE_DSHIDMINIDRV);

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

		status = USB_WriteInterruptOutSync(
			pDevCtx,
			&memoryDesc
		);

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

		status = DMF_DefaultTarget_SendSynchronously(
			pDevCtx->Connection.Bth.HidControl.OutputWriterModule,
			ClientWorkBuffer,
			bufferSize,
			NULL,
			0,
			ContinuousRequestTarget_RequestType_Ioctl,
			IOCTL_BTHPS3_HID_CONTROL_WRITE,
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
void
DSHM_OutputReportDelayTimerElapsed(
	WDFTIMER Timer
)
{
	const WDFDEVICE device = WdfTimerGetParentObject(Timer);
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(device);
	const PUCHAR sourceBuffer = pDevCtx->OutputReport.Cache.PendingClientBuffer;
	const PDS_OUTPUT_REPORT_CONTEXT pRepCtx = pDevCtx->OutputReport.Cache.PendingClientBufferContext;
	PUCHAR targetBuffer;
	PDS_OUTPUT_REPORT_CONTEXT targetBufferContext;

	FuncEntry(TRACE_DSHIDMINIDRV);

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
