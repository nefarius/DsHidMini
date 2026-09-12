#pragma once

#define DS_MOTION_ACCEL_GAIN                 113
#define DS_MOTION_NOMINAL_ZERO               512
#define DS_MOTION_NOMINAL_ONE_G              (DS_MOTION_NOMINAL_ZERO - DS_MOTION_ACCEL_GAIN)
#define DS_MOTION_GYRO_STEP_Q10              0x6999
#define DS_MOTION_EEPROM_PAGE                0xA0
#define DS_MOTION_EEPROM_PAYLOAD_OFFSET      0x11
#define DS_MOTION_IPC_SNAPSHOT_VERSION       1

#define DSHM_IPC_MOTION_FLAG_AVAILABLE       0x0001
#define DSHM_IPC_MOTION_FLAG_FALLBACK        0x0002
#define DSHM_IPC_MOTION_FLAG_HW_CAL          0x0004
#define DSHM_IPC_MOTION_FLAG_TRACKER         0x0008
#define DSHM_IPC_MOTION_FLAG_SOFT_ZERO       0x0010

#define Ds3FeatureEeprom                     0x03EF

typedef struct _DEVICE_CONTEXT DEVICE_CONTEXT, *PDEVICE_CONTEXT;

typedef struct _DS_MOTION_AXIS_CAL
{
	USHORT Zero;
	USHORT OneG;
} DS_MOTION_AXIS_CAL, *PDS_MOTION_AXIS_CAL;

typedef struct _DS_GYRO_TRACKER
{
	INT32 CalByte;
	INT32 ZeroRef;
	INT32 LastRaw;
	INT32 Output;
	INT32 SettleLeft;
	BOOLEAN Moving;
	INT32 BlockCount;
	INT32 BlockSum;
	INT32 BlockMin;
	INT32 BlockMax;
	INT32 LongCount;
	INT32 LongSum;
	INT32 Ring[4];
	INT32 RingFilled;
	INT32 RingIdx;
	INT32 RingSum;
	BOOLEAN Pending;
	INT32 PendingCal;
	INT32 PendingZero;
	INT32 PendingTol;
	BOOLEAN Initialized;
	BOOLEAN SoftwareOnly;
} DS_GYRO_TRACKER, *PDS_GYRO_TRACKER;

typedef struct _DS_MOTION_SAMPLE
{
	BOOLEAN Valid;
	USHORT RawAccelX;
	USHORT RawAccelY;
	USHORT RawAccelZ;
	USHORT RawGyro;
	SHORT CalAccelX;
	SHORT CalAccelY;
	SHORT CalAccelZ;
	USHORT CalGyro;
	INT32 AccelMilliGX;
	INT32 AccelMilliGY;
	INT32 AccelMilliGZ;
	INT32 GyroMilliDps;
	UINT32 SampleIndex;
	LARGE_INTEGER TimestampQpc;
} DS_MOTION_SAMPLE, *PDS_MOTION_SAMPLE;

typedef struct _DS_MOTION_STATE
{
	BOOLEAN Fallback;
	BOOLEAN SendHardwareCal;
	BOOLEAN HasSample;
	DS_IDENTIFICATION_MOTION_PATH Path;
	DS_MOTION_AXIS_CAL Accel[3];
	DS_MOTION_AXIS_CAL Gyro;
	DS_GYRO_TRACKER Tracker;
	DS_MOTION_SAMPLE Sample;
} DS_MOTION_STATE, *PDS_MOTION_STATE;

#include <pshpack1.h>
typedef struct _IPC_MOTION_SNAPSHOT_MESSAGE
{
	UINT32 SlotIndex;
	volatile LONG SequenceNumber;
	UINT16 Version;
	UINT16 Flags;
	USHORT AccelZeroX;
	USHORT AccelZeroY;
	USHORT AccelZeroZ;
	USHORT AccelOneGX;
	USHORT AccelOneGY;
	USHORT AccelOneGZ;
	USHORT GyroZero;
	USHORT GyroEepromCal;
	USHORT RawAccelX;
	USHORT RawAccelY;
	USHORT RawAccelZ;
	USHORT RawGyro;
	SHORT CalAccelX;
	SHORT CalAccelY;
	SHORT CalAccelZ;
	USHORT CalGyro;
	INT32 AccelMilliGX;
	INT32 AccelMilliGY;
	INT32 AccelMilliGZ;
	INT32 GyroMilliDps;
	INT32 ZeroRef;
	UINT32 SampleIndex;
	UINT64 TimestampQpc;
	UCHAR CalByte;
	UCHAR MotionPath;
	UCHAR Reserved0;
	UCHAR Reserved1;
} IPC_MOTION_SNAPSHOT_MESSAGE, *PIPC_MOTION_SNAPSHOT_MESSAGE;
#include <poppack.h>

C_ASSERT(sizeof(IPC_MOTION_SNAPSHOT_MESSAGE) == 80);
C_ASSERT((FIELD_OFFSET(IPC_MOTION_SNAPSHOT_MESSAGE, SequenceNumber) % sizeof(LONG)) == 0);
C_ASSERT((sizeof(IPC_MOTION_SNAPSHOT_MESSAGE) % sizeof(LONG)) == 0);

VOID
DsMotion_Initialize(
	_Inout_ PDS_MOTION_STATE Motion
);

VOID
DsMotion_TryLoadUsbCalibration(
	_In_ WDFDEVICE Device
);

VOID
DsMotion_ProcessInputReport(
	_In_ PDEVICE_CONTEXT Context,
	_In_ const PDS3_RAW_INPUT_REPORT Report,
	_Out_ PBOOLEAN CalByteChanged
);

VOID
DsMotion_ApplyOutputCalByte(
	_In_ PDEVICE_CONTEXT Context
);

VOID
DsMotion_OverlayCalByteOnUnified(
	_In_ PDEVICE_CONTEXT Context,
	_Inout_updates_(Length) PUCHAR Unified,
	_In_ ULONG Length
);

VOID
DsMotion_FillIpcSnapshot(
	_In_ PDEVICE_CONTEXT Context,
	_Out_ PIPC_MOTION_SNAPSHOT_MESSAGE Snapshot
);

VOID
DsMotion_PublishOrClearIpcSnapshot(
	_In_ PDEVICE_CONTEXT Context,
	_In_ BOOLEAN Clear
);
