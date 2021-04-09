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
	NTSTATUS				status;
	PDEVICE_CONTEXT			pDeviceContext;
	WDF_OBJECT_ATTRIBUTES	attribs;
	WDF_TIMER_CONFIG		timerCfg;

	PAGED_CODE();

	FuncEntry(TRACE_DSBTH);

	pDeviceContext = DeviceGetContext(Device);


#pragma region Timers

	//
	// Output Report Delay
	// 

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;
	
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
	PDEVICE_CONTEXT pDevCtx;

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

	DMF_DefaultTarget_StreamStart(pDevCtx->Connection.Bth.HidInterrupt.InputStreamerModule);
	
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
