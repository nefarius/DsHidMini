#pragma once

#define DS_INPUT_REPORT_METRICS_VERSION      1
#define DS_INPUT_REPORT_METRICS_PERIOD_MS    1000

typedef struct _DEVICE_CONTEXT DEVICE_CONTEXT, *PDEVICE_CONTEXT;

typedef struct _DS_INPUT_REPORT_METRICS_STATE
{
	//
	// Protects the window counters and last-arrival QPC. Taken at
	// DISPATCH_LEVEL on the USB continuous-reader path.
	//
	WDFSPINLOCK Lock;

	//
	// 1 Hz publisher. Created at PassiveLevel so it can take IpcLock.
	// UMDF rejects periodic passive timers, so this is a one-shot that
	// re-arms itself while Running is set.
	//
	WDFTIMER PublishTimer;

	//
	// TRUE while D0 publication is armed. Cleared before WdfTimerStop so
	// a racing callback cannot restart the timer after teardown.
	//
	BOOLEAN Running;

	LARGE_INTEGER WindowStartQpc;

	LARGE_INTEGER LastArrivalQpc;

	UINT64 WindowIntervalSumQpc;

	ULONG WindowReportCount;

	ULONG WindowIntervalCount;

} DS_INPUT_REPORT_METRICS_STATE, *PDS_INPUT_REPORT_METRICS_STATE;

#include <pshpack1.h>
typedef struct _IPC_INPUT_REPORT_METRICS_MESSAGE
{
	UINT32 SlotIndex;
	volatile LONG SequenceNumber;
	UINT16 Version;
	UINT16 Reserved0;
	UINT32 ReportRateHz;
	UINT32 AverageIntervalUs;
	UINT64 TimestampQpc;
} IPC_INPUT_REPORT_METRICS_MESSAGE, *PIPC_INPUT_REPORT_METRICS_MESSAGE;
#include <poppack.h>

C_ASSERT(sizeof(IPC_INPUT_REPORT_METRICS_MESSAGE) == 28);
C_ASSERT((FIELD_OFFSET(IPC_INPUT_REPORT_METRICS_MESSAGE, SequenceNumber) % sizeof(LONG)) == 0);
C_ASSERT((sizeof(IPC_INPUT_REPORT_METRICS_MESSAGE) % sizeof(LONG)) == 0);

_IRQL_requires_max_(PASSIVE_LEVEL)
NTSTATUS
DsInputReportMetrics_Create(
	_In_ WDFDEVICE Device
);

_IRQL_requires_max_(PASSIVE_LEVEL)
VOID
DsInputReportMetrics_D0Entry(
	_In_ PDEVICE_CONTEXT Context
);

_IRQL_requires_max_(PASSIVE_LEVEL)
VOID
DsInputReportMetrics_Stop(
	_In_ PDEVICE_CONTEXT Context,
	_In_ BOOLEAN Wait
);

_IRQL_requires_max_(DISPATCH_LEVEL)
VOID
DsInputReportMetrics_NoteArrival(
	_In_ PDEVICE_CONTEXT Context
);

_IRQL_requires_max_(PASSIVE_LEVEL)
VOID
DsInputReportMetrics_PublishOrClearIpcSnapshot(
	_In_ PDEVICE_CONTEXT Context,
	_In_ BOOLEAN Clear
);
