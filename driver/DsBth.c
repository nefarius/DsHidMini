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

	EventWriteWirelessDisconnectIoctlCompleted(Context->DeviceAddressString, status);

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
	const PDEVICE_CONTEXT pDevCtx = (PDEVICE_CONTEXT)lpParameter;
	UNREFERENCED_PARAMETER(TimerOrWaitFired);

	FuncEntry(TRACE_DSBTH);

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
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

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
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

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

static
BOOLEAN
DsBth_IsDisconnectStatus(
	_In_ NTSTATUS Status
)
{
	switch (Status)
	{
	case STATUS_DEVICE_NOT_CONNECTED:
	case STATUS_DEVICE_REMOVED:
	case STATUS_CANCELLED:
	case STATUS_DELETE_PENDING:
	case STATUS_NO_SUCH_DEVICE:
	case STATUS_INVALID_DEVICE_STATE:
		return TRUE;
	default:
		return FALSE;
	}
}

static
NTSTATUS
DsBth_HidControlWrite(
	_In_ PDEVICE_CONTEXT Context,
	_In_reads_bytes_(BufferLength) PVOID Buffer,
	_In_ size_t BufferLength,
	_In_ ULONG TimeoutMs
)
{
	NTSTATUS status;
	size_t bytesWritten = 0;

	if (Context == NULL ||
		Context->Connection.Bth.HidControl.OutputWriterModule == NULL ||
		Buffer == NULL ||
		BufferLength == 0)
	{
		return STATUS_INVALID_PARAMETER;
	}

	status = DMF_DefaultTarget_SendSynchronously(
		Context->Connection.Bth.HidControl.OutputWriterModule,
		Buffer,
		BufferLength,
		NULL,
		0,
		ContinuousRequestTarget_RequestType_Ioctl,
		IOCTL_BTHPS3_HID_CONTROL_WRITE,
		TimeoutMs,
		&bytesWritten
	);

	if (!NT_SUCCESS(status))
	{
		TraceWarning(
			TRACE_DSBTH,
			"HID control WRITE failed with %!STATUS! (%lu bytes)",
			status,
			(ULONG)bytesWritten
		);
	}

	return status;
}

static
NTSTATUS
DsBth_HidControlRead(
	_In_ PDEVICE_CONTEXT Context,
	_Out_writes_bytes_(BufferLength) PVOID Buffer,
	_In_ size_t BufferLength,
	_Out_ PULONG BytesRead,
	_In_ ULONG TimeoutMs
)
{
	NTSTATUS status;
	size_t bytesRead = 0;

	if (BytesRead != NULL)
	{
		*BytesRead = 0;
	}

	if (Context == NULL ||
		Context->Connection.Bth.HidControl.OutputWriterModule == NULL ||
		Buffer == NULL ||
		BufferLength == 0)
	{
		return STATUS_INVALID_PARAMETER;
	}

	status = DMF_DefaultTarget_SendSynchronously(
		Context->Connection.Bth.HidControl.OutputWriterModule,
		NULL,
		0,
		Buffer,
		BufferLength,
		ContinuousRequestTarget_RequestType_Ioctl,
		IOCTL_BTHPS3_HID_CONTROL_READ,
		TimeoutMs,
		&bytesRead
	);

	if (NT_SUCCESS(status) && BytesRead != NULL)
	{
		*BytesRead = (ULONG)bytesRead;
	}
	else if (!NT_SUCCESS(status) && !DsBth_IsDisconnectStatus(status))
	{
		TraceVerbose(
			TRACE_DSBTH,
			"HID control READ completed with %!STATUS! (%lu bytes)",
			status,
			(ULONG)bytesRead
		);
	}

	return status;
}

static
NTSTATUS
DsBth_DrainHandshake(
	_In_ PDEVICE_CONTEXT Context
)
{
	UCHAR handshake[8];
	ULONG transferred = 0;
	NTSTATUS status;

	RtlZeroMemory(handshake, sizeof(handshake));
	status = DsBth_HidControlRead(
		Context,
		handshake,
		sizeof(handshake),
		&transferred,
		DS_BTH_FEATURE_HANDSHAKE_TIMEOUT_MS
	);

	if (DsBth_IsDisconnectStatus(status))
	{
		return status;
	}

	if (!NT_SUCCESS(status))
	{
		//
		// Many pads never send a control-channel handshake. Timeout is
		// expected and must not fail the SET.
		//
		return STATUS_SUCCESS;
	}

	if (transferred == 1 && handshake[0] == DS3_BTH_HIDP_HANDSHAKE_SUCCESS)
	{
		TraceVerbose(
			TRACE_DSBTH,
			"HID control handshake SUCCESS after SET Feature"
		);
		return STATUS_SUCCESS;
	}

	TraceWarning(
		TRACE_DSBTH,
		"Unexpected HID control read after SET Feature (%lu bytes, first=0x%02X)",
		transferred,
		handshake[0]
	);
	DumpAsHex("BthFeatureHandshake", handshake, transferred);
	return STATUS_SUCCESS;
}

NTSTATUS
DsBth_HidControlSetFeature(
	_In_ PDEVICE_CONTEXT Context,
	_In_ UCHAR ReportId,
	_In_reads_bytes_opt_(PayloadLength) const UCHAR* Payload,
	_In_ ULONG PayloadLength
)
{
	UCHAR packet[2 + DS_BTH_FEATURE_MAX_PAYLOAD];
	NTSTATUS status;

	FuncEntry(TRACE_DSBTH);

	if (PayloadLength > DS_BTH_FEATURE_MAX_PAYLOAD)
	{
		status = STATUS_BUFFER_OVERFLOW;
		FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);
		return status;
	}

	if (PayloadLength > 0 && Payload == NULL)
	{
		status = STATUS_INVALID_PARAMETER;
		FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);
		return status;
	}

	RtlZeroMemory(packet, sizeof(packet));
	packet[0] = DS3_BTH_HID_FEATURE_SET_PREFIX;
	packet[1] = ReportId;
	if (PayloadLength > 0)
	{
		RtlCopyMemory(&packet[2], Payload, PayloadLength);
	}

	status = DsBth_HidControlWrite(
		Context,
		packet,
		2 + PayloadLength,
		DS_BTH_FEATURE_TIMEOUT_MS
	);

	if (!NT_SUCCESS(status))
	{
		FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);
		return status;
	}

	status = DsBth_DrainHandshake(Context);

	FuncExit(TRACE_DSBTH, "status=%!STATUS!", status);
	return status;
}
