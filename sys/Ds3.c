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
// Requests device host address, defaulting to a pre-defined address in case of failure, and sets the WDF_DEVICE's host address
//
NTSTATUS DsUsb_Ds3RequestHostAddress(WDFDEVICE Device)
{
	NTSTATUS status;
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	UCHAR controlTransferBuffer[CONTROL_TRANSFER_BUFFER_LENGTH];
	WDF_DEVICE_PROPERTY_DATA propertyData;
	UINT64 hostAddress;

	FuncEntry(TRACE_DS3);

	//
	// Request host BTH address
	// 
	if (NT_SUCCESS(status = USB_SendControlRequest(
		pDevCtx,
		BmRequestDeviceToHost,
		BmRequestClass,
		GetReport,
		Ds3FeatureHostAddress,
		0,
		controlTransferBuffer,
		CONTROL_TRANSFER_BUFFER_LENGTH
	)))
	{
		/*
		 * NOTE: the first byte is 0x01 followed by a 0x00 and then
		 * the host radio MAC address the device is currently paired to.
		 * The remaining ~36 bytes also contain data but we do not know
		 * what it stands for by the time of writing. Help pls!
		 */
		RtlCopyMemory(
			&pDevCtx->HostAddress,
			&controlTransferBuffer[2],
			sizeof(BD_ADDR)
		);
	}
	else
	{
		//
		// Set context's host radio address to pre-defined address in case of failure
		// 
		TraceError(
			TRACE_DS3,
			"Requesting host address failed with %!STATUS!. Setting device's context host address to 00:00:00:00:00:00",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"Requesting host address", status);

		const BD_ADDR zeroAddress = { 0 };
		RtlCopyMemory(
			&pDevCtx->HostAddress,
			&zeroAddress,
			sizeof(BD_ADDR)
		);
	}

	//
	// Store in property
	// 

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_LastHostRequestStatus);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	status = WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_NTSTATUS,
		sizeof(NTSTATUS),
		&status
	);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DS3,
			"Setting DEVPKEY_DsHidMini_RO_LastHostRequestStatus failed with status %!STATUS!",
			status
		);
	}

	//
	// Set host radio address property
	// 

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_BluetoothRadio_Address);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	hostAddress = (UINT64)(pDevCtx->HostAddress.Address[5]) |
		(UINT64)(pDevCtx->HostAddress.Address[4]) << 8 |
		(UINT64)(pDevCtx->HostAddress.Address[3]) << 16 |
		(UINT64)(pDevCtx->HostAddress.Address[2]) << 24 |
		(UINT64)(pDevCtx->HostAddress.Address[1]) << 32 |
		(UINT64)(pDevCtx->HostAddress.Address[0]) << 40;

	status = WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_UINT64,
		sizeof(UINT64),
		&hostAddress
	);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DS3,
			"Setting DEVPKEY_BluetoothRadio_Address failed with status %!STATUS!",
			status
		);
	}

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}

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
		DS3_USB_COMMON_ENABLE
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
// Sends a pairing request to the device, stores result status in property
// 
NTSTATUS DsUsb_Ds3SendPairingRequest(WDFDEVICE Device, BD_ADDR NewHostAddress)
{
	NTSTATUS status = STATUS_UNSUCCESSFUL;
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_DS3);

	TraceInformation(
		TRACE_DS3,
		"Sending pairing request with new host address defined as %02X:%02X:%02X:%02X:%02X:%02X",
		NewHostAddress.Address[0],
		NewHostAddress.Address[1],
		NewHostAddress.Address[2],
		NewHostAddress.Address[3],
		NewHostAddress.Address[4],
		NewHostAddress.Address[5]
	);

	UCHAR controlBuffer[SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH] = { 0x01, 0x00 };
	
	RtlCopyMemory(
		&controlBuffer[2],
		&NewHostAddress,
		sizeof(BD_ADDR)
	);

	//
	// Submit new host address
	// 
	if (!NT_SUCCESS(status = USB_SendControlRequest(
		pDevCtx,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		Ds3FeatureHostAddress,
		0,
		controlBuffer,
		SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH
	)))
	{
		TraceError(
			TRACE_DS3,
			"Setting host address failed with %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"Pairing", status);
	}

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}


//
// Pairs DS3 to current BT host or to user defined host address, depending on current pairing mode
// 
NTSTATUS DS3_GetActiveRadioAddress(BD_ADDR* Address)
{
	NTSTATUS status = STATUS_UNSUCCESSFUL;
	HANDLE hRadio = NULL;
	HBLUETOOTH_RADIO_FIND hFind = NULL;
	BLUETOOTH_FIND_RADIO_PARAMS params;
	BLUETOOTH_RADIO_INFO info;
	DWORD ret;
	DWORD error = ERROR_SUCCESS;

	params.dwSize = sizeof(BLUETOOTH_FIND_RADIO_PARAMS);
	info.dwSize = sizeof(BLUETOOTH_RADIO_INFO);

	FuncEntry(TRACE_DS3);

	do
	{
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

			if (error == ERROR_NO_MORE_ITEMS)
			{
				TraceWarning(
					TRACE_DS3,
					"No active host radio found."
				);
			}
			else
			{
				TraceError(
					TRACE_DS3,
					"BluetoothFindFirstRadio failed with error %!WINERROR!",
					error
				);
			}
			
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
				"BluetoothGetRadioInfo failed with error %!WINERROR!",
				error
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"BluetoothGetRadioInfo", error);
			break;
		}

		// Copy and reverse to match expected format/order
		for (int i = 0; i < sizeof(BD_ADDR); i++)
		{
			Address->Address[sizeof(BD_ADDR) - 1 - i] = info.address.rgBytes[i];
		}

		status = STATUS_SUCCESS;

	} while (FALSE);

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

	if (hRadio)
		CloseHandle(hRadio);
	if (hFind)
		BluetoothFindRadioClose(hFind);

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}

//
// Pairs DS3 to current BT host or to user defined host address, depending on current pairing mode
// 
NTSTATUS DsUsb_Ds3PairToNewHost(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_UNSUCCESSFUL;
	WDF_DEVICE_PROPERTY_DATA propertyData;
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	BD_ADDR newHostAddress = { 0 };

	FuncEntry(TRACE_DS3);

	do
	{
		if (pDevCtx->Configuration.DevicePairingMode == DsDevicePairingModeDisabled)
		{
			TraceInformation(
				TRACE_DS3,
				"Pairing mode set to disabled. Skipping pairing process"
			);
			status = STATUS_SUCCESS;
			break;
		}

		if (pDevCtx->Configuration.DevicePairingMode == DsDevicePairingModeAuto)
		{
			TraceInformation(
				TRACE_DS3,
				"Pairing device to active radio host address"
			);

			if (!NT_SUCCESS(DS3_GetActiveRadioAddress(&newHostAddress)))
			{
				TraceError(
					TRACE_DS3,
					"Failed to get active radio host address"
				);
				break;
			}
		}

		//
		// Use configured custom mac address if in custom mode
		//
		if (pDevCtx->Configuration.DevicePairingMode == DsDevicePairingModeCustom)
		{
			TraceInformation(
				TRACE_DS3,
				"Pairing device to user defined MAC address"
			);

			for (int i = 0; i < sizeof(BD_ADDR); i++)
			{
				newHostAddress.Address[i] = pDevCtx->Configuration.CustomHostAddress[i];
			}
		}

		//
		// Don't issue request when addresses already match
		// 
		if (RtlCompareMemory(
				&newHostAddress,
				&pDevCtx->HostAddress.Address[0],
				sizeof(BD_ADDR)
			) == sizeof(BD_ADDR)
			)
		{
			TraceInformation(
				TRACE_DS3,
				"Device's current host address equals desired new address, skipping"
			);

			EventWriteAlreadyPaired(pDevCtx->DeviceAddressString);

			status = STATUS_SUCCESS;
			break;
		}

		//
		// Send pairing request
		//
		if (!NT_SUCCESS(status = DsUsb_Ds3SendPairingRequest(Device, newHostAddress)))
		{
			TraceError(
				TRACE_DS3,
				"DsUsb_Ds3SendPairingRequest failed with status %!STATUS!",
				status
			);
			break;
		}

		status = STATUS_SUCCESS;

	} while (FALSE);

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

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DS3,
			"Setting DEVPKEY_DsHidMini_RO_LastPairingStatus failed with status %!STATUS!",
			status
		);
	}

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}

//
// Send magic packet over BTH
// 
NTSTATUS DsBth_Ds3SixaxisInit(PDEVICE_CONTEXT Context)
{
	FuncEntry(TRACE_DS3);

	// 
	// "Magic packet"
	// 
	BYTE hidCommandEnable[] = {
		DS3_BTH_SIXAXIS_ENABLE
	};

	NTSTATUS status;
	size_t bytesWritten = 0;

	if (!NT_SUCCESS(status = DMF_DefaultTarget_SendSynchronously(
		Context->Connection.Bth.HidControl.OutputWriterModule,
		hidCommandEnable,
		ARRAYSIZE(hidCommandEnable),
		NULL,
		0,
		ContinuousRequestTarget_RequestType_Ioctl,
		IOCTL_BTHPS3_HID_CONTROL_WRITE,
		0,
		&bytesWritten
	)))
	{
		TraceError(
			TRACE_DS3,
			"DMF_DefaultTarget_SendSynchronously failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_DefaultTarget_SendSynchronously", status);
	}

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}

//
// Sets all properties for a specific Player LED
// 
VOID DS3_SET_LED_DURATION(
	PDEVICE_CONTEXT Context,
	UCHAR LedIndex,
	UCHAR TotalDuration,
	USHORT BasePortionDuration,
	UCHAR OffPortionMultiplier,
	UCHAR OnPortionMultiplier
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
	buffer[11 + (LedIndex * 5)] = BasePortionDuration >> 8;
	buffer[12 + (LedIndex * 5)] = BasePortionDuration & 0xFF;
	buffer[13 + (LedIndex * 5)] = OffPortionMultiplier;
	buffer[14 + (LedIndex * 5)] = OnPortionMultiplier;
}

//
// Revert LED properties to default for a specific Player LED
// 
VOID DS3_SET_LED_DURATION_DEFAULT(PDEVICE_CONTEXT Context, UCHAR LedIndex)
{
	DS3_SET_LED_DURATION(
		Context,
		LedIndex,
		0xFF, // Interval repeat never ends
		0x27, // BasePortionDuration
		0x00, // No OFF-portion
		0x32 // Default ON-portion multiplier
	);
}

//
// Gets output report protocol-agnostic
// 
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

//
// Gets output report without offset
// 
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

//
// Sets the LED flags byte
// 
VOID DS3_SET_LED_FLAGS(
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

//
// Gets the LED flags byte
// 
UCHAR DS3_GET_LED_FLAGS(
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
	Context->RumbleControlState.LightCache = Value;
	DS3_PROCESS_RUMBLE_STRENGTH(Context);
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
	Context->RumbleControlState.HeavyCache = Value;
	DS3_PROCESS_RUMBLE_STRENGTH(Context);
}

VOID DS3_SET_BOTH_RUMBLE_STRENGTH(
	PDEVICE_CONTEXT Context,
	UCHAR LargeValue,
	UCHAR SmallValue
)
{
	Context->RumbleControlState.LightCache = SmallValue;
	Context->RumbleControlState.HeavyCache = LargeValue;
	DS3_PROCESS_RUMBLE_STRENGTH(Context);
}

VOID DS3_PROCESS_RUMBLE_STRENGTH(
	PDEVICE_CONTEXT Context
)
{
	DS_RUMBLE_SETTINGS* rumbSet = &Context->Configuration.RumbleSettings;

	DS_RESCALE_STATE* heavyResc = &Context->RumbleControlState.HeavyRescale;
	DS_RESCALE_STATE* lightResc = &Context->RumbleControlState.AltMode.LightRescale;

	// Get last received rumble values so they can be processed
	DOUBLE heavyRumble = Context->RumbleControlState.HeavyCache;
	DOUBLE lightRumble = Context->RumbleControlState.LightCache;

	// LINEAR RANGE RESCALLING
	// 
	// To rescale a value that exists in a range into a new range:
	// newvalue = a * value + b
	//
	// a = (max'-min')/(max-min)
	// b = max' - a * max
	//
	// In which max and min are the limits of the the original range
	// For the DS3 rumble, it's max = 255 and min = 1 since 0 is not considered.
	// 
	// max' and min' are the limits of the new range
	// 0 is not considered for the new range too regarding rumble
	//
	// constants a and b are calculated on configuration (re-)loading

	if (Context->RumbleControlState.AltMode.IsEnabled && lightResc->IsAllowed)
	{
		if (lightRumble > 0) {

			// Light Motor Strength Rescale 
			lightRumble = lightResc->ConstA * lightRumble + lightResc->ConstB;
			if (lightRumble > heavyRumble)
			{
				heavyRumble = lightRumble;
			}
			lightRumble = 0; // Always disable Light Motor after the if statement above
		}

		// Force Activate right motor if original heavy or light values are above their respective thresholds
		if (
			(rumbSet->AlternativeMode.ForcedRight.IsHeavyThresholdEnabled && (Context->RumbleControlState.HeavyCache >= rumbSet->AlternativeMode.ForcedRight.HeavyThreshold))
			||
			(rumbSet->AlternativeMode.ForcedRight.IsLightThresholdEnabled && (Context->RumbleControlState.LightCache >= rumbSet->AlternativeMode.ForcedRight.LightThreshold))
			)
		{
			lightRumble = 1;
		}
	}
	else
	{
		if (rumbSet->DisableLeft) heavyRumble = 0;
		if (rumbSet->DisableRight) lightRumble = 0;
	}

	// Heavy Motor Strength Rescale
	if (heavyRumble > 0 && Context->RumbleControlState.HeavyRescaleEnabled && heavyResc->IsAllowed)
	{
		heavyRumble = heavyResc->ConstA * heavyRumble + heavyResc->ConstB;
	}

	switch (Context->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		DS3_USB_SET_LARGE_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), (UCHAR)heavyRumble);
		DS3_USB_SET_SMALL_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), (UCHAR)lightRumble);
		break;

	case DsDeviceConnectionTypeBth:

		DS3_BTH_SET_LARGE_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), (UCHAR)heavyRumble);
		DS3_BTH_SET_SMALL_RUMBLE_STRENGTH(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), (UCHAR)lightRumble);
		break;
	}

}
