#pragma once

#define DS_IDENTIFICATION_REPORT_SIZE          64
#define DS_IDENTIFICATION_MIN_PARSE_SIZE       0x2A
#define DS_IDENTIFICATION_FIELD_COUNT_OFFSET   0x25
#define DS_IDENTIFICATION_FIELD_LIST_OFFSET    0x26
#define DS_IDENTIFICATION_CLONE_BYTE_OFFSET    0x29
#define DS_IDENTIFICATION_MAX_FIELDS           8

typedef enum _DS_IDENTIFICATION_MOTION_PATH
{
	DsIdentificationMotionPathUnknown = 0,
	DsIdentificationMotionPathPlainZero = 1,
	DsIdentificationMotionPathHwCal = 2,
	DsIdentificationMotionPathSixaxis = 3,
} DS_IDENTIFICATION_MOTION_PATH, * PDS_IDENTIFICATION_MOTION_PATH;

typedef struct _DS_IDENTIFICATION_INFO
{
	UCHAR Firmware[3];
	UINT32 FirmwarePacked;
	UCHAR PadType;
	DS_IDENTIFICATION_MOTION_PATH MotionPath;
	BOOLEAN CloneHeuristic;
	UCHAR FieldCount;
	UCHAR Fields[DS_IDENTIFICATION_MAX_FIELDS];
} DS_IDENTIFICATION_INFO, * PDS_IDENTIFICATION_INFO;

BOOLEAN
DsIdentification_Parse(
	_In_reads_(ReportLength) const UCHAR* Report,
	_In_ SIZE_T ReportLength,
	_Out_ PDS_IDENTIFICATION_INFO Info
);

VOID
DsIdentification_ResetDecodedProperties(
	_In_ WDFDEVICE Device
);

VOID
DsIdentification_AssignDeviceProperties(
	_In_ WDFDEVICE Device,
	_In_ PDS_IDENTIFICATION_INFO Info
);

VOID
DsIdentification_Clear(
	_In_ WDFDEVICE Device
);

VOID
DsIdentification_PublishFromReport(
	_In_ WDFDEVICE Device,
	_In_reads_(ReportLength) const UCHAR* Report,
	_In_ ULONG ReportLength
);
