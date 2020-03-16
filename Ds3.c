#include "Driver.h"
#include "Ds3.tmh"


//
// Sends the "magic packet" to the DS3 so it starts its interrupt endpoint.
// 
NTSTATUS DsUsb_Ds3Init(PDEVICE_CONTEXT Context)
{
	// "Magic packet"
	UCHAR hidCommandEnable[DS3_HID_COMMAND_ENABLE_SIZE] =
	{
		0x42, 0x0C, 0x00, 0x00
	};

	return SendControlRequest(
		Context,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		Ds3FeatureStartDevice,
		0,
		hidCommandEnable,
		DS3_HID_COMMAND_ENABLE_SIZE
	);
}

VOID DsBth_Ds3Init(PDEVICE_CONTEXT Context)
{
	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DS3, "%!FUNC! Entry");

	BYTE hidCommandEnable[] = {
			0x53, 0xF4, 0x42, 0x03, 0x00, 0x00
	};

	NTSTATUS				status;
	PVOID					buffer = NULL;
	WDF_MEMORY_DESCRIPTOR	MemoryDescriptor;
	WDFMEMORY				MemoryHandle = NULL;
	
	status = WdfMemoryCreate(NULL,
		NonPagedPool,
		DS3_POOL_TAG,
		ARRAYSIZE(hidCommandEnable),
		&MemoryHandle,
		&buffer);
	if (!NT_SUCCESS(status)) {
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DS3,
			"WdfMemoryCreate failed with status %!STATUS!",
			status
		);
	}
	
	RtlCopyMemory(buffer, hidCommandEnable, ARRAYSIZE(hidCommandEnable));

	WDF_MEMORY_DESCRIPTOR_INIT_HANDLE(&MemoryDescriptor,
		MemoryHandle,
		NULL);

	status = WdfIoTargetSendIoctlSynchronously(
		Context->Connection.Bth.BthIoTarget,
		NULL,
		IOCTL_BTHPS3_HID_CONTROL_WRITE,
		&MemoryDescriptor,
		NULL,
		NULL,
		NULL
	);

	WdfObjectDelete(MemoryHandle);

	if (!NT_SUCCESS(status)) {
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DS3,
			"WdfIoTargetSendInternalIoctlSynchronously failed with status %!STATUS!",
			status
		);
	}

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DS3, "%!FUNC! Exit");
}
