#include "Driver.h"
#include "DsIdentification.tmh"

BOOLEAN
DsIdentification_Parse(
	_In_reads_(ReportLength) const UCHAR* Report,
	_In_ SIZE_T ReportLength,
	_Out_ PDS_IDENTIFICATION_INFO Info
)
{
	UCHAR fieldCount;
	SIZE_T fieldEnd;
	BOOLEAN hasHwCal = FALSE;
	BOOLEAN plainZero = FALSE;
	UCHAR i;

	if (Report == NULL || Info == NULL)
	{
		return FALSE;
	}

	RtlZeroMemory(Info, sizeof(*Info));

	if (ReportLength < DS_IDENTIFICATION_MIN_PARSE_SIZE)
	{
		return FALSE;
	}

	fieldCount = Report[DS_IDENTIFICATION_FIELD_COUNT_OFFSET];
	if (fieldCount == 0 || fieldCount > DS_IDENTIFICATION_MAX_FIELDS)
	{
		return FALSE;
	}

	fieldEnd = (SIZE_T)DS_IDENTIFICATION_FIELD_LIST_OFFSET + fieldCount;
	if (fieldEnd > ReportLength)
	{
		return FALSE;
	}

	Info->Firmware[0] = Report[2];
	Info->Firmware[1] = Report[3];
	Info->Firmware[2] = Report[4];
	Info->FirmwarePacked =
		((UINT32)Report[2] << 16) |
		((UINT32)Report[3] << 8) |
		(UINT32)Report[4];
	Info->PadType = Report[8];
	Info->FieldCount = fieldCount;

	for (i = 0; i < fieldCount; i++)
	{
		Info->Fields[i] = Report[DS_IDENTIFICATION_FIELD_LIST_OFFSET + i];
		if (Info->Fields[i] == 0x07)
		{
			hasHwCal = TRUE;
		}
	}

	if (fieldCount >= 2 &&
		Info->Fields[0] == 0x01 &&
		Info->Fields[1] == 0x02)
	{
		plainZero = TRUE;
	}
	else if (fieldCount >= 3 &&
		Info->Fields[1] == 0x01 &&
		Info->Fields[2] == 0x02)
	{
		plainZero = TRUE;
	}

	if (hasHwCal)
	{
		Info->MotionPath = DsIdentificationMotionPathHwCal;
	}
	else if (!plainZero)
	{
		Info->MotionPath = DsIdentificationMotionPathSixaxis;
	}
	else
	{
		Info->MotionPath = DsIdentificationMotionPathPlainZero;
	}

	if (fieldCount == 2 &&
		Info->Fields[0] == 0x01 &&
		Info->Fields[1] == 0x02 &&
		Report[DS_IDENTIFICATION_CLONE_BYTE_OFFSET] == 0x64)
	{
		Info->CloneHeuristic = TRUE;
	}

	return TRUE;
}

static VOID
DsIdentification_AssignProperty(
	_In_ WDFDEVICE Device,
	_In_ const DEVPROPKEY* Key,
	_In_ DEVPROPTYPE Type,
	_In_ ULONG Size,
	_In_opt_ PVOID Buffer,
	_In_ PCWSTR KeyName
)
{
	WDF_DEVICE_PROPERTY_DATA propertyData;
	NTSTATUS status;

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, Key);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	status = WdfDeviceAssignProperty(
		Device,
		&propertyData,
		Type,
		Size,
		Buffer
	);

	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSUSB,
			"WdfDeviceAssignProperty(%ls) failed with %!STATUS!",
			KeyName,
			status
		);
	}
}

VOID
DsIdentification_ResetDecodedProperties(
	_In_ WDFDEVICE Device
)
{
	//
	// BufferLength 0 deletes a previously persisted value so a failed GET or
	// parse cannot leave stale decoded keys from an earlier power-up.
	// 
	DsIdentification_AssignProperty(
		Device,
		&DEVPKEY_DsHidMini_RO_IdentificationFirmware,
		DEVPROP_TYPE_UINT32,
		0,
		NULL,
		L"DEVPKEY_DsHidMini_RO_IdentificationFirmware"
	);
	DsIdentification_AssignProperty(
		Device,
		&DEVPKEY_DsHidMini_RO_IdentificationPadType,
		DEVPROP_TYPE_BYTE,
		0,
		NULL,
		L"DEVPKEY_DsHidMini_RO_IdentificationPadType"
	);
	DsIdentification_AssignProperty(
		Device,
		&DEVPKEY_DsHidMini_RO_IdentificationMotionPath,
		DEVPROP_TYPE_BYTE,
		0,
		NULL,
		L"DEVPKEY_DsHidMini_RO_IdentificationMotionPath"
	);
	DsIdentification_AssignProperty(
		Device,
		&DEVPKEY_DsHidMini_RO_IdentificationCloneHeuristic,
		DEVPROP_TYPE_BOOLEAN,
		0,
		NULL,
		L"DEVPKEY_DsHidMini_RO_IdentificationCloneHeuristic"
	);
}

VOID
DsIdentification_AssignDeviceProperties(
	_In_ WDFDEVICE Device,
	_In_ PDS_IDENTIFICATION_INFO Info
)
{
	UCHAR padType;
	UCHAR motionPath;
	DEVPROP_BOOLEAN cloneHeuristic;

	if (Info == NULL)
	{
		return;
	}

	DsIdentification_AssignProperty(
		Device,
		&DEVPKEY_DsHidMini_RO_IdentificationFirmware,
		DEVPROP_TYPE_UINT32,
		sizeof(UINT32),
		&Info->FirmwarePacked,
		L"DEVPKEY_DsHidMini_RO_IdentificationFirmware"
	);

	padType = Info->PadType;
	DsIdentification_AssignProperty(
		Device,
		&DEVPKEY_DsHidMini_RO_IdentificationPadType,
		DEVPROP_TYPE_BYTE,
		sizeof(UCHAR),
		&padType,
		L"DEVPKEY_DsHidMini_RO_IdentificationPadType"
	);

	motionPath = (UCHAR)Info->MotionPath;
	DsIdentification_AssignProperty(
		Device,
		&DEVPKEY_DsHidMini_RO_IdentificationMotionPath,
		DEVPROP_TYPE_BYTE,
		sizeof(UCHAR),
		&motionPath,
		L"DEVPKEY_DsHidMini_RO_IdentificationMotionPath"
	);

	cloneHeuristic = Info->CloneHeuristic ? DEVPROP_TRUE : DEVPROP_FALSE;
	DsIdentification_AssignProperty(
		Device,
		&DEVPKEY_DsHidMini_RO_IdentificationCloneHeuristic,
		DEVPROP_TYPE_BOOLEAN,
		sizeof(DEVPROP_BOOLEAN),
		&cloneHeuristic,
		L"DEVPKEY_DsHidMini_RO_IdentificationCloneHeuristic"
	);
}

VOID
DsIdentification_Clear(
	_In_ WDFDEVICE Device
)
{
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	pDevCtx->IdentificationPresent = FALSE;
	RtlZeroMemory(&pDevCtx->Identification, sizeof(pDevCtx->Identification));
	DsIdentification_ResetDecodedProperties(Device);
	DsIdentification_AssignProperty(
		Device,
		&DEVPKEY_DsHidMini_RO_IdentificationData,
		DEVPROP_TYPE_BINARY,
		0,
		NULL,
		L"DEVPKEY_DsHidMini_RO_IdentificationData"
	);
}

VOID
DsIdentification_PublishFromReport(
	_In_ WDFDEVICE Device,
	_In_reads_(ReportLength) const UCHAR* Report,
	_In_ ULONG ReportLength
)
{
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	WDF_DEVICE_PROPERTY_DATA propertyData;
	ULONG length = ReportLength;

	if (Report == NULL || length == 0)
	{
		return;
	}

	if (length > DS_IDENTIFICATION_REPORT_SIZE)
	{
		length = DS_IDENTIFICATION_REPORT_SIZE;
	}

	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_IdentificationData);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	(void)WdfDeviceAssignProperty(
		Device,
		&propertyData,
		DEVPROP_TYPE_BINARY,
		length,
		(PVOID)Report
	);

	if (DsIdentification_Parse(Report, length, &pDevCtx->Identification))
	{
		pDevCtx->IdentificationPresent = TRUE;
		DsIdentification_AssignDeviceProperties(Device, &pDevCtx->Identification);

		TraceVerbose(
			TRACE_DSUSB,
			"Feature 0x01 firmware %02X %02X %02X type %02X path %d clone %!BOOLEAN!",
			pDevCtx->Identification.Firmware[0],
			pDevCtx->Identification.Firmware[1],
			pDevCtx->Identification.Firmware[2],
			pDevCtx->Identification.PadType,
			pDevCtx->Identification.MotionPath,
			pDevCtx->Identification.CloneHeuristic
		);
	}
	else
	{
		pDevCtx->IdentificationPresent = FALSE;
		TraceWarning(
			TRACE_DSUSB,
			"Feature 0x01 identification blob could not be parsed"
		);
	}
}
