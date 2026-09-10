#include "Driver.h"
#include "DsMotion.tmh"

#define DS_MOTION_TARGET                 512
#define DS_MOTION_SETTLE_INITIAL         32
#define DS_MOTION_SETTLE_AFTER_CAL       2
#define DS_MOTION_RAW_MAX                0x333
#define DS_MOTION_RAW_MIN                0xCC
#define DS_MOTION_BLOCK_N                16
#define DS_MOTION_BLOCK_VAR_MAX          10
#define DS_MOTION_RING_N                 4
#define DS_MOTION_RING_RANGE_MAX         4
#define DS_MOTION_LONG_N                 0xEC
#define DS_MOTION_PENDING_TOL0           100
#define DS_MOTION_INT_MIN                ((INT32)0x80000000)
#define DS_MOTION_INT_MAX                ((INT32)0x7FFFFFFF)

static
INT32
DsMotion_Q10(
	_In_ INT32 Value
)
{
	return (Value + ((Value >> 31) & 0x3FF)) >> 10;
}

static
INT32
DsMotion_Clamp10(
	_In_ INT32 Value
)
{
	if (Value < 0)
	{
		return 0;
	}

	if (Value > 1023)
	{
		return 1023;
	}

	return Value;
}

static
INT32
DsMotion_CalibrateAccelAxis(
	_In_ INT32 Raw,
	_In_ USHORT Zero,
	_In_ USHORT OneG,
	_In_ BOOLEAN Mirror
)
{
	INT32 cal;
	INT32 t;

	if (Zero != OneG)
	{
		t = ((Raw - (INT32)Zero) * 1024 / ((INT32)Zero - (INT32)OneG)) * DS_MOTION_ACCEL_GAIN;
		cal = DsMotion_Q10(t) + DS_MOTION_NOMINAL_ZERO;
	}
	else
	{
		cal = Raw;
	}

	if (Mirror)
	{
		cal = 0x3FF - cal;
	}

	return cal;
}

static
INT32
DsMotion_ToMilliG(
	_In_ INT32 Calibrated
)
{
	return ((Calibrated - DS_MOTION_NOMINAL_ZERO) * 1000) / DS_MOTION_ACCEL_GAIN;
}

static
INT32
DsMotion_ToMilliDps(
	_In_ INT32 Calibrated
)
{
	//
	// ~1.4 counts per deg/s (docs/MOTION.md). (cal - 512) * 1000 / 1.4
	// = (cal - 512) * 5000 / 7
	// 
	return ((Calibrated - DS_MOTION_NOMINAL_ZERO) * 5000) / 7;
}

static
VOID
DsMotion_TrackerResetBlock(
	_Inout_ PDS_GYRO_TRACKER Tracker
)
{
	Tracker->BlockCount = 0;
	Tracker->BlockSum = 0;
	Tracker->BlockMin = DS_MOTION_INT_MAX;
	Tracker->BlockMax = DS_MOTION_INT_MIN;
}

static
VOID
DsMotion_TrackerRingReset(
	_Inout_ PDS_GYRO_TRACKER Tracker
)
{
	RtlZeroMemory(Tracker->Ring, sizeof(Tracker->Ring));
	Tracker->RingFilled = 0;
	Tracker->RingIdx = 0;
	Tracker->RingSum = 0;
}

static
BOOLEAN
DsMotion_TrackerRetarget(
	_Inout_ PDS_GYRO_TRACKER Tracker,
	_In_ INT32 RestAvg,
	_Out_ PINT32 ZeroRef,
	_Out_ PINT32 CalByte
)
{
	const INT32 delta = (DS_MOTION_TARGET - RestAvg) * 1024;

	if (delta >= -DS_MOTION_GYRO_STEP_Q10 && delta <= DS_MOTION_GYRO_STEP_Q10)
	{
		*ZeroRef = RestAvg;
		*CalByte = Tracker->CalByte;
		return FALSE;
	}

	{
		const INT32 steps = delta / DS_MOTION_GYRO_STEP_Q10;
		*ZeroRef = RestAvg + DsMotion_Q10(steps * DS_MOTION_GYRO_STEP_Q10);
		*CalByte = Tracker->CalByte + steps;
		return TRUE;
	}
}

static
BOOLEAN
DsMotion_TrackerRingPush(
	_Inout_ PDS_GYRO_TRACKER Tracker,
	_In_ INT32 Value
)
{
	const INT32 old = Tracker->Ring[Tracker->RingIdx];

	Tracker->Ring[Tracker->RingIdx] = Value;
	Tracker->RingSum += Value;
	Tracker->RingIdx++;

	if (Tracker->RingFilled < DS_MOTION_RING_N)
	{
		Tracker->RingFilled++;
		if (Tracker->RingFilled < DS_MOTION_RING_N)
		{
			if (Tracker->RingIdx == DS_MOTION_RING_N)
			{
				Tracker->RingIdx = 0;
			}

			return FALSE;
		}
	}
	else
	{
		Tracker->RingSum -= old;
	}

	if (Tracker->RingIdx == DS_MOTION_RING_N)
	{
		Tracker->RingIdx = 0;
	}

	return TRUE;
}

static
INT32
DsMotion_TrackerRingRange(
	_In_ const PDS_GYRO_TRACKER Tracker
)
{
	INT32 minValue = DS_MOTION_INT_MAX;
	INT32 maxValue = DS_MOTION_INT_MIN;
	INT32 i;

	for (i = 0; i < DS_MOTION_RING_N; i++)
	{
		if (Tracker->Ring[i] > maxValue)
		{
			maxValue = Tracker->Ring[i];
		}

		if (Tracker->Ring[i] < minValue)
		{
			minValue = Tracker->Ring[i];
		}
	}

	return maxValue - minValue;
}

static
VOID
DsMotion_TrackerApplyPending(
	_Inout_ PDS_GYRO_TRACKER Tracker
)
{
	Tracker->ZeroRef = Tracker->PendingZero;
	Tracker->CalByte = Tracker->PendingCal;
	Tracker->Pending = FALSE;
	DsMotion_TrackerRingReset(Tracker);
	Tracker->LongCount = 0;
	Tracker->LongSum = 0;
}

static
BOOLEAN
DsMotion_TrackerTrack(
	_Inout_ PDS_GYRO_TRACKER Tracker,
	_In_ INT32 Raw
)
{
	INT32 blockAvg;
	INT32 lastMin;
	INT32 lastMax;
	INT32 var;

	if (Raw > DS_MOTION_RAW_MAX || Raw < DS_MOTION_RAW_MIN)
	{
		Tracker->Moving = TRUE;
	}

	Tracker->BlockSum += Raw;
	if (Raw > Tracker->BlockMax)
	{
		Tracker->BlockMax = Raw;
	}

	if (Raw < Tracker->BlockMin)
	{
		Tracker->BlockMin = Raw;
	}

	if (++Tracker->BlockCount != DS_MOTION_BLOCK_N)
	{
		return FALSE;
	}

	Tracker->BlockCount = 0;
	blockAvg = (Tracker->BlockSum + DS_MOTION_BLOCK_N / 2) / DS_MOTION_BLOCK_N;
	Tracker->BlockSum = 0;
	lastMin = Tracker->BlockMin;
	lastMax = Tracker->BlockMax;
	Tracker->BlockMin = DS_MOTION_INT_MAX;
	Tracker->BlockMax = DS_MOTION_INT_MIN;
	var = (lastMax - blockAvg) * (lastMax - blockAvg) +
		(blockAvg - lastMin) * (blockAvg - lastMin);

	Tracker->LongSum += blockAvg;
	if (++Tracker->LongCount == DS_MOTION_LONG_N)
	{
		const INT32 longAvg = (Tracker->LongSum + DS_MOTION_LONG_N / 2) / DS_MOTION_LONG_N;
		INT32 zeroRef;
		INT32 calByte;

		Tracker->LongCount = 0;
		Tracker->LongSum = 0;

		if (DsMotion_TrackerRetarget(Tracker, longAvg, &zeroRef, &calByte))
		{
			Tracker->PendingZero = zeroRef;
			Tracker->PendingCal = calByte;
			Tracker->Pending = TRUE;
			Tracker->PendingTol = DS_MOTION_PENDING_TOL0;
		}
		else
		{
			Tracker->PendingZero = zeroRef;
			Tracker->ZeroRef = longAvg;
		}
	}

	if (Tracker->Moving)
	{
		Tracker->Moving = FALSE;
		return FALSE;
	}

	if (var < DS_MOTION_BLOCK_VAR_MAX)
	{
		if (DsMotion_TrackerRingPush(Tracker, blockAvg))
		{
			if (DsMotion_TrackerRingRange(Tracker) < DS_MOTION_RING_RANGE_MAX)
			{
				const INT32 restAvg = (Tracker->RingSum + DS_MOTION_RING_N / 2) / DS_MOTION_RING_N;
				INT32 newCal;
				BOOLEAN changed;

				Tracker->LongCount = 0;
				Tracker->LongSum = 0;
				changed = DsMotion_TrackerRetarget(
					Tracker,
					restAvg,
					&Tracker->ZeroRef,
					&newCal
				);

				if (changed)
				{
					Tracker->CalByte = newCal;
					DsMotion_TrackerRingReset(Tracker);
					Tracker->SettleLeft = DS_MOTION_SETTLE_AFTER_CAL;
				}

				Tracker->Pending = FALSE;
				return changed;
			}
		}

		if (Tracker->Pending)
		{
			DsMotion_TrackerApplyPending(Tracker);
			Tracker->SettleLeft = DS_MOTION_SETTLE_AFTER_CAL;
			return TRUE;
		}
	}

	return FALSE;
}

static
VOID
DsMotion_TrackerInitial(
	_Inout_ PDS_GYRO_TRACKER Tracker,
	_In_ USHORT EepromCal,
	_In_ USHORT EepromZero
)
{
	RtlZeroMemory(Tracker, sizeof(*Tracker));
	Tracker->CalByte = (INT32)EepromCal;
	Tracker->LastRaw = (INT32)EepromZero;
	Tracker->SettleLeft = DS_MOTION_SETTLE_INITIAL;
	Tracker->BlockMin = DS_MOTION_INT_MAX;
	Tracker->BlockMax = DS_MOTION_INT_MIN;
	DsMotion_TrackerRetarget(
		Tracker,
		(INT32)EepromZero,
		&Tracker->ZeroRef,
		&Tracker->CalByte
	);
	Tracker->Output = DsMotion_Clamp10(Tracker->ZeroRef - (INT32)EepromZero + DS_MOTION_TARGET);
	Tracker->Initialized = TRUE;
}

static
INT32
DsMotion_TrackerRuntime(
	_Inout_ PDS_GYRO_TRACKER Tracker,
	_In_ INT32 Raw,
	_Out_ PBOOLEAN CalChanged
)
{
	*CalChanged = FALSE;

	if (!Tracker->Initialized)
	{
		return DS_MOTION_TARGET;
	}

	if (Tracker->SettleLeft > 0)
	{
		Tracker->SettleLeft--;
		Tracker->LastRaw = Raw;
		return Tracker->Output;
	}

	Tracker->Output = DsMotion_Clamp10(DS_MOTION_TARGET - Raw + Tracker->ZeroRef);
	*CalChanged = DsMotion_TrackerTrack(Tracker, Raw);

	if (Tracker->Pending)
	{
		const INT32 jump = Raw - Tracker->LastRaw;

		if (jump >= -Tracker->PendingTol && jump <= Tracker->PendingTol)
		{
			Tracker->PendingTol -= 1;
		}
		else
		{
			DsMotion_TrackerApplyPending(Tracker);
			DsMotion_TrackerResetBlock(Tracker);
			*CalChanged = TRUE;
		}
	}

	Tracker->LastRaw = Raw;
	return Tracker->Output;
}

static
DS_IDENTIFICATION_MOTION_PATH
DsMotion_ResolvePath(
	_In_ const PDEVICE_CONTEXT Context
)
{
	if (Context->IdentificationPresent &&
		Context->Identification.MotionPath != DsIdentificationMotionPathUnknown)
	{
		return Context->Identification.MotionPath;
	}

	return DsIdentificationMotionPathPlainZero;
}

static
UCHAR
DsMotion_CurrentCalByte(
	_In_ const PDS_MOTION_STATE Motion
)
{
	if (Motion->Tracker.Initialized)
	{
		return (UCHAR)Motion->Tracker.CalByte;
	}

	return (UCHAR)Motion->Gyro.OneG;
}

VOID
DsMotion_Initialize(
	_Inout_ PDS_MOTION_STATE Motion
)
{
	INT32 i;

	RtlZeroMemory(Motion, sizeof(*Motion));
	Motion->Fallback = TRUE;
	Motion->Path = DsIdentificationMotionPathUnknown;

	for (i = 0; i < 3; i++)
	{
		Motion->Accel[i].Zero = DS_MOTION_NOMINAL_ZERO;
		Motion->Accel[i].OneG = DS_MOTION_NOMINAL_ONE_G;
	}

	Motion->Gyro.Zero = DS_MOTION_NOMINAL_ZERO;
	Motion->Gyro.OneG = 0;
}

static
BOOLEAN
DsMotion_ParseEepromPage(
	_In_reads_(BufferLength) const UCHAR* Buffer,
	_In_ ULONG BufferLength,
	_Out_ PDS_MOTION_STATE Motion
)
{
	const UCHAR* payload;
	INT32 i;

	if (Buffer == NULL || Motion == NULL ||
		BufferLength < DS_MOTION_EEPROM_PAYLOAD_OFFSET + 16)
	{
		return FALSE;
	}

	//
	// The GET Feature 0xEF reply echoes the page select at offsets 5-7.
	// Reject a mismatched page so a stale buffer cannot look like A0 data.
	// 
	if (BufferLength > 7 && Buffer[7] != 0 && Buffer[7] != DS_MOTION_EEPROM_PAGE)
	{
		return FALSE;
	}

	payload = &Buffer[DS_MOTION_EEPROM_PAYLOAD_OFFSET];

	for (i = 0; i < 3; i++)
	{
		Motion->Accel[i].Zero = (USHORT)((payload[i * 4] << 8) | payload[i * 4 + 1]);
		Motion->Accel[i].OneG = (USHORT)((payload[i * 4 + 2] << 8) | payload[i * 4 + 3]);
	}

	Motion->Gyro.Zero = (USHORT)((payload[12] << 8) | payload[13]);
	Motion->Gyro.OneG = (USHORT)((payload[14] << 8) | payload[15]);
	return TRUE;
}

VOID
DsMotion_TryLoadUsbCalibration(
	_In_ WDFDEVICE Device
)
{
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);
	UCHAR select[48];
	UCHAR page[CONTROL_TRANSFER_BUFFER_LENGTH];
	ULONG transferred = 0;
	NTSTATUS status;

	FuncEntry(TRACE_MOTION);

	pDevCtx->Motion.Path = DsMotion_ResolvePath(pDevCtx);

	RtlZeroMemory(select, sizeof(select));
	select[4] = 0x03;
	select[5] = 0x01;
	select[6] = DS_MOTION_EEPROM_PAGE;

	status = USB_SendControlRequest(
		pDevCtx,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		Ds3FeatureEeprom,
		0,
		select,
		ARRAYSIZE(select),
		NULL
	);

	if (!NT_SUCCESS(status))
	{
		TraceWarning(
			TRACE_MOTION,
			"SET Feature 0xEF page 0xA0 failed with %!STATUS!; using nominal calibration",
			status
		);
		FuncExitNoReturn(TRACE_MOTION);
		return;
	}

	RtlZeroMemory(page, sizeof(page));
	status = USB_SendControlRequest(
		pDevCtx,
		BmRequestDeviceToHost,
		BmRequestClass,
		GetReport,
		Ds3FeatureEeprom,
		0,
		page,
		ARRAYSIZE(page),
		&transferred
	);

	if (!NT_SUCCESS(status) ||
		!DsMotion_ParseEepromPage(page, transferred, &pDevCtx->Motion))
	{
		TraceWarning(
			TRACE_MOTION,
			"GET Feature 0xEF page 0xA0 failed (status %!STATUS!, %lu bytes); using nominal calibration",
			status,
			transferred
		);
		FuncExitNoReturn(TRACE_MOTION);
		return;
	}

	pDevCtx->Motion.Fallback = FALSE;
	pDevCtx->Motion.Path = DsMotion_ResolvePath(pDevCtx);

	if (pDevCtx->Motion.Path == DsIdentificationMotionPathHwCal ||
		pDevCtx->Motion.Path == DsIdentificationMotionPathSixaxis)
	{
		DsMotion_TrackerInitial(
			&pDevCtx->Motion.Tracker,
			pDevCtx->Motion.Gyro.OneG,
			pDevCtx->Motion.Gyro.Zero
		);
		pDevCtx->Motion.SendHardwareCal = TRUE;
		DsMotion_ApplyOutputCalByte(pDevCtx);
	}

	TraceInformation(
		TRACE_MOTION,
		"EEPROM 0xA0 X=%u/%u Y=%u/%u Z=%u/%u G=%u/%u path=%d fallback=%!BOOLEAN!",
		pDevCtx->Motion.Accel[0].Zero,
		pDevCtx->Motion.Accel[0].OneG,
		pDevCtx->Motion.Accel[1].Zero,
		pDevCtx->Motion.Accel[1].OneG,
		pDevCtx->Motion.Accel[2].Zero,
		pDevCtx->Motion.Accel[2].OneG,
		pDevCtx->Motion.Gyro.Zero,
		pDevCtx->Motion.Gyro.OneG,
		pDevCtx->Motion.Path,
		pDevCtx->Motion.Fallback
	);

	FuncExitNoReturn(TRACE_MOTION);
}

VOID
DsMotion_ApplyOutputCalByte(
	_In_ PDEVICE_CONTEXT Context
)
{
	PUCHAR raw = NULL;
	SIZE_T length = 0;
	const UCHAR cal = DsMotion_CurrentCalByte(&Context->Motion);

	if (!Context->Motion.SendHardwareCal ||
		Context->ConnectionType != DsDeviceConnectionTypeUsb)
	{
		return;
	}

	Ds3_GetRawOutputReportBuffer(Context, &raw, &length);
	if (raw == NULL)
	{
		return;
	}

	if (Context->Motion.Path == DsIdentificationMotionPathHwCal && length > 7)
	{
		raw[6] = 0xFF;
		raw[7] = cal;
	}
	else if (Context->Motion.Path == DsIdentificationMotionPathSixaxis && length > 5)
	{
		raw[4] = 0xFF;
		raw[5] = cal;
	}
}

VOID
DsMotion_OverlayCalByteOnUnified(
	_In_ PDEVICE_CONTEXT Context,
	_Inout_updates_(Length) PUCHAR Unified,
	_In_ ULONG Length
)
{
	const UCHAR cal = DsMotion_CurrentCalByte(&Context->Motion);

	if (!Context->Motion.SendHardwareCal || Unified == NULL)
	{
		return;
	}

	if (Context->Motion.Path == DsIdentificationMotionPathHwCal && Length > 6)
	{
		Unified[5] = 0xFF;
		Unified[6] = cal;
	}
	else if (Context->Motion.Path == DsIdentificationMotionPathSixaxis && Length > 4)
	{
		Unified[3] = 0xFF;
		Unified[4] = cal;
	}
}

VOID
DsMotion_ProcessInputReport(
	_In_ PDEVICE_CONTEXT Context,
	_In_ const PDS3_RAW_INPUT_REPORT Report,
	_Out_ PBOOLEAN CalByteChanged
)
{
	PDS_MOTION_STATE motion = &Context->Motion;
	INT32 raw[4];
	INT32 calAccel[3];
	INT32 calGyro;
	BOOLEAN trackerChanged = FALSE;
	INT32 i;

	*CalByteChanged = FALSE;

	if (motion->Path == DsIdentificationMotionPathUnknown)
	{
		motion->Path = DsMotion_ResolvePath(Context);
	}

	raw[0] = _byteswap_ushort(Report->AccelerometerX);
	raw[1] = _byteswap_ushort(Report->AccelerometerY);
	raw[2] = _byteswap_ushort(Report->AccelerometerZ);
	raw[3] = _byteswap_ushort(Report->Gyroscope);

	for (i = 0; i < 3; i++)
	{
		calAccel[i] = DsMotion_CalibrateAccelAxis(
			raw[i],
			motion->Accel[i].Zero,
			motion->Accel[i].OneG,
			i == 0
		);
	}

	switch (motion->Path)
	{
	case DsIdentificationMotionPathHwCal:
		calGyro = DsMotion_Clamp10(0x3FF - raw[3]);
		if (motion->Tracker.Initialized)
		{
			(void)DsMotion_TrackerRuntime(&motion->Tracker, raw[3], &trackerChanged);
		}
		break;

	case DsIdentificationMotionPathSixaxis:
		if (motion->Tracker.Initialized)
		{
			calGyro = DsMotion_TrackerRuntime(&motion->Tracker, raw[3], &trackerChanged);
		}
		else
		{
			calGyro = DsMotion_Clamp10(DS_MOTION_TARGET + (INT32)motion->Gyro.Zero - raw[3]);
		}
		break;

	case DsIdentificationMotionPathPlainZero:
	default:
		calGyro = DsMotion_Clamp10(DS_MOTION_TARGET + (INT32)motion->Gyro.Zero - raw[3]);
		break;
	}

	motion->Sample.Valid = TRUE;
	motion->Sample.RawAccelX = (USHORT)raw[0];
	motion->Sample.RawAccelY = (USHORT)raw[1];
	motion->Sample.RawAccelZ = (USHORT)raw[2];
	motion->Sample.RawGyro = (USHORT)raw[3];
	motion->Sample.CalAccelX = (SHORT)calAccel[0];
	motion->Sample.CalAccelY = (SHORT)calAccel[1];
	motion->Sample.CalAccelZ = (SHORT)calAccel[2];
	motion->Sample.CalGyro = (USHORT)calGyro;
	motion->Sample.AccelMilliGX = DsMotion_ToMilliG(calAccel[0]);
	motion->Sample.AccelMilliGY = DsMotion_ToMilliG(calAccel[1]);
	motion->Sample.AccelMilliGZ = DsMotion_ToMilliG(calAccel[2]);
	motion->Sample.GyroMilliDps = DsMotion_ToMilliDps(calGyro);
	motion->Sample.SampleIndex++;
	QueryPerformanceCounter(&motion->Sample.TimestampQpc);
	motion->HasSample = TRUE;

	*CalByteChanged = trackerChanged && motion->SendHardwareCal;
}

VOID
DsMotion_FillIpcSnapshot(
	_In_ PDEVICE_CONTEXT Context,
	_Out_ PIPC_MOTION_SNAPSHOT_MESSAGE Snapshot
)
{
	const PDS_MOTION_STATE motion = &Context->Motion;
	UINT16 flags = 0;

	RtlZeroMemory(Snapshot, sizeof(*Snapshot));
	Snapshot->SlotIndex = Context->SlotIndex;
	Snapshot->Version = DS_MOTION_IPC_SNAPSHOT_VERSION;
	Snapshot->AccelZeroX = motion->Accel[0].Zero;
	Snapshot->AccelZeroY = motion->Accel[1].Zero;
	Snapshot->AccelZeroZ = motion->Accel[2].Zero;
	Snapshot->AccelOneGX = motion->Accel[0].OneG;
	Snapshot->AccelOneGY = motion->Accel[1].OneG;
	Snapshot->AccelOneGZ = motion->Accel[2].OneG;
	Snapshot->GyroZero = motion->Gyro.Zero;
	Snapshot->GyroEepromCal = motion->Gyro.OneG;
	Snapshot->CalByte = DsMotion_CurrentCalByte(motion);
	Snapshot->MotionPath = (UCHAR)motion->Path;
	Snapshot->ZeroRef = motion->Tracker.Initialized
		? motion->Tracker.ZeroRef
		: (INT32)motion->Gyro.Zero;

	if (motion->HasSample)
	{
		flags |= DSHM_IPC_MOTION_FLAG_AVAILABLE;
		Snapshot->RawAccelX = motion->Sample.RawAccelX;
		Snapshot->RawAccelY = motion->Sample.RawAccelY;
		Snapshot->RawAccelZ = motion->Sample.RawAccelZ;
		Snapshot->RawGyro = motion->Sample.RawGyro;
		Snapshot->CalAccelX = motion->Sample.CalAccelX;
		Snapshot->CalAccelY = motion->Sample.CalAccelY;
		Snapshot->CalAccelZ = motion->Sample.CalAccelZ;
		Snapshot->CalGyro = motion->Sample.CalGyro;
		Snapshot->AccelMilliGX = motion->Sample.AccelMilliGX;
		Snapshot->AccelMilliGY = motion->Sample.AccelMilliGY;
		Snapshot->AccelMilliGZ = motion->Sample.AccelMilliGZ;
		Snapshot->GyroMilliDps = motion->Sample.GyroMilliDps;
		Snapshot->SampleIndex = motion->Sample.SampleIndex;
		Snapshot->TimestampQpc = (UINT64)motion->Sample.TimestampQpc.QuadPart;
	}

	if (motion->Fallback)
	{
		flags |= DSHM_IPC_MOTION_FLAG_FALLBACK;
	}

	if (motion->SendHardwareCal)
	{
		flags |= DSHM_IPC_MOTION_FLAG_HW_CAL;
	}

	if (motion->Tracker.Initialized)
	{
		flags |= DSHM_IPC_MOTION_FLAG_TRACKER;
	}

	Snapshot->Flags = flags;
}

VOID
DsMotion_PublishOrClearIpcSnapshot(
	_In_ PDEVICE_CONTEXT Context,
	_In_ BOOLEAN Clear
)
{
	const WDFDRIVER driver = WdfGetDriver();
	const PDSHM_DRIVER_CONTEXT pDrvCtx = DriverGetContext(driver);
	PIPC_MOTION_SNAPSHOT_MESSAGE slot;
	size_t offset;

	if (pDrvCtx->IPC.SharedRegions.Motion.Buffer == NULL || Context->SlotIndex < 1)
	{
		return;
	}

	offset = sizeof(IPC_MOTION_SNAPSHOT_MESSAGE) * (Context->SlotIndex - 1);
	if (offset + sizeof(IPC_MOTION_SNAPSHOT_MESSAGE) > pDrvCtx->IPC.SharedRegions.Motion.BufferSize)
	{
		return;
	}

	slot = (PIPC_MOTION_SNAPSHOT_MESSAGE)(pDrvCtx->IPC.SharedRegions.Motion.Buffer + offset);
	{
		const LONG sequence = InterlockedIncrement(&slot->SequenceNumber);

		if (Clear)
		{
			RtlZeroMemory(slot, sizeof(*slot));
		}
		else
		{
			DsMotion_FillIpcSnapshot(Context, slot);
		}

		slot->SequenceNumber = sequence;
	}

	InterlockedIncrement(&slot->SequenceNumber);
}
