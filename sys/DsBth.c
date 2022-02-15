#include "Driver.h"
#include "DsBth.tmh"
#include <bluetoothapis.h>
#include <bthioctl.h>


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
		WdfDeviceGetIoTarget(WdfObjectContextGetObject(Context)),
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
// Called once 1 second after power-up
// 
_Use_decl_annotations_
VOID
DsBth_EvtControlWriteTimerFunc(
	WDFTIMER  Timer
)
{
	NTSTATUS status;
	PDEVICE_CONTEXT pDevCtx;

	FuncEntry(TRACE_DSBTH);

	pDevCtx = DeviceGetContext(WdfTimerGetParentObject(Timer));
	
	if (pDevCtx->BatteryStatus == DsBatteryStatusNone)
	{
		DS3_SET_LED(pDevCtx, DS3_LED_OFF);
	}
	else
	{
		switch (pDevCtx->BatteryStatus)
		{
		case DsBatteryStatusCharged:
		case DsBatteryStatusFull:
			DS3_SET_LED(pDevCtx, DS3_LED_4);
			break;
		case DsBatteryStatusHigh:
			DS3_SET_LED(pDevCtx, DS3_LED_3);
			break;
		case DsBatteryStatusMedium:
			DS3_SET_LED(pDevCtx, DS3_LED_2);
			break;
		case DsBatteryStatusLow:
		case DsBatteryStatusDying:
			DS3_SET_LED(pDevCtx, DS3_LED_1);
			DS3_SET_LED_DURATION(pDevCtx, 0, 0xFF, 15, 127, 127);
			break;
		default:
			break;
		}
	}
		
	DS3_SET_SMALL_RUMBLE_DURATION(pDevCtx, 0xFE);
	DS3_SET_SMALL_RUMBLE_STRENGTH(pDevCtx, 0x00);
	DS3_SET_LARGE_RUMBLE_DURATION(pDevCtx, 0xFE);
	DS3_SET_LARGE_RUMBLE_STRENGTH(pDevCtx, 0x00);

	status = Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverHighPriority);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"Ds_SendOutputReport failed with status %!STATUS!",
			status
		);
	}
	
	//
	// Start consuming input packets
	// 
	status = DMF_DefaultTarget_StreamStart(pDevCtx->Connection.Bth.HidInterrupt.InputStreamerModule);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"DMF_DefaultTarget_StreamStart failed with status %!STATUS!",
			status
		);
	}
	
	FuncExitNoReturn(TRACE_DSBTH);
}

//
// Invoked once the Bluetooth disconnect event got fired
// 
VOID CALLBACK
DsBth_DisconnectEventCallback(
	_In_ PVOID   lpParameter,
	_In_ BOOLEAN TimerOrWaitFired
)
{
	NTSTATUS status;
	PDEVICE_CONTEXT pDevCtx = (PDEVICE_CONTEXT)lpParameter;
	UNREFERENCED_PARAMETER(TimerOrWaitFired);

	FuncEntry(TRACE_DSBTH);
	
	UnregisterWait(pDevCtx->ConfigurationDirectoryWatcherWaitHandle);
	CloseHandle(pDevCtx->ConfigurationDirectoryWatcherEvent);

	status = DsBth_SendDisconnectRequest(pDevCtx);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"DsBth_SendDisconnectRequest failed with status %!STATUS!",
			status
		);
	}

	FuncExitNoReturn(TRACE_DSBTH);
}

NTSTATUS DsBth_SelfManagedIoInit(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DSBTH);
	
	//
	// Send magic packet, starts input report sending
	// 
	status = DsBth_Ds3Init(pDevCtx);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSBTH,
			"DsBth_Ds3Init failed with status %!STATUS!",
			status
		);
	}
	
	//
	// Send initial output report
	// 
	//status = Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverHighPriority);

	//
	// Send preset output report (delayed)
	// Required for compatibility with some SIXAXIS models
	// 
	WdfTimerStart(
		pDevCtx->Connection.Bth.Timers.HidOutputReport,
		WDF_REL_TIMEOUT_IN_SEC(1)
	);

	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);

	return status;
}

NTSTATUS DsBth_SelfManagedIoSuspend(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DSBTH);

	WdfTimerStop(pDevCtx->Connection.Bth.Timers.HidOutputReport, FALSE);

	DMF_DefaultTarget_StreamStop(pDevCtx->Connection.Bth.HidInterrupt.InputStreamerModule);
	DMF_DefaultTarget_StreamStop(pDevCtx->Connection.Bth.HidControl.OutputWriterModule);

	//
	// Instruct disconnect to start PDO removal procedure
	// 
	status = DsBth_SendDisconnectRequest(pDevCtx);

	if (!NT_SUCCESS(status))
	{
		TraceVerbose(
			TRACE_DSBTH,
			"DsBth_SendDisconnectRequest failed with status %!STATUS!",
			status
		);
	}

	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);

	return status;
}

NTSTATUS DsBth_D0Entry(WDFDEVICE Device, WDF_POWER_DEVICE_STATE PreviousState)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DSBTH);

	if (PreviousState == WdfPowerDeviceD3Final)
	{
		DMF_DefaultTarget_Get(
			pDevCtx->Connection.Bth.HidInterrupt.InputStreamerModule,
			&pDevCtx->Connection.Bth.HidInterrupt.InputStreamerIoTarget
		);
		DMF_DefaultTarget_Get(
			pDevCtx->Connection.Bth.HidControl.OutputWriterModule,
			&pDevCtx->Connection.Bth.HidControl.OutputWriterIoTarget
		);
	}
	else
	{
		// Targets are started by default.
		//
		WdfIoTargetStart(pDevCtx->Connection.Bth.HidInterrupt.InputStreamerIoTarget);
		WdfIoTargetStart(pDevCtx->Connection.Bth.HidControl.OutputWriterIoTarget);
	}
	
	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);

	return status;
}
