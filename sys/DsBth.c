#include "Driver.h"
#include "DsBth.tmh"
#include <bluetoothapis.h>
#include <bthioctl.h>


//
// Initialize objects required for BTH communication
// 
NTSTATUS DsHidMini_BthConnectionContextInit(
	WDFDEVICE Device
)
{
	NTSTATUS				status = STATUS_SUCCESS;
	PDEVICE_CONTEXT			pDeviceContext;
	WDF_OBJECT_ATTRIBUTES	attribs;
	PUCHAR					outBuffer;
	WDF_TIMER_CONFIG		timerCfg;

	PAGED_CODE();

	FuncEntry(TRACE_DSBTH);

	pDeviceContext = DeviceGetContext(Device);

#pragma region HID Interrupt Read

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;

	status = WdfRequestCreate(
		&attribs,
		pDeviceContext->Connection.Bth.BthIoTarget,
		&pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfRequestCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest;

	status = WdfMemoryCreate(
		&attribs,
		NonPagedPoolNx,
		DS3_POOL_TAG,
		BTHPS3_SIXAXIS_HID_INPUT_REPORT_SIZE,
		&pDeviceContext->Connection.Bth.HidInterrupt.ReadMemory,
		NULL
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfMemoryCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

#pragma endregion

#pragma region HID Control Write

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;

	status = WdfRequestCreate(
		&attribs,
		pDeviceContext->Connection.Bth.BthIoTarget,
		&pDeviceContext->Connection.Bth.HidControl.WriteRequest
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfRequestCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = pDeviceContext->Connection.Bth.HidControl.WriteRequest;

	status = WdfMemoryCreate(
		&attribs,
		NonPagedPoolNx,
		DS3_POOL_TAG,
		BTHPS3_SIXAXIS_HID_OUTPUT_REPORT_SIZE,
		&pDeviceContext->Connection.Bth.HidControl.WriteMemory,
		(PVOID)&outBuffer
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfMemoryCreate failed with status %!STATUS!",
			status
		);
		return status;
	}
	else
	{
		//
		// Initialize output report
		// 
		RtlCopyMemory(outBuffer, G_Ds3BthHidOutputReport, DS3_BTH_HID_OUTPUT_REPORT_SIZE);

		//
		// Turn flashing LEDs off
		// 
		DS3_BTH_SET_LED(outBuffer, DS3_LED_OFF);
	}

#pragma endregion

#pragma region HID Control Read

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;

	status = WdfRequestCreate(
		&attribs,
		pDeviceContext->Connection.Bth.BthIoTarget,
		&pDeviceContext->Connection.Bth.HidControl.ReadRequest
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfRequestCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = pDeviceContext->Connection.Bth.HidControl.ReadRequest;

	status = WdfMemoryCreate(
		&attribs,
		NonPagedPoolNx,
		DS3_POOL_TAG,
		0x0A, // TODO: introduce const
		&pDeviceContext->Connection.Bth.HidControl.ReadMemory,
		NULL
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfMemoryCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

#pragma endregion

#pragma region Timers

	//
	// Control consume
	// 

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;

	WDF_TIMER_CONFIG_INIT(
		&timerCfg,
		DsBth_EvtControlReadTimerFunc
	);

	status = WdfTimerCreate(
		&timerCfg,
		&attribs,
		&pDeviceContext->Connection.Bth.Timers.HidControlConsume
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfTimerCreate (HidControlConsume) failed with status %!STATUS!",
			status
		);
		return status;
	}

	WdfTimerStart(
		pDeviceContext->Connection.Bth.Timers.HidControlConsume,
		WDF_REL_TIMEOUT_IN_MS(0x64) // TODO: introduce const
	);

	//
	// Output Report Delay
	// 

	WDF_TIMER_CONFIG_INIT(
		&timerCfg,
		DsBth_EvtControlWriteTimerFunc
	);

	status = WdfTimerCreate(
		&timerCfg,
		&attribs,
		&pDeviceContext->Connection.Bth.Timers.HidOutputReport
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfTimerCreate (HidOutputReport) failed with status %!STATUS!",
			status
		);
		return status;
	}

#pragma endregion

#pragma region Locks

	//
	// HidControl.WriteLock
	// 

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;

	status = WdfWaitLockCreate(
		&attribs,
		&pDeviceContext->Connection.Bth.HidControl.WriteLock
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfWaitLockCreate (HidControl.WriteLock) failed with status %!STATUS!",
			status
		);
		return status;
	}

#pragma endregion 

	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);

	return status;
}

//
// Send HID Control OUT Request
// 
NTSTATUS 
DsBth_SendHidControlWriteRequest(
	_In_ PDEVICE_CONTEXT Context,
	_In_ PWDF_MEMORY_DESCRIPTOR Memory
)
{
	NTSTATUS					status;
	WDF_REQUEST_REUSE_PARAMS    params;

	FuncEntry(TRACE_DSBTH);
	
	status = WdfIoTargetSendIoctlSynchronously(
		Context->Connection.Bth.BthIoTarget,
		Context->Connection.Bth.HidControl.WriteRequest,
		IOCTL_BTHPS3_HID_CONTROL_WRITE,
		Memory,
		NULL,
		NULL,
		NULL
	);
	if (!NT_SUCCESS(status)) {
		TraceError(
			TRACE_DSBTH,
			"WdfIoTargetSendInternalIoctlSynchronously failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_REQUEST_REUSE_PARAMS_INIT(
		&params,
		WDF_REQUEST_REUSE_NO_FLAGS,
		STATUS_SUCCESS
	);
	status = WdfRequestReuse(
		Context->Connection.Bth.HidControl.WriteRequest,
		&params
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfRequestReuse failed with status %!STATUS!",
			status
		);
	}

	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);

	return status;
}

//
// Send off async HID Control OUT request
// 
NTSTATUS DsBth_SendHidControlWriteRequestAsync(PDEVICE_CONTEXT Context)
{
	NTSTATUS status;

	FuncEntry(TRACE_DSBTH);
	
	if (!NT_SUCCESS(WdfWaitLockAcquire(Context->Connection.Bth.HidControl.WriteLock, 0)))
	{
		return STATUS_DEVICE_BUSY;
	}

	status = WdfIoTargetFormatRequestForIoctl(
		Context->Connection.Bth.BthIoTarget,
		Context->Connection.Bth.HidControl.WriteRequest,
		IOCTL_BTHPS3_HID_CONTROL_WRITE,
		Context->Connection.Bth.HidControl.WriteMemory,
		NULL,
		NULL,
		NULL
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfIoTargetFormatRequestForIoctl failed with status %!STATUS!",
			status
		);
	}

	WdfRequestSetCompletionRoutine(
		Context->Connection.Bth.HidControl.WriteRequest,
		DsBth_HidControlWriteRequestCompletionRoutine,
		Context
	);

	if (WdfRequestSend(
		Context->Connection.Bth.HidControl.WriteRequest,
		Context->Connection.Bth.BthIoTarget,
		WDF_NO_SEND_OPTIONS
	) == FALSE) {
		status = WdfRequestGetStatus(Context->Connection.Bth.HidControl.WriteRequest);
	}
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"WdfRequestSend failed with status %!STATUS!",
			status
		);
	}

	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);
	
	return status;
}

//
// Request device disconnect from host radio
// 
NTSTATUS DsBth_SendDisconnectRequest(PDEVICE_CONTEXT Context)
{
	NTSTATUS status;
	BLUETOOTH_ADDRESS address;
	WDF_MEMORY_DESCRIPTOR memDesc;
	UCHAR buffer[sizeof(BLUETOOTH_ADDRESS)];

	FuncEntry(TRACE_DSBTH);

	RtlZeroMemory(buffer, sizeof(BLUETOOTH_ADDRESS));
	RtlCopyMemory(buffer, &Context->DeviceAddress, sizeof(Context->DeviceAddress));

	address.ullLong = *(PULONGLONG)&buffer[0];

	WDF_MEMORY_DESCRIPTOR_INIT_BUFFER(
		&memDesc,
		&address,
		sizeof(BLUETOOTH_ADDRESS)
	);

	//
	// Send disconnect request
	// 
	status = WdfIoTargetSendIoctlSynchronously(
		Context->Connection.Bth.BthIoTarget,
		NULL, // use internal request object
		IOCTL_BTH_DISCONNECT_DEVICE,
		&memDesc, // holds address to disconnect
		NULL,
		NULL,
		NULL
	);

	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);
	
	return status;
}

//
// Periodic timer to consume pending memory on HID Control channel
// 
_Use_decl_annotations_
VOID
DsBth_EvtControlReadTimerFunc(
	WDFTIMER  Timer
)
{
	NTSTATUS					status;
	PDEVICE_CONTEXT				pDevCtx;
	WDF_MEMORY_DESCRIPTOR		memDesc;
	size_t						bufferLength = 0;
	WDF_REQUEST_REUSE_PARAMS    params;

	FuncEntry(TRACE_DSBTH);

	do
	{
		pDevCtx = DeviceGetContext(WdfTimerGetParentObject(Timer));

		WDF_MEMORY_DESCRIPTOR_INIT_HANDLE(
			&memDesc,
			pDevCtx->Connection.Bth.HidControl.ReadMemory,
			NULL
		);

		status = WdfIoTargetSendIoctlSynchronously(
			pDevCtx->Connection.Bth.BthIoTarget,
			pDevCtx->Connection.Bth.HidControl.ReadRequest,
			IOCTL_BTHPS3_HID_CONTROL_READ,
			NULL,
			&memDesc,
			NULL,
			&bufferLength
		);

		if (status == STATUS_DEVICE_NOT_CONNECTED || status == STATUS_CANCELLED)
		{
			break;
		}

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSBTH,
				"WdfIoTargetSendInternalIoctlSynchronously failed with status %!STATUS!",
				status
			);
		}

		TraceVerbose(TRACE_DSBTH,
		             "++ Control bytes consumed: %d",
		             (ULONG)bufferLength
		);

		WDF_REQUEST_REUSE_PARAMS_INIT(
			&params,
			WDF_REQUEST_REUSE_NO_FLAGS,
			STATUS_SUCCESS
		);
		status = WdfRequestReuse(
			pDevCtx->Connection.Bth.HidControl.ReadRequest,
			&params
		);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSBTH,
				"WdfRequestReuse failed with status %!STATUS!",
				status
			);
			break;
		}

		WdfTimerStart(
			pDevCtx->Connection.Bth.Timers.HidControlConsume,
			WDF_REL_TIMEOUT_IN_MS(0x0A)
		);
	}
	while (FALSE);

	FuncExitNoReturn(TRACE_DSBTH);
}

//
// Called once 1 second after power-up
// 
_Use_decl_annotations_
VOID
DsBth_EvtControlWriteTimerFunc(
	WDFTIMER  Timer
)
{
	PDEVICE_CONTEXT		pDevCtx;

	FuncEntry(TRACE_DSBTH);

	pDevCtx = DeviceGetContext(WdfTimerGetParentObject(Timer));
	
	if (pDevCtx->BatteryStatus == DsBatteryStatusNone)
	{
		DS3_SET_LED(pDevCtx, DS3_LED_1);
	}
	else
	{
		switch (pDevCtx->BatteryStatus)
		{
		case DsBatteryStatusCharged:
		case DsBatteryStatusFull:
		case DsBatteryStatusHigh:
			DS3_SET_LED(pDevCtx, DS3_LED_4);
			break;
		case DsBatteryStatusMedium:
			DS3_SET_LED(pDevCtx, DS3_LED_3);
			break;
		case DsBatteryStatusLow:
			DS3_SET_LED(pDevCtx, DS3_LED_2);
			break;
		case DsBatteryStatusDying:
			DS3_SET_LED(pDevCtx, DS3_LED_1);
			break;
		default:
			break;
		}
	}
		
	DS3_SET_SMALL_RUMBLE_DURATION(pDevCtx, 0xFE);
	DS3_SET_SMALL_RUMBLE_STRENGTH(pDevCtx, 0x00);
	DS3_SET_LARGE_RUMBLE_DURATION(pDevCtx, 0xFE);
	DS3_SET_LARGE_RUMBLE_STRENGTH(pDevCtx, 0x00);

	(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverHighPriority);

	FuncExitNoReturn(TRACE_DSBTH);
}

//
// Reuse HID Control OUT Request
// 
void DsBth_HidControlWriteRequestCompletionRoutine(
	WDFREQUEST Request,
	WDFIOTARGET Target,
	PWDF_REQUEST_COMPLETION_PARAMS Params,
	WDFCONTEXT Context
)
{
	NTSTATUS                    status;
	WDF_REQUEST_REUSE_PARAMS    params;
	PDEVICE_CONTEXT				pDevCtx = (PDEVICE_CONTEXT)Context;

	UNREFERENCED_PARAMETER(Target);
	UNREFERENCED_PARAMETER(Context);

	FuncEntry(TRACE_DSBTH);

	if (Params->IoStatus.Status != STATUS_DEVICE_NOT_CONNECTED)
	{
		WDF_REQUEST_REUSE_PARAMS_INIT(
			&params,
			WDF_REQUEST_REUSE_NO_FLAGS,
			Params->IoStatus.Status
		);
		status = WdfRequestReuse(Request, &params);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSBTH,
				"WdfRequestReuse failed with status %!STATUS!",
				status
			);
		}
	}

	WdfWaitLockRelease(pDevCtx->Connection.Bth.HidControl.WriteLock);

	FuncExitNoReturn(TRACE_DSBTH);
}

VOID CALLBACK
DsBth_DisconnectEventCallback(
	_In_ PVOID   lpParameter,
	_In_ BOOLEAN TimerOrWaitFired
)
{
	PDEVICE_CONTEXT pDevCtx = (PDEVICE_CONTEXT)lpParameter;
	UNREFERENCED_PARAMETER(TimerOrWaitFired);

	UnregisterWait(pDevCtx->ConfigurationReloadWaitHandle);
	CloseHandle(pDevCtx->ConfigurationReloadEvent);

	(void)DsBth_SendDisconnectRequest(pDevCtx);
}
