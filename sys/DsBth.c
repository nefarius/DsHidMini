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

	if (!NT_SUCCESS(status = DsBth_SendDisconnectRequest(pDevCtx)))
	{
		TraceError(
			TRACE_DSBTH,
			"DsBth_SendDisconnectRequest failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"DsBth_SendDisconnectRequest", status);
	}

	FuncExitNoReturn(TRACE_DSBTH);
}

//
// Power-up tasks on Bluetooth
// 
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

//
// Power-up tasks on Bluetooth
// 
NTSTATUS DsBth_SelfManagedIoInit(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DSBTH);
	
	//
	// Send delayed initialization packets
	// Required for compatibility with some SIXAXIS models
	// 
	WdfTimerStart(
		pDevCtx->Connection.Bth.Timers.StartupDelay,
		WDF_REL_TIMEOUT_IN_SEC(1)
	);

	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);

	return status;
}

//
// Power-down tasks on Bluetooth
// 
NTSTATUS DsBth_SelfManagedIoSuspend(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DSBTH);

	WdfTimerStop(pDevCtx->Connection.Bth.Timers.StartupDelay, FALSE);

	DMF_DefaultTarget_StreamStop(pDevCtx->Connection.Bth.HidInterrupt.InputStreamerModule);
	DMF_DefaultTarget_StreamStop(pDevCtx->Connection.Bth.HidControl.OutputWriterModule);

	//
	// Instruct disconnect to start PDO removal procedure
	// 
	if (!NT_SUCCESS(status = DsBth_SendDisconnectRequest(pDevCtx)))
	{
		TraceVerbose(
			TRACE_DSBTH,
			"DsBth_SendDisconnectRequest failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"DsBth_SendDisconnectRequest", status);
	}

	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);

	return status;
}
