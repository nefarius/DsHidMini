#include "Driver.h"
#include "DsInputReportMetrics.tmh"

EVT_WDF_TIMER DsInputReportMetrics_EvtPublishTimer;

static VOID
DsInputReportMetrics_ResetWindow(
	_Inout_ PDS_INPUT_REPORT_METRICS_STATE Metrics,
	_In_ BOOLEAN ResetLastArrival
)
{
	QueryPerformanceCounter(&Metrics->WindowStartQpc);
	Metrics->WindowIntervalSumQpc = 0;
	Metrics->WindowReportCount = 0;
	Metrics->WindowIntervalCount = 0;

	if (ResetLastArrival)
	{
		Metrics->LastArrivalQpc.QuadPart = 0;
	}
}

static VOID
DsInputReportMetrics_AssignVersionProperty(
	_In_ WDFDEVICE Device
)
{
	WDF_DEVICE_PROPERTY_DATA propertyData;
	const UINT32 version = DS_INPUT_REPORT_METRICS_VERSION;
	NTSTATUS status;

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_InputReportMetricsVersion);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	status = WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_UINT32,
		sizeof(version),
		(PVOID)&version
	);
	if (!NT_SUCCESS(status))
	{
		TraceWarning(
			TRACE_IPC,
			"WdfDeviceAssignProperty(DEVPKEY_DsHidMini_RO_InputReportMetricsVersion) failed with %!STATUS!",
			status
		);
	}
}

static VOID
DsInputReportMetrics_PublishValues(
	_In_ PDEVICE_CONTEXT Context,
	_In_ UINT32 ReportRateHz,
	_In_ UINT32 AverageIntervalUs
)
{
	const WDFDRIVER driver = WdfGetDriver();
	const PDSHM_DRIVER_CONTEXT pDrvCtx = DriverGetContext(driver);
	PIPC_INPUT_REPORT_METRICS_MESSAGE slot;
	size_t offset;
	LARGE_INTEGER now;

	if (pDrvCtx->IPC.SharedRegions.InputMetrics.Buffer == NULL || Context->SlotIndex < 1)
	{
		return;
	}

	offset = sizeof(IPC_INPUT_REPORT_METRICS_MESSAGE) * (Context->SlotIndex - 1);
	if (offset + sizeof(IPC_INPUT_REPORT_METRICS_MESSAGE) > pDrvCtx->IPC.SharedRegions.InputMetrics.BufferSize)
	{
		return;
	}

	slot = (PIPC_INPUT_REPORT_METRICS_MESSAGE)(pDrvCtx->IPC.SharedRegions.InputMetrics.Buffer + offset);
	QueryPerformanceCounter(&now);

	InterlockedIncrement(&slot->SequenceNumber);
	slot->SlotIndex = Context->SlotIndex;
	slot->Version = DS_INPUT_REPORT_METRICS_VERSION;
	slot->Reserved0 = 0;
	slot->ReportRateHz = ReportRateHz;
	slot->AverageIntervalUs = AverageIntervalUs;
	slot->TimestampQpc = (UINT64)now.QuadPart;
	InterlockedIncrement(&slot->SequenceNumber);
}

static VOID
DsInputReportMetrics_ComputeAndPublish(
	_In_ PDEVICE_CONTEXT Context
)
{
	PDS_INPUT_REPORT_METRICS_STATE metrics = &Context->InputReportMetrics;
	ULONG reportCount;
	ULONG intervalCount;
	UINT64 intervalSumQpc;
	LARGE_INTEGER windowStart;
	LARGE_INTEGER now;
	LARGE_INTEGER freq;
	UINT32 rateHz = 0;
	UINT32 averageIntervalUs = 0;

	QueryPerformanceCounter(&now);
	QueryPerformanceFrequency(&freq);

	WdfSpinLockAcquire(metrics->Lock);
	{
		reportCount = metrics->WindowReportCount;
		intervalCount = metrics->WindowIntervalCount;
		intervalSumQpc = metrics->WindowIntervalSumQpc;
		windowStart = metrics->WindowStartQpc;
		DsInputReportMetrics_ResetWindow(metrics, FALSE);
	}
	WdfSpinLockRelease(metrics->Lock);

	if (freq.QuadPart > 0 &&
		windowStart.QuadPart > 0 &&
		now.QuadPart > windowStart.QuadPart &&
		reportCount > 0)
	{
		const UINT64 elapsedQpc = (UINT64)(now.QuadPart - windowStart.QuadPart);

		rateHz = (UINT32)(((UINT64)reportCount * (UINT64)freq.QuadPart) / elapsedQpc);
	}

	if (freq.QuadPart > 0 && intervalCount > 0 && intervalSumQpc > 0)
	{
		averageIntervalUs = (UINT32)((intervalSumQpc * 1000000ULL) /
			((UINT64)freq.QuadPart * (UINT64)intervalCount));
	}

	WdfWaitLockAcquire(DriverGetContext(WdfGetDriver())->IpcLock, NULL);
	{
		DsInputReportMetrics_PublishValues(Context, rateHz, averageIntervalUs);
	}
	WdfWaitLockRelease(DriverGetContext(WdfGetDriver())->IpcLock);

	TraceVerbose(
		TRACE_IPC,
		"Input report metrics: %u Hz, %u us avg interval (reports=%lu intervals=%lu)",
		rateHz,
		averageIntervalUs,
		reportCount,
		intervalCount
	);
}

_Use_decl_annotations_
VOID
DsInputReportMetrics_EvtPublishTimer(
	WDFTIMER Timer
)
{
	const WDFDEVICE device = WdfTimerGetParentObject(Timer);
	const PDEVICE_CONTEXT context = DeviceGetContext(device);

	DsInputReportMetrics_ComputeAndPublish(context);

	WdfSpinLockAcquire(context->InputReportMetrics.Lock);
	{
		if (context->InputReportMetrics.Running)
		{
			WdfTimerStart(
				Timer,
				WDF_REL_TIMEOUT_IN_MS(DS_INPUT_REPORT_METRICS_PERIOD_MS)
			);
		}
	}
	WdfSpinLockRelease(context->InputReportMetrics.Lock);
}

_Use_decl_annotations_
NTSTATUS
DsInputReportMetrics_Create(
	WDFDEVICE Device
)
{
	const PDEVICE_CONTEXT context = DeviceGetContext(Device);
	WDF_OBJECT_ATTRIBUTES attributes;
	WDF_TIMER_CONFIG timerCfg;
	NTSTATUS status;

	WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
	attributes.ParentObject = Device;

	status = WdfSpinLockCreate(&attributes, &context->InputReportMetrics.Lock);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DEVICE,
			"WdfSpinLockCreate (InputReportMetrics) failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfSpinLockCreate (InputReportMetrics)", status);
		return status;
	}

	WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
	attributes.ParentObject = Device;
	attributes.ExecutionLevel = WdfExecutionLevelPassive;

	//
	// One-shot: WDF does not allow periodic timers at PassiveLevel, which
	// is the level UMDF callbacks (and this publisher, which takes IpcLock)
	// run at. EvtPublishTimer re-arms while Running remains set.
	//
	WDF_TIMER_CONFIG_INIT(
		&timerCfg,
		DsInputReportMetrics_EvtPublishTimer
	);

	status = WdfTimerCreate(
		&timerCfg,
		&attributes,
		&context->InputReportMetrics.PublishTimer
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DEVICE,
			"WdfTimerCreate (InputReportMetrics) failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"WdfTimerCreate (InputReportMetrics)", status);
		return status;
	}

	DsInputReportMetrics_AssignVersionProperty(Device);

	return STATUS_SUCCESS;
}

_Use_decl_annotations_
VOID
DsInputReportMetrics_D0Entry(
	PDEVICE_CONTEXT Context
)
{
	if (Context->InputReportMetrics.Lock == NULL ||
		Context->InputReportMetrics.PublishTimer == NULL)
	{
		return;
	}

	WdfSpinLockAcquire(Context->InputReportMetrics.Lock);
	{
		DsInputReportMetrics_ResetWindow(&Context->InputReportMetrics, TRUE);
		Context->InputReportMetrics.Running = TRUE;
	}
	WdfSpinLockRelease(Context->InputReportMetrics.Lock);

	WdfWaitLockAcquire(DriverGetContext(WdfGetDriver())->IpcLock, NULL);
	{
		DsInputReportMetrics_PublishValues(Context, 0, 0);
	}
	WdfWaitLockRelease(DriverGetContext(WdfGetDriver())->IpcLock);

	WdfTimerStart(
		Context->InputReportMetrics.PublishTimer,
		WDF_REL_TIMEOUT_IN_MS(DS_INPUT_REPORT_METRICS_PERIOD_MS)
	);
}

_Use_decl_annotations_
VOID
DsInputReportMetrics_Stop(
	PDEVICE_CONTEXT Context,
	BOOLEAN Wait
)
{
	if (Context->InputReportMetrics.Lock)
	{
		WdfSpinLockAcquire(Context->InputReportMetrics.Lock);
		{
			Context->InputReportMetrics.Running = FALSE;
			DsInputReportMetrics_ResetWindow(&Context->InputReportMetrics, TRUE);
		}
		WdfSpinLockRelease(Context->InputReportMetrics.Lock);
	}

	if (Context->InputReportMetrics.PublishTimer)
	{
		WdfTimerStop(Context->InputReportMetrics.PublishTimer, Wait);
	}

	WdfWaitLockAcquire(DriverGetContext(WdfGetDriver())->IpcLock, NULL);
	{
		DsInputReportMetrics_PublishValues(Context, 0, 0);
	}
	WdfWaitLockRelease(DriverGetContext(WdfGetDriver())->IpcLock);
}

_Use_decl_annotations_
VOID
DsInputReportMetrics_NoteArrival(
	PDEVICE_CONTEXT Context
)
{
	LARGE_INTEGER now;
	PDS_INPUT_REPORT_METRICS_STATE metrics = &Context->InputReportMetrics;

	if (metrics->Lock == NULL)
	{
		return;
	}

	QueryPerformanceCounter(&now);

	WdfSpinLockAcquire(metrics->Lock);
	{
		if (metrics->LastArrivalQpc.QuadPart != 0)
		{
			metrics->WindowIntervalSumQpc += (UINT64)(now.QuadPart - metrics->LastArrivalQpc.QuadPart);
			metrics->WindowIntervalCount++;
		}

		metrics->LastArrivalQpc = now;
		metrics->WindowReportCount++;
	}
	WdfSpinLockRelease(metrics->Lock);
}

_Use_decl_annotations_
VOID
DsInputReportMetrics_PublishOrClearIpcSnapshot(
	PDEVICE_CONTEXT Context,
	BOOLEAN Clear
)
{
	if (Clear)
	{
		const WDFDRIVER driver = WdfGetDriver();
		const PDSHM_DRIVER_CONTEXT pDrvCtx = DriverGetContext(driver);
		PIPC_INPUT_REPORT_METRICS_MESSAGE slot;
		size_t offset;

		if (pDrvCtx->IPC.SharedRegions.InputMetrics.Buffer == NULL || Context->SlotIndex < 1)
		{
			return;
		}

		offset = sizeof(IPC_INPUT_REPORT_METRICS_MESSAGE) * (Context->SlotIndex - 1);
		if (offset + sizeof(IPC_INPUT_REPORT_METRICS_MESSAGE) > pDrvCtx->IPC.SharedRegions.InputMetrics.BufferSize)
		{
			return;
		}

		slot = (PIPC_INPUT_REPORT_METRICS_MESSAGE)(pDrvCtx->IPC.SharedRegions.InputMetrics.Buffer + offset);

		InterlockedIncrement(&slot->SequenceNumber);
		slot->SlotIndex = 0;
		slot->Version = 0;
		slot->Reserved0 = 0;
		slot->ReportRateHz = 0;
		slot->AverageIntervalUs = 0;
		slot->TimestampQpc = 0;
		InterlockedIncrement(&slot->SequenceNumber);
		return;
	}

	DsInputReportMetrics_PublishValues(Context, 0, 0);
}
