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
	//
	// LED flags byte (offset 10) starts off (see issue #351); the four
	// 5-byte effect blocks below use the PS3-correct static effect
	// (FF.00.01.00.01, see issue #365) rather than the previous
	// FF.27.10.00.32, which is meaningless since no LED flag is set here
	// anyway - DsLed_Apply/DsLed_SetFlagsAndEffects normalize both flags
	// and effect blocks together before anything is ever sent.
	// 
	0xFF, 0x00, 0x01, 0x00, 0x01,
	0xFF, 0x00, 0x01, 0x00, 0x01,
	0xFF, 0x00, 0x01, 0x00, 0x01,
	0xFF, 0x00, 0x01, 0x00, 0x01,
	0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
};

//
// Default Output Report for LED & Rumble state changes (Bluetooth)
// 
const UCHAR G_Ds3BthHidOutputReport[] = {
	DS3_BTH_HID_OUTPUT_REPORT_CONTROL_PREFIX, /* HID BT Set_report (0x50) | Output (0x02); send path may override to 0xA2 */
	0x01, /* Report ID */
	0x00, 0xFF, 0x00, 0xFF, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00,
	//
	// See G_Ds3UsbHidOutputReport above (issues #351 and #365); LED flags
	// byte (offset 11) starts off and is seeded to DS3_LED_OFF explicitly
	// in Device.c right after this buffer is copied in, so both connection
	// types are consistent from the very first output report.
	// 
	0xFF, 0x00, 0x01, 0x00, 0x01,
	0xFF, 0x00, 0x01, 0x00, 0x01,
	0xFF, 0x00, 0x01, 0x00, 0x01,
	0xFF, 0x00, 0x01, 0x00, 0x01,
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
		CONTROL_TRANSFER_BUFFER_LENGTH,
		NULL
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

	if (!NT_SUCCESS(status = WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_NTSTATUS,
		sizeof(NTSTATUS),
		&status
	)))
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
	
	if (!NT_SUCCESS(status = WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_UINT64,
		sizeof(UINT64),
		&hostAddress
	)))
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
// Instructs the DS3 to start sending data on Interrupt IN
// 
NTSTATUS DsUsb_Ds3Init(PDEVICE_CONTEXT Context)
{
	FuncEntry(TRACE_DS3);

	UCHAR hidCommandEnable[] = {
		DS3_USB_COMMON_ENABLE
	};

	const NTSTATUS status = USB_SendControlRequest(
		Context,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		Ds3FeatureDeviceState,
		0,
		hidCommandEnable,
		ARRAYSIZE(hidCommandEnable),
		NULL
	);

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}

//
// Instructs the DS3 to stop sending data on Interrupt IN
// 
NTSTATUS DsUsb_Ds3Shutdown(PDEVICE_CONTEXT Context)
{
	FuncEntry(TRACE_DS3);

	UCHAR hidCommandEnable[] = {
		DS3_USB_COMMON_DISABLE
	};

	const NTSTATUS status = USB_SendControlRequest(
		Context,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		Ds3FeatureDeviceState,
		0,
		hidCommandEnable,
		ARRAYSIZE(hidCommandEnable),
		NULL
	);

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}

//
// Sends an output report (LEDs/rumble) over the control endpoint (EP0) via
// SET_REPORT, exactly like the PS3 itself does for the very first report
// after enumeration and again right after enabling streaming (see issue
// #321). Buffer must not include the report ID byte (Caller strips it, same
// convention as Ds3_GetUnifiedOutputReportBuffer for USB).
// 
NTSTATUS DsUsb_Ds3SendOutputReportControl(PDEVICE_CONTEXT Context, PUCHAR Buffer, ULONG BufferLength)
{
	FuncEntry(TRACE_DS3);

	const NTSTATUS status = USB_SendControlRequest(
		Context,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		Dss3FeatureOutputReport,
		0,
		Buffer,
		BufferLength,
		NULL
	);

	if (!NT_SUCCESS(status))
	{
		TraceWarning(
			TRACE_DS3,
			"Sending output report via control endpoint failed with %!STATUS!",
			status
		);
	}

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}

//
// Maximum number of attempts to read the device's own Bluetooth MAC address
// (Feature 0xF2) before giving up and synthesizing a fallback. See issue
// #321: some fakes/clones are timing-sensitive and may succeed on a retry;
// others (e.g. the Retro Fighters Defender's original USB mode) never
// implement this request and there is no point retrying forever.
// 
#define DS3_DEVICE_ADDRESS_MAX_ATTEMPTS      3

//
// Delay between device address retry attempts, in milliseconds.
// 
#define DS3_DEVICE_ADDRESS_RETRY_DELAY_MS    50

//
// Delay observed between the PS3's SET Feature 0xF5 (pairing) and its
// verification GET Feature 0xF5 (27-75 ms across capture samples). Mirrored
// here so a freshly-paired device has settled before we read it back.
// 
#define DS3_PAIRING_VERIFY_DELAY_MS          50

//
// Derives a stable, locally-administered fallback MAC address from the
// device's PnP instance ID, for controllers that do not answer Feature 0xF2
// (see issue #321). Deterministic (not random) so the address, and therefore
// the per-device JSON config key derived from it, stays the same across
// replugs on the same USB port. The locally-administered bit is set and the
// multicast bit cleared so this can never collide with a real, globally
// assigned Sony OUI, allowing the genuine/fake heuristics elsewhere in the
// stack (see issue #3) to keep working correctly.
// 
static VOID DsUsb_Ds3SynthesizeDeviceAddress(_In_ WDFDEVICE Device, _Out_ PBD_ADDR Address)
{
	WDF_DEVICE_PROPERTY_DATA propertyData;
	WDFMEMORY instanceIdMemory = NULL;
	DEVPROPTYPE propType;
	ULONGLONG hash = 0xCBF29CE484222325ULL; // FNV-1a 64-bit offset basis
	const ULONGLONG prime = 0x100000001B3ULL; // FNV-1a 64-bit prime

	FuncEntry(TRACE_DS3);

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_Device_InstanceId);

	if (NT_SUCCESS(WdfDeviceAllocAndQueryPropertyEx(
		Device,
		&propertyData,
		NonPagedPoolNx,
		WDF_NO_OBJECT_ATTRIBUTES,
		&instanceIdMemory,
		&propType
	)))
	{
		size_t bufferSize = 0;
		const PUCHAR buffer = (PUCHAR)WdfMemoryGetBuffer(instanceIdMemory, &bufferSize);

		for (size_t i = 0; i < bufferSize; i++)
		{
			hash ^= buffer[i];
			hash *= prime;
		}

		WdfObjectDelete(instanceIdMemory);
	}
	else
	{
		//
		// Extremely unlikely (querying our own instance ID should never
		// fail); fall back to a fixed seed so the device can still boot.
		// 
		TraceWarning(
			TRACE_DS3,
			"Querying DEVPKEY_Device_InstanceId failed, using a fixed seed for the synthesized address"
		);

		hash = 0xDEC0DE0BADC0FFEEULL;
	}

	for (size_t i = 0; i < sizeof(BD_ADDR); i++)
	{
		Address->Address[i] = (UCHAR)((hash >> (8 * i)) & 0xFF);
	}

	//
	// Set locally-administered bit, clear multicast bit (IEEE 802 convention)
	// 
	Address->Address[0] = (Address->Address[0] & 0xFE) | 0x02;

	FuncExitNoReturn(TRACE_DS3);
}

//
// Requests the device's own Bluetooth MAC address (Feature 0xF2), retrying a
// bounded number of times. On persistent failure this no longer fails the
// caller: a deterministic fallback address is synthesized instead so devices
// without this feature report (e.g. the Retro Fighters Defender in its
// original USB mode) can still be used. See issue #321.
// 
NTSTATUS DsUsb_Ds3RequestDeviceAddress(WDFDEVICE Device)
{
	NTSTATUS status = STATUS_UNSUCCESSFUL;
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	UCHAR controlTransferBuffer[CONTROL_TRANSFER_BUFFER_LENGTH];
	WDF_DEVICE_PROPERTY_DATA propertyData;
	WCHAR deviceAddress[DSHM_DEVICE_ADDRESS_CCH];
	BOOLEAN synthesized;

	FuncEntry(TRACE_DS3);

	for (ULONG attempt = 1; attempt <= DS3_DEVICE_ADDRESS_MAX_ATTEMPTS; attempt++)
	{
		if (NT_SUCCESS(status = USB_SendControlRequest(
			pDevCtx,
			BmRequestDeviceToHost,
			BmRequestClass,
			GetReport,
			Ds3FeatureDeviceAddress,
			0,
			controlTransferBuffer,
			CONTROL_TRANSFER_BUFFER_LENGTH,
			NULL
		)))
		{
			break;
		}

		if (attempt < DS3_DEVICE_ADDRESS_MAX_ATTEMPTS)
		{
			TraceWarning(
				TRACE_DS3,
				"Requesting device address attempt %d of %d failed with %!STATUS!, retrying in %d ms",
				attempt,
				DS3_DEVICE_ADDRESS_MAX_ATTEMPTS,
				status,
				DS3_DEVICE_ADDRESS_RETRY_DELAY_MS
			);

			Sleep(DS3_DEVICE_ADDRESS_RETRY_DELAY_MS);
		}
	}

	const NTSTATUS requestStatus = status;

	if (NT_SUCCESS(status))
	{
		RtlCopyMemory(
			&pDevCtx->DeviceAddress,
			&controlTransferBuffer[4],
			sizeof(BD_ADDR)
		);

		pDevCtx->SupportsBluetoothAddressReports = TRUE;
		synthesized = FALSE;

		TraceInformation(
			TRACE_DS3,
			"Device address: %02X:%02X:%02X:%02X:%02X:%02X",
			pDevCtx->DeviceAddress.Address[0],
			pDevCtx->DeviceAddress.Address[1],
			pDevCtx->DeviceAddress.Address[2],
			pDevCtx->DeviceAddress.Address[3],
			pDevCtx->DeviceAddress.Address[4],
			pDevCtx->DeviceAddress.Address[5]
		);
	}
	else
	{
		TraceWarning(
			TRACE_DS3,
			"Requesting device address failed with %!STATUS! after %d attempts, synthesizing a fallback address",
			status,
			DS3_DEVICE_ADDRESS_MAX_ATTEMPTS
		);

		DsUsb_Ds3SynthesizeDeviceAddress(Device, &pDevCtx->DeviceAddress);

		pDevCtx->SupportsBluetoothAddressReports = FALSE;
		synthesized = TRUE;

		TraceWarning(
			TRACE_DS3,
			"Synthesized device address: %02X:%02X:%02X:%02X:%02X:%02X",
			pDevCtx->DeviceAddress.Address[0],
			pDevCtx->DeviceAddress.Address[1],
			pDevCtx->DeviceAddress.Address[2],
			pDevCtx->DeviceAddress.Address[3],
			pDevCtx->DeviceAddress.Address[4],
			pDevCtx->DeviceAddress.Address[5]
		);

		//
		// Never fail device start because of this; the whole point of
		// issue #321 is that a missing MAC report must not be fatal.
		// 
		status = STATUS_SUCCESS;
	}

	pDevCtx->DeviceAddressSynthesized = synthesized;

	DsDevice_FormatCanonicalAddress(
		pDevCtx,
		deviceAddress,
		ARRAYSIZE(deviceAddress)
	);

	sprintf_s(
		pDevCtx->DeviceAddressString,
		ARRAYSIZE(pDevCtx->DeviceAddressString),
		"%02X%02X%02X%02X%02X%02X",
		pDevCtx->DeviceAddress.Address[0],
		pDevCtx->DeviceAddress.Address[1],
		pDevCtx->DeviceAddress.Address[2],
		pDevCtx->DeviceAddress.Address[3],
		pDevCtx->DeviceAddress.Address[4],
		pDevCtx->DeviceAddress.Address[5]
	);

	if (synthesized)
	{
		//
		// DeviceAddressString is only valid from this point on, so the
		// fallback event (and the address it references) is only emitted
		// after it has been formatted above.
		// 
		EventWriteDeviceAddressUnavailableUsingFallback(pDevCtx->DeviceAddressString, requestStatus);
	}

	//
	// Set device address property
	// 

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_Bluetooth_DeviceAddress);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	if (!NT_SUCCESS(WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_STRING,
		ARRAYSIZE(deviceAddress) * sizeof(WCHAR),
		deviceAddress
	)))
	{
		TraceError(
			TRACE_DS3,
			"Setting DEVPKEY_Bluetooth_DeviceAddress failed with status %!STATUS!",
			status
		);
	}

	//
	// Set synthesized-address flag property, so ControlApp can tell the user
	// this device never reported a real Bluetooth MAC of its own.
	// 

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_DeviceAddressSynthesized);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	if (!NT_SUCCESS(WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_BOOLEAN,
		sizeof(BOOLEAN),
		&synthesized
	)))
	{
		TraceError(
			TRACE_DS3,
			"Setting DEVPKEY_DsHidMini_RO_DeviceAddressSynthesized failed with status %!STATUS!",
			status
		);
	}

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
		SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH,
		NULL
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
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	BD_ADDR newHostAddress = { 0 };

	FuncEntry(TRACE_DS3);

	do
	{
		if (!pDevCtx->SupportsBluetoothAddressReports)
		{
			//
			// This device never reported its own Bluetooth MAC (see issue
			// #321), so it cannot meaningfully be paired to a host either;
			// the address the console/host would store against is not one
			// this device recognizes. Callers already tolerate this status.
			// 
			TraceInformation(
				TRACE_DS3,
				"Device does not support Bluetooth address reports, skipping pairing"
			);
			status = STATUS_NOT_SUPPORTED;
			break;
		}

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

	FuncExit(TRACE_DS3, "status=%!STATUS!", status);

	return status;
}

//
// Pairs to the configured host (or skips it, see DsUsb_Ds3PairToNewHost) and
// then re-reads the host address to verify/reflect the outcome, waiting a
// short delay in between. The PS3 itself leaves 27-75 ms between its SET
// Feature 0xF5 and the verifying GET Feature 0xF5 across capture samples;
// mirrored here as a single fixed delay so a freshly-paired device has
// settled before being read back. Used by every call site that used to
// call DsUsb_Ds3PairToNewHost followed by DsUsb_Ds3RequestHostAddress
// directly, so the delay only needs to live in one place. See issue #321.
// 
NTSTATUS DsUsb_Ds3PairAndVerify(_In_ WDFDEVICE Device, _Out_opt_ PNTSTATUS ReadStatus)
{
	FuncEntry(TRACE_DS3);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	const NTSTATUS writeStatus = DsUsb_Ds3PairToNewHost(Device);

	//
	// A device that never reported its own Bluetooth MAC (see issue #321)
	// doesn't support the host-address feature report either; skip the
	// verify read so we don't emit a spurious failed-host-request trace
	// and event for an expected, already-logged condition, and keep
	// reporting the not-supported status instead of overwriting it with
	// whatever the (equally unsupported) read attempt would have failed
	// with.
	// 
	if (writeStatus == STATUS_NOT_SUPPORTED || !pDevCtx->SupportsBluetoothAddressReports)
	{
		WDF_DEVICE_PROPERTY_DATA propertyData;
		NTSTATUS notSupportedStatus = STATUS_NOT_SUPPORTED;

		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_LastHostRequestStatus);
		propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
		propertyData.Lcid = LOCALE_NEUTRAL;

		(VOID)WdfDeviceAssignProperty(
			Device,
			&propertyData,
			DEVPROP_TYPE_NTSTATUS,
			sizeof(NTSTATUS),
			&notSupportedStatus
		);

		if (ReadStatus)
		{
			*ReadStatus = STATUS_NOT_SUPPORTED;
		}

		FuncExit(TRACE_DS3, "writeStatus=%!STATUS!", writeStatus);

		return writeStatus;
	}

	if (NT_SUCCESS(writeStatus))
	{
		Sleep(DS3_PAIRING_VERIFY_DELAY_MS);
	}

	const NTSTATUS readStatus = DsUsb_Ds3RequestHostAddress(Device);

	if (ReadStatus)
	{
		*ReadStatus = readStatus;
	}

	FuncExit(TRACE_DS3, "writeStatus=%!STATUS!", writeStatus);

	return writeStatus;
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
VOID DsLed_SetEffect(
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

	Ds3_GetUnifiedOutputReportBuffer(
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
// Gets output report protocol-agnostic
// 
VOID Ds3_GetUnifiedOutputReportBuffer(
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
VOID Ds3_GetRawOutputReportBuffer(
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
VOID DsLed_SetFlags(
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
UCHAR DsLed_GetFlags(
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

_Use_decl_annotations_
NTSTATUS
DSHM_SetIpcRumble(
	_In_ PDEVICE_CONTEXT Context,
	_In_ UCHAR LargeValue,
	_In_ UCHAR SmallValue
)
{
	NTSTATUS status;

	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	DS3_SET_BOTH_RUMBLE_STRENGTH(Context, LargeValue, SmallValue);
	status = DSHM_SendOutputReportUnlocked(Context, Ds3OutputReportSourceIpc);

	WdfWaitLockRelease(Context->OutputReport.Lock);

	return status;
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

	//
	// Navigation has no motors. Zero after AlternativeMode.ForcedRight
	// (and rescale) so a cached threshold cannot re-arm the small motor
	// before the output-report write (issue #48).
	//
	if (Context->DeviceType == DsDeviceTypeNavigation)
	{
		heavyRumble = 0;
		lightRumble = 0;
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

		//
		// Always accompany a strength update with an explicit, finite
		// duration (issue #356). Without this the duration bytes are
		// whatever residue was last written into the shared buffer -
		// which used to be an accidental 0xFF (infinite) on USB and an
		// accidental, permanently time-capped 0xFE on Bluetooth. Matching
		// the real PS3 console's own behaviour (see
		// docs/PS3_USB_STARTUP.md) makes rumble time out on its own if the
		// keep-alive timer below (or the requesting application) ever
		// stops updating it.
		//
		DS3_USB_SET_LARGE_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), DS3_RUMBLE_DURATION_DEFAULT);
		DS3_USB_SET_SMALL_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), DS3_RUMBLE_DURATION_DEFAULT);
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

		//
		// See USB case above (issue #356).
		//
		DS3_BTH_SET_LARGE_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), DS3_RUMBLE_DURATION_DEFAULT);
		DS3_BTH_SET_SMALL_RUMBLE_DURATION(
			(PUCHAR)WdfMemoryGetBuffer(
				Context->OutputReportMemory,
				NULL
			), DS3_RUMBLE_DURATION_DEFAULT);
		break;
	}

	//
	// Arm (or restart) the keep-alive timer while at least one motor is
	// active, so the finite duration set above never times out for as
	// long as the requesting application keeps rumble engaged (issue
	// #356). Disarm it once both motors are silent - the next
	// DS3_PROCESS_RUMBLE_STRENGTH call (if any) re-arms it.
	//
	// IsTearingDown is checked here (rather than relying on
	// DsHidMini_EvtDeviceD0Exit/ReleaseHardware's WdfTimerStop alone) so a
	// rumble write racing with power-down on another thread cannot re-arm
	// the timer after teardown already decided to stop it.
	//
	if (!Context->RumbleControlState.IsTearingDown && ((UCHAR)heavyRumble != 0 || (UCHAR)lightRumble != 0))
	{
		WdfTimerStart(
			Context->RumbleControlState.RumbleKeepAliveTimer,
			WDF_REL_TIMEOUT_IN_MS(DS3_RUMBLE_KEEPALIVE_PERIOD_MS)
		);
	}
	else
	{
		WdfTimerStop(Context->RumbleControlState.RumbleKeepAliveTimer, FALSE);
	}
}

//
// Periodically re-sends the current output report while rumble is active,
// so the finite motor duration DS3_PROCESS_RUMBLE_STRENGTH now writes on
// every strength update (issue #356) never lapses as long as at least one
// motor stays engaged. Runs at PASSIVE_LEVEL (UMDF), so taking
// OutputReport.Lock here is safe - mirrors the lock/apply/send sequence
// DsBth_EvtStartupDelayTimerFunc already uses.
//
// Deliberately does not stop itself here even if both motors happen to
// read quiet: DS3_PROCESS_RUMBLE_STRENGTH is the sole authority that starts
// and stops this timer, and it always stops the timer itself once strength
// drops to zero. A callback-side stop based on this cache read (as this
// used to do) can race a fresh WdfTimerStart from a rumble write that
// arrives concurrently - cancelling a legitimately just-armed timer. Any
// stray invocation this races against instead just resends the current
// (by then already-zeroed) buffer once, which is harmless.
//
_Use_decl_annotations_
VOID
DS3_EvtRumbleKeepAliveTimerFunc(
	WDFTIMER Timer
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(WdfTimerGetParentObject(Timer));

	const NTSTATUS status = DSHM_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverHighPriority);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSHIDMINIDRV,
			"DSHM_SendOutputReport failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"DSHM_SendOutputReport", status);
	}

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}
