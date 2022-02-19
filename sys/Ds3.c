#include "Driver.h"
#include "Ds3.tmh"
#include <bluetoothapis.h>


//
// Default Output Report for LED & Rumble state changes (USB)
// 
const UCHAR G_Ds3UsbHidOutputReport[] = {
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
NTSTATUS DsUsb_Ds3PairToFirstRadio(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_UNSUCCESSFUL;
	HANDLE hRadio = NULL;
	HBLUETOOTH_RADIO_FIND hFind = NULL;
	BLUETOOTH_FIND_RADIO_PARAMS params;
	BLUETOOTH_RADIO_INFO info;
	DWORD ret;
	DWORD error = ERROR_SUCCESS;
	WDF_DEVICE_PROPERTY_DATA propertyData;
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DS3);
	
	params.dwSize = sizeof(BLUETOOTH_FIND_RADIO_PARAMS);
	info.dwSize = sizeof(BLUETOOTH_RADIO_INFO);

	do {
		//
		// Grab first (active) radio
		// 
		hFind = BluetoothFindFirstRadio(
			&params,
			&hRadio
		);

		if (!hFind)
		{
			error = GetLastError();
			TraceError(
				TRACE_DS3,
				"BluetoothFindFirstRadio failed: 0x%X",
				error
			);
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
			error = ret;
			TraceError(
				TRACE_DS3,
				"BluetoothGetRadioInfo failed: 0x%X", 
				error
			);
			break;
		}

		//
		// Align address to expected format/order
		// 
		REVERSE_BYTE_ARRAY(&info.address.rgBytes[0], sizeof(BD_ADDR));

		//
		// Don't issue request when addresses already match
		// 
		if (RtlCompareMemory(
				&info.address.rgBytes[0],
				&pDevCtx->HostAddress.Address[0],
				sizeof(BD_ADDR)
			) == sizeof(BD_ADDR)
		)
		{
			TraceVerbose(
				TRACE_DS3,
				"Host address equals local radio address, skipping"
			);

			status = STATUS_SUCCESS;
			break;
		}
		
		TraceInformation(
			TRACE_DS3,
			"Updating host address from %02X:%02X:%02X:%02X:%02X:%02X to %02X:%02X:%02X:%02X:%02X:%02X",
			pDevCtx->HostAddress.Address[0],
			pDevCtx->HostAddress.Address[1],
			pDevCtx->HostAddress.Address[2],
			pDevCtx->HostAddress.Address[3],
			pDevCtx->HostAddress.Address[4],
			pDevCtx->HostAddress.Address[5],
			info.address.rgBytes[0],
			info.address.rgBytes[1],
			info.address.rgBytes[2],
			info.address.rgBytes[3],
			info.address.rgBytes[4],
			info.address.rgBytes[5]
		);
		
		UCHAR controlBuffer[SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH];
		
		RtlZeroMemory(
			controlBuffer, 
			SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH
		);

		RtlCopyMemory(
			&controlBuffer[2], 
			&info.address, 
			sizeof(BD_ADDR)
		);

		//
		// Submit new host address
		// 
		status = USB_SendControlRequest(
			pDevCtx,
			BmRequestHostToDevice,
			BmRequestClass,
			SetReport,
			Ds3FeatureHostAddress,
			0,
			controlBuffer,
			SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH
		);

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DS3,
				"Setting host address failed with %!STATUS!", 
				status
			);
			break;
		}

		//
		// Update in device context after success
		// 
		RtlCopyMemory(&pDevCtx->HostAddress, &info.address, sizeof(BD_ADDR));
		
	} while (FALSE);

	if (hRadio)
		CloseHandle(hRadio);
	if (hFind)
		CloseHandle(hFind);

	//
	// Translate Win32 error codes to NTSTATUS
	// 
	if (!NT_SUCCESS(status))
	{
		switch (error)
		{
		case ERROR_NO_MORE_ITEMS:
			status = STATUS_NO_MORE_ENTRIES;
			break;
		case ERROR_INVALID_PARAMETER:
			status = STATUS_INVALID_PARAMETER;
			break;
		case ERROR_REVISION_MISMATCH:
			status = STATUS_REVISION_MISMATCH;
			break;
		case ERROR_OUTOFMEMORY:
			status = STATUS_SECTION_NOT_EXTENDED;
			break;
		default:
			break;
		}
	}

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_LastPairingStatus);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	//
	// Store in property
	// 
	status = WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_NTSTATUS,
		sizeof(NTSTATUS),
		&status
	);
	
	FuncExit(TRACE_DS3, "status=%!STATUS!", status);
	
	return status;
}

//
// Send magic packet over BTH
// 
NTSTATUS DsBth_Ds3Init(PDEVICE_CONTEXT Context)
{
	FuncEntry(TRACE_DS3);

	// 
	// "Magic packet"
	// 
	BYTE hidCommandEnable[] = {
		0x53, 0xF4, 0x42, 0x03, 0x00, 0x00
	};

	NTSTATUS status;
	size_t bytesWritten = 0;

	status = DMF_DefaultTarget_SendSynchronously(
		Context->Connection.Bth.HidControl.OutputWriterModule,
		hidCommandEnable,
		ARRAYSIZE(hidCommandEnable),
		NULL,
		0,
		ContinuousRequestTarget_RequestType_Ioctl,
		IOCTL_BTHPS3_HID_CONTROL_WRITE,
		0,
		&bytesWritten
	);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DS3,
			"DMF_DefaultTarget_SendSynchronously failed with status %!STATUS!",
			status
		);
	}

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}

VOID DS3_SET_LED_DURATION(
	PDEVICE_CONTEXT Context, 
	UCHAR LedIndex, 
	UCHAR TotalDuration, 
	UCHAR Interval, 
	UCHAR OffInterval,
	UCHAR OnInterval
)
{
	if (LedIndex > 3)
		return;

	// Inverse
	LedIndex = 3 - LedIndex;
	
	PUCHAR buffer;

	DS3_GET_UNIFIED_OUTPUT_REPORT_BUFFER(
		Context,
		&buffer,
		NULL
	);

	buffer[10 + (LedIndex * 5)] = TotalDuration;
	buffer[11 + (LedIndex * 5)] = Interval;
	buffer[13 + (LedIndex * 5)] = OffInterval;
	buffer[14 + (LedIndex * 5)] = OnInterval;
}

VOID DS3_SET_LED_DURATION_DEFAULT(PDEVICE_CONTEXT Context, UCHAR LedIndex)
{
	DS3_SET_LED_DURATION(
		Context, 
		LedIndex, 
		0xFF, // Interval repeat never ends
		0x27, // Interval duration
		0x00, // No OFF-portion
		0x32 // Default ON-portion
	);
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

		//
		// Skip Report ID
		// 

		*Buffer = &((PUCHAR)WdfMemoryGetBuffer(
			Context->OutputReportMemory,
			BufferLength
		))[1];

		if (BufferLength)
			*BufferLength -= 1;
		
		break;

	case DsDeviceConnectionTypeBth:

		//
		// Skip first 2 bytes special to Bluetooth
		// 

		*Buffer = &((PUCHAR)WdfMemoryGetBuffer(
			Context->OutputReportMemory,
			BufferLength
		))[2];

		if (BufferLength)
			*BufferLength -= 2;

		break;
	}
}

VOID DS3_GET_RAW_OUTPUT_REPORT_BUFFER(
	PDEVICE_CONTEXT Context,
	UCHAR** Buffer,
	PSIZE_T BufferLength
)
{
	*Buffer = (PUCHAR)WdfMemoryGetBuffer(
		Context->OutputReportMemory,
		BufferLength
	);
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
				Context->OutputReportMemory,
				NULL
			), Value);
		
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_LED(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
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
				Context->OutputReportMemory,
				NULL
			));

	case DsDeviceConnectionTypeBth:

		return DS3_BTH_GET_LED(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
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
				Context->OutputReportMemory,
				NULL
			), Value);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_SMALL_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
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
				Context->OutputReportMemory,
				NULL
			), Value);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_SMALL_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
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
				Context->OutputReportMemory,
				NULL
			), Value);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_LARGE_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
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
				Context->OutputReportMemory,
				NULL
			), Value);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_LARGE_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), Value);
		break;
	}
}

VOID DS3_SET_BOTH_RUMBLE_STRENGTH(
	PDEVICE_CONTEXT Context
)
{
	DOUBLE LargeValue = Context->MotorStrCache.Big, SmallValue = Context->MotorStrCache.Small;

	if(SmallValue > 0) {

		if (Context->Configuration.RumbleSettings.SMToBMConversion.Enabled) {

			// Small Motor Strength Rescale
			SmallValue = 
				(DOUBLE)(Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMaxValue - Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMinValue)
				/ 254 * (SmallValue - 255) + Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMaxValue;

			if (SmallValue > LargeValue) {
				LargeValue = SmallValue;
			}
			SmallValue = 0; // Always disable Small Motor after the comparison above

			// Force Activate Small Motor if original BIG Motor Strength is above certain level and related boolean is enabled
			if (
				/*
				Context->Configuration.RumbleSettings.ForcedSM.BMThresholdEnabled
				&& Context->RawBigMotorStrengthCache >= Context->Configuration.RumbleSettings.ForcedSM.BMThresholdValue
				) {
				SmallValue = 1;
				*/
				Context->Configuration.RumbleSettings.ForcedSM.BMThresholdEnabled
				&& Context->MotorStrCache.Big >= Context->Configuration.RumbleSettings.ForcedSM.BMThresholdValue
				) 
			{
				SmallValue = 1;
			}

			// Force Activate Small Motor if original SMALL Motor Strength is above certain level and related boolean is enabled
			if (
				/*
				Context->Configuration.RumbleSettings.ForcedSM.SMThresholdEnabled
				&& Context->RawSmallMotorStrengthCache >= Context->Configuration.RumbleSettings.ForcedSM.SMThresholdValue
				*/
				Context->Configuration.RumbleSettings.ForcedSM.SMThresholdEnabled
				&& Context->MotorStrCache.Small >= Context->Configuration.RumbleSettings.ForcedSM.SMThresholdValue
				)
			{
				SmallValue = 1;
			}

		}
	}

	// Big Motor Strength Rescale
	if (Context->Configuration.RumbleSettings.BMStrRescale.Enabled && LargeValue > 0) {
		LargeValue = 
			(DOUBLE)(Context->Configuration.RumbleSettings.BMStrRescale.MaxValue - Context->Configuration.RumbleSettings.BMStrRescale.MinValue)
			/ (254) * (LargeValue - 255) + Context->Configuration.RumbleSettings.BMStrRescale.MaxValue;
	}


	switch (Context->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		DS3_USB_SET_LARGE_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), (UCHAR)LargeValue);
		DS3_USB_SET_SMALL_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), (UCHAR)SmallValue);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_LARGE_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), (UCHAR)LargeValue);
		DS3_BTH_SET_SMALL_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), (UCHAR)SmallValue);
		break;
	}

}
