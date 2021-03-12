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
// Sends the "magic packet" to the DS3 so it starts its interrupt endpoint.
// 
NTSTATUS DsUsb_Ds3Init(PDEVICE_CONTEXT Context)
{
	NTSTATUS status;

	FuncEntry(TRACE_DS3);
	
	// 
	// "Magic packet"
	// 
	UCHAR hidCommandEnable[] = {
		0x42, 0x0C, 0x00, 0x00
	};

	status = USB_SendControlRequest(
		Context,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		Ds3FeatureStartDevice,
		0,
		hidCommandEnable,
		ARRAYSIZE(hidCommandEnable)
	);

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);
	
	return status;
}

//
// Auto-pair this device to first found host radio.
// 
NTSTATUS DsUsb_Ds3PairToFirstRadio(PDEVICE_CONTEXT Context)
{
	NTSTATUS status = STATUS_UNSUCCESSFUL;
	HANDLE hRadio = NULL;
	HBLUETOOTH_RADIO_FIND hFind = NULL;
	BLUETOOTH_FIND_RADIO_PARAMS params;
	BLUETOOTH_RADIO_INFO info;
	DWORD ret;
	WDF_DEVICE_PROPERTY_DATA propertyData;
	
	FuncEntry(TRACE_DS3);
	
	params.dwSize = sizeof(BLUETOOTH_FIND_RADIO_PARAMS);
	info.dwSize = sizeof(BLUETOOTH_RADIO_INFO);

	do {
		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_LastPairingStatus);
		propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
		propertyData.Lcid = LOCALE_NEUTRAL;

		(void)WdfDeviceAssignProperty(
			(WDFDEVICE)Context,
			&propertyData,
			DEVPROP_TYPE_NTSTATUS,
			sizeof(NTSTATUS),
			&status
		);		
		
		//
		// Grab first (active) radio
		// 
		hFind = BluetoothFindFirstRadio(
			&params,
			&hRadio
		);

		if (!hFind)
		{
			TraceError(
				TRACE_DS3,
				"BluetoothFindFirstRadio failed: 0x%X", GetLastError());
			break;
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
			TraceError(
				TRACE_DS3,
				"BluetoothGetRadioInfo failed: 0x%X", ret);
			break;
		}

		//
		// Align address to expected format/order
		// 
		REVERSE_BYTE_ARRAY(&info.address.rgBytes[0], sizeof(BD_ADDR));

		TraceInformation(
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
		status = USB_SendControlRequest(
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
			TraceError(
				TRACE_DS3,
				"Setting host address failed with %!STATUS!", status);
			break;
		}

		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_LastPairingStatus);
		propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
		propertyData.Lcid = LOCALE_NEUTRAL;

		(void)WdfDeviceAssignProperty(
			(WDFDEVICE)Context,
			&propertyData,
			DEVPROP_TYPE_NTSTATUS,
			sizeof(NTSTATUS),
			&status
		);

		//
		// Update in device context after success
		// 
		RtlCopyMemory(&Context->HostAddress, &info.address, sizeof(BD_ADDR));
	} while (FALSE);

	if (hRadio)
		CloseHandle(hRadio);
	if (hFind)
		CloseHandle(hFind);

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);
	
	return status;
}

//
// Send magic packet over BTH
// 
VOID DsBth_Ds3Init(PDEVICE_CONTEXT Context)
{
	FuncEntry(TRACE_DS3);

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
		TraceError(
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
		TraceError(
			TRACE_DS3,
			"WdfIoTargetSendInternalIoctlSynchronously failed with status %!STATUS!",
			status
		);
	}

	FuncExitNoReturn(TRACE_DS3);
}

VOID DS3_GET_UNIFIED_OUTPUT_REPORT_BUFFER(
	PDEVICE_CONTEXT Context,
	UCHAR** Buffer,
	PSIZE_T BufferLength
)
{
	switch (Context->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		*Buffer = (PUCHAR)WdfMemoryGetBuffer(
			Context->Connection.Usb.OutputReportMemory,
			BufferLength
		);

		break;

	case DsDeviceConnectionTypeBth:

		//
		// Skip first 2 bytes special to Bluetooth
		// 

		*Buffer = &((PUCHAR)WdfMemoryGetBuffer(
			Context->Connection.Bth.HidControl.WriteMemory,
			BufferLength
		))[2];

		*BufferLength -= 2;

		break;
	}
}

VOID DS3_SET_LED(
	PDEVICE_CONTEXT Context,
	UCHAR Value
)
{
	switch (Context->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:
		
		DS3_USB_SET_LED(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Usb.OutputReportMemory,
				NULL
			), Value);
		
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_LED(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Bth.HidControl.WriteMemory,
				NULL
			), Value);
		
		break;
	}
}

UCHAR DS3_GET_LED(
	PDEVICE_CONTEXT Context
)
{
	switch (Context->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		return DS3_USB_GET_LED(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Usb.OutputReportMemory,
				NULL
			));

	case DsDeviceConnectionTypeBth:

		return DS3_BTH_GET_LED(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Bth.HidControl.WriteMemory,
				NULL
			));
	}

	return 0x00;
}

VOID DS3_SET_SMALL_RUMBLE_DURATION(
	PDEVICE_CONTEXT Context,
	UCHAR Value
)
{
	switch (Context->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		DS3_USB_SET_SMALL_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Usb.OutputReportMemory,
				NULL
			), Value);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_SMALL_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Bth.HidControl.WriteMemory,
				NULL
			), Value);
		break;
	}
}

VOID DS3_SET_SMALL_RUMBLE_STRENGTH(
	PDEVICE_CONTEXT Context,
	UCHAR Value
)
{
	switch (Context->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		DS3_USB_SET_SMALL_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Usb.OutputReportMemory,
				NULL
			), Value);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_SMALL_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Bth.HidControl.WriteMemory,
				NULL
			), Value);
		break;
	}
}

VOID DS3_SET_LARGE_RUMBLE_DURATION(
	PDEVICE_CONTEXT Context,
	UCHAR Value
)
{
	switch (Context->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		DS3_USB_SET_LARGE_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Usb.OutputReportMemory,
				NULL
			), Value);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_LARGE_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Bth.HidControl.WriteMemory,
				NULL
			), Value);
		break;
	}
}

VOID DS3_SET_LARGE_RUMBLE_STRENGTH(
	PDEVICE_CONTEXT Context,
	UCHAR Value
)
{
	switch (Context->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		DS3_USB_SET_LARGE_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Usb.OutputReportMemory,
				NULL
			), Value);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_LARGE_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->Connection.Bth.HidControl.WriteMemory,
				NULL
			), Value);
		break;
	}
}
