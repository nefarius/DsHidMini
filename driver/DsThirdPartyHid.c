#include "Driver.h"
#include "DsMotion.h"
#include "DsThirdPartyHid.tmh"


//
// Sticks rest at 0x7F. DS3 consumers treat 0x80 as center, so shift every
// value except 0xFF up by one count.
// 
static
UCHAR
ThirdPartyHid_BiasAxis(
	_In_ UCHAR Value
)
{
	return Value == 0xFF ? 0xFF : (UCHAR)(Value + 1);
}

static
VOID
ThirdPartyHid_ApplyHat(
	_Inout_ PDS3_RAW_INPUT_REPORT Report,
	_In_ UCHAR Hat
)
{
	switch (Hat & 0x0F)
	{
	case 0:
		Report->Buttons.Individual.Up = 1;
		break;
	case 1:
		Report->Buttons.Individual.Up = 1;
		Report->Buttons.Individual.Right = 1;
		break;
	case 2:
		Report->Buttons.Individual.Right = 1;
		break;
	case 3:
		Report->Buttons.Individual.Right = 1;
		Report->Buttons.Individual.Down = 1;
		break;
	case 4:
		Report->Buttons.Individual.Down = 1;
		break;
	case 5:
		Report->Buttons.Individual.Down = 1;
		Report->Buttons.Individual.Left = 1;
		break;
	case 6:
		Report->Buttons.Individual.Left = 1;
		break;
	case 7:
		Report->Buttons.Individual.Left = 1;
		Report->Buttons.Individual.Up = 1;
		break;
	default:
		break;
	}
}

//
// 27-byte ShanWan HID input (no report id on the wire) into the DS3 report
// every HID mode already consumes. Face-button order is the conventional
// map for this 137-byte descriptor: bit 0 is Square, not Cross.
// 
VOID
ThirdPartyHid_TranslateInput(
	_In_reads_(Length) const UCHAR* Raw,
	_In_ size_t Length,
	_Out_ PDS3_RAW_INPUT_REPORT Report
)
{
	UCHAR face;
	UCHAR system;

	RtlZeroMemory(Report, sizeof(*Report));
	Report->ReportId = 0x01;

	if (Raw == NULL || Length < 3)
	{
		return;
	}

	face = Raw[0];
	Report->Buttons.Individual.Square = (face >> 0) & 0x01;
	Report->Buttons.Individual.Cross = (face >> 1) & 0x01;
	Report->Buttons.Individual.Circle = (face >> 2) & 0x01;
	Report->Buttons.Individual.Triangle = (face >> 3) & 0x01;
	Report->Buttons.Individual.L1 = (face >> 4) & 0x01;
	Report->Buttons.Individual.R1 = (face >> 5) & 0x01;
	Report->Buttons.Individual.L2 = (face >> 6) & 0x01;
	Report->Buttons.Individual.R2 = (face >> 7) & 0x01;

	//
	// Bits 5-7 are constant padding and read as 1 while idle.
	// 
	system = Raw[1] & 0x1F;
	Report->Buttons.Individual.Select = (system >> 0) & 0x01;
	Report->Buttons.Individual.Start = (system >> 1) & 0x01;
	Report->Buttons.Individual.L3 = (system >> 2) & 0x01;
	Report->Buttons.Individual.R3 = (system >> 3) & 0x01;
	Report->Buttons.Individual.PS = (system >> 4) & 0x01;

	ThirdPartyHid_ApplyHat(Report, Raw[2]);

	if (Length >= 7)
	{
		Report->LeftThumbX = ThirdPartyHid_BiasAxis(Raw[3]);
		Report->LeftThumbY = ThirdPartyHid_BiasAxis(Raw[4]);
		Report->RightThumbX = ThirdPartyHid_BiasAxis(Raw[5]);
		Report->RightThumbY = ThirdPartyHid_BiasAxis(Raw[6]);
	}

	//
	// Vendor bytes 0x20-0x2B, idle 0x00. Order used by this descriptor
	// family: Right, Left, Up, Down, Triangle, Circle, Cross, Square,
	// L1, R1, L2, R2.
	// 
	if (Length >= THIRD_PARTY_HID_INPUT_REPORT_MINIMUM)
	{
		Report->Pressure.Values.Right = Raw[7];
		Report->Pressure.Values.Left = Raw[8];
		Report->Pressure.Values.Up = Raw[9];
		Report->Pressure.Values.Down = Raw[10];
		Report->Pressure.Values.Triangle = Raw[11];
		Report->Pressure.Values.Circle = Raw[12];
		Report->Pressure.Values.Cross = Raw[13];
		Report->Pressure.Values.Square = Raw[14];
		Report->Pressure.Values.L1 = Raw[15];
		Report->Pressure.Values.R1 = Raw[16];
		Report->Pressure.Values.L2 = Raw[17];
		Report->Pressure.Values.R2 = Raw[18];
	}

	Report->BatteryStatus = (UCHAR)DsBatteryStatusCharged;

	//
	// Big-endian, matching DS3_RAW_INPUT_REPORT. DsMotion_ProcessInputReport
	// byteswaps these back to the nominal rest sample (Z at +1 g).
	// 
	Report->AccelerometerX = _byteswap_ushort((USHORT)DS_MOTION_NOMINAL_ZERO);
	Report->AccelerometerY = _byteswap_ushort((USHORT)DS_MOTION_NOMINAL_ZERO);
	Report->AccelerometerZ = _byteswap_ushort(
		(USHORT)(DS_MOTION_NOMINAL_ZERO + DS_MOTION_ACCEL_GAIN)
	);
	Report->Gyroscope = _byteswap_ushort((USHORT)DS_MOTION_NOMINAL_ZERO);
}

//
// Snapshot the right-motor magnitude for the report about to be queued.
// Pass-through copies a DS3 report whose small-motor byte is only on/off
// and does not update LightCache, so an "on" write uses full strength.
// Every other source already stored its magnitude in LightCache before
// the send, and that value is what must travel with this queue entry.
// 
UCHAR
ThirdPartyHid_CaptureRightMotorStrength(
	_In_ DS_OUTPUT_REPORT_SOURCE Source,
	_In_reads_(Ds3ReportLength) const UCHAR* Ds3Report,
	_In_ size_t Ds3ReportLength,
	_In_ UCHAR LightCache
)
{
	if (Ds3Report == NULL || Ds3ReportLength <= 3 || Ds3Report[3] == 0)
	{
		return 0;
	}

	if (Source == Ds3OutputReportSourcePassThrough || LightCache == 0)
	{
		return THIRD_PARTY_HID_SMALL_MOTOR_ON_STRENGTH;
	}

	return LightCache;
}

//
// Large-motor byte 5 already holds the rescaled strength. RightMotorStrength
// is the snapshot taken when this report was queued.
// 
VOID
ThirdPartyHid_BuildOutputReport(
	_In_reads_(Ds3ReportLength) const UCHAR* Ds3Report,
	_In_ size_t Ds3ReportLength,
	_In_ UCHAR RightMotorStrength,
	_Out_writes_(THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH) PUCHAR Output
)
{
	UCHAR right = 0;
	UCHAR left = 0;

	RtlZeroMemory(Output, THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH);

	if (Ds3Report != NULL && Ds3ReportLength > 3 && Ds3Report[3] != 0)
	{
		right = RightMotorStrength;
	}

	if (Ds3Report != NULL && Ds3ReportLength > 5)
	{
		left = Ds3Report[5];
	}

	Output[0] = 0x02;
	Output[1] = 0x08;
	Output[2] = right;
	Output[3] = left;
	Output[4] = (right != 0 || left != 0) ? 0xFF : 0x00;
}

NTSTATUS
ThirdPartyHid_SendOutputReport(
	_In_ PDEVICE_CONTEXT Context,
	_In_reads_(THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH) PUCHAR Output
)
{
	return USB_SendControlRequest(
		Context,
		BmRequestHostToDevice,
		BmRequestClass,
		SetReport,
		THIRD_PARTY_HID_OUTPUT_REPORT_VALUE,
		0,
		Output,
		THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH,
		NULL
	);
}
