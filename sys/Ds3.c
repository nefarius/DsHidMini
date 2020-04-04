#include "Driver.h"
#include "Ds3.tmh"
#include <bluetoothapis.h>


//
// Default Output Report for LED & Rumble state changes (USB)
// 
const UCHAR G_Ds3UsbHidOutputReport[] = {
	0x00, 0xFF, 0x00, 0xFF, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00,
	0xFF, 0x27, 0x10, 0x00, 0x32,
	0xFF, 0x27, 0x10, 0x00, 0x32,
	0xFF, 0x27, 0x10, 0x00, 0x32,
	0xFF, 0x27, 0x10, 0x00, 0x32,
	0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
};

//
// Default Output Report for LED & Rumble state changes (Bluetooth)
// 
const UCHAR G_Ds3BthHidOutputReport[] = {
	0x52, /* HID BT Set_report (0x50) | Report Type (Output 0x02)*/
	0x01, /* Report ID */
	0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00,
	0xFF, 0x27, 0x10, 0x00, 0x32,
	0xFF, 0x27, 0x10, 0x00, 0x32,
	0xFF, 0x27, 0x10, 0x00, 0x32,
	0xFF, 0x27, 0x10, 0x00, 0x32,
	0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
};

//
// Sends the "magic packet" to the DS3 so it starts its interrupt endpoint.
// 
NTSTATUS DsUsb_Ds3Init(PDEVICE_CONTEXT Context)
{
	// 
	// "Magic packet"
	// 
	UCHAR hidCommandEnable[] = {
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
		ARRAYSIZE(hidCommandEnable)
	);
}

//
// Auto-pair this device to first found host radio.
// 
NTSTATUS DsUsb_Ds3PairToFirstRadio(PDEVICE_CONTEXT Context)
{
	NTSTATUS status = STATUS_SUCCESS;
	HANDLE hRadio = NULL;
	HBLUETOOTH_RADIO_FIND hFind = NULL;
	BLUETOOTH_FIND_RADIO_PARAMS params;
	BLUETOOTH_RADIO_INFO info;
	DWORD ret;

	params.dwSize = sizeof(BLUETOOTH_FIND_RADIO_PARAMS);
	info.dwSize = sizeof(BLUETOOTH_RADIO_INFO);

	//
	// Grab first (active) radio
	// 
	hFind = BluetoothFindFirstRadio(
		&params,
		&hRadio
	);

	if (!hFind)
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DS3,
			"BluetoothFindFirstRadio failed: 0x%X", GetLastError());
		goto Exit;
	}

	//
	// Get radio info (address)
	// 
	ret = BluetoothGetRadioInfo(
		hRadio,
		&info
	);

	if (ERROR_SUCCESS != ret)
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DS3,
			"BluetoothGetRadioInfo failed: 0x%X", ret);
		goto Exit;
	}

	//
	// Align address to expected format/order
	// 
	REVERSE_BYTE_ARRAY(&info.address.rgBytes[0], sizeof(BD_ADDR));
	
	TraceEvents(TRACE_LEVEL_INFORMATION,
		TRACE_DS3,
		"Updating host address from %02X:%02X:%02X:%02X:%02X:%02X to %02X:%02X:%02X:%02X:%02X:%02X",
		Context->HostAddress.Address[0],
		Context->HostAddress.Address[1],
		Context->HostAddress.Address[2],
		Context->HostAddress.Address[3],
		Context->HostAddress.Address[4],
		Context->HostAddress.Address[5],
		info.address.rgBytes[0],
		info.address.rgBytes[1],
		info.address.rgBytes[2],
		info.address.rgBytes[3],
		info.address.rgBytes[4],
		info.address.rgBytes[5]
	);

	UCHAR controlBuffer[SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH];
	RtlZeroMemory(controlBuffer, SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH);

	RtlCopyMemory(&controlBuffer[2], &info.address, sizeof(BD_ADDR));

	//
	// Submit new host address
	// 
	status = SendControlRequest(
		Context,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		Ds3FeatureHostAddress,
		0,
		controlBuffer,
		SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH);

	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DS3,
			"Setting host address failed with %!STATUS!", status);
		goto Exit;
	}

	//
	// Update in device context after success
	// 
	RtlCopyMemory(&Context->HostAddress, &info.address, sizeof(BD_ADDR));

Exit:

	if (hRadio)
		CloseHandle(hRadio);
	if (hFind)
		CloseHandle(hFind);

	return status;
}

//
// Send magic packet over BTH
// 
VOID DsBth_Ds3Init(PDEVICE_CONTEXT Context)
{
	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DS3, "%!FUNC! Entry");

	// 
	// "Magic packet"
	// 
	BYTE hidCommandEnable[] = {
		0x53, 0xF4, 0x42, 0x03, 0x00, 0x00
	};

	NTSTATUS				status;
	PVOID					buffer = NULL;
	WDF_MEMORY_DESCRIPTOR	MemoryDescriptor;
	WDFMEMORY				MemoryHandle = NULL;

	status = WdfMemoryCreate(NULL,
		NonPagedPoolNx,
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
