#pragma once

//
// SXS output classification without WDF. sixaxis.sys commands arrive with
// report id 0 after HIDClass strips a leading 0x00:
//
//   0x02 motors: 3 padding bytes, then small duration, small on/off,
//                large duration, large force. SDL's Sony HIDAPI driver
//                sends only those bytes (the rest of the 48-byte write is
//                zero). RPCS3 and XInputBridge send the same prefix and
//                then the full DS3 body, including the LED block.
//   0x01 LEDs:   3 padding bytes, then LED4..LED1 modes
//                (0 off, 1 on, 2 flashing).
//   0x03, 0x09, 0x0A: accepted, no output change.
//
// A full-body motor write is detected by a non-zero LED section at report
// bytes [12..32] (unified offset 9, the flags byte, through the four
// effect blocks). Motors-only writes touch unified[0..4] and leave the LED
// block alone, so a rumble packet cannot blank the player LEDs.
//

#define DS_SXS_UNIFIED_OUTPUT_LENGTH 48
#define DS_SXS_LED_BLOCK_OFFSET 9
//
// Same span the previous SXS handler saved: the LED flag plus four DS_LED
// values, including the padding MSVC inserts after TotalDuration.
//
#define DS_SXS_LED_BLOCK_LENGTH (sizeof(UCHAR) + (sizeof(DS_LED) * 4))

#define DS_SXS_LED_FLAGS_OFFSET 9
#define DS_SXS_LED_EFFECT_OFFSET 10
#define DS_SXS_LED_EFFECT_BYTES 5

//
// Wire image of DS3_LED_EFFECT_STATIC / DS3_LED_EFFECT_SLOW_FLASH, matching
// DsLed_SetEffect (duration high byte first).
//
#define DS_SXS_EFFECT_STATIC { 0xFF, 0x00, 0x01, 0x00, 0x01 }
#define DS_SXS_EFFECT_SLOW_FLASH { 0xFF, 0x00, 0x0F, 0x7F, 0x7F }
#define DS_SXS_EFFECT_NONE { 0x00, 0x00, 0x00, 0x00, 0x00 }

#ifndef DS3_LED_1
#define DS3_LED_1 0x02
#define DS3_LED_2 0x04
#define DS3_LED_3 0x08
#define DS3_LED_4 0x10
#endif

typedef enum _DS_SXS_OUTPUT_KIND
{
	DsSxsOutputKindPassthrough = 1,
	DsSxsOutputKindMotors,
	DsSxsOutputKindLeds,
	DsSxsOutputKindNoOutput
} DS_SXS_OUTPUT_KIND;

typedef struct _DS_SXS_OUTPUT
{
	DS_SXS_OUTPUT_KIND Kind;
	BOOLEAN HasFullBody;
	BOOLEAN AnyFlashing;
	UCHAR SmallDuration;
	UCHAR SmallOn;
	UCHAR LargeDuration;
	UCHAR LargeForce;
	UCHAR LedFlags;
} DS_SXS_OUTPUT, * PDS_SXS_OUTPUT;

static
BOOLEAN
DsSxsOutput_SpanNonZero(
	_In_reads_(Length) const UCHAR* Report,
	_In_ ULONG Length,
	_In_ ULONG Start,
	_In_ ULONG End
)
{
	ULONG index;

	if (Start >= Length)
	{
		return FALSE;
	}

	if (End > Length)
	{
		End = Length;
	}

	for (index = Start; index < End; index++)
	{
		if (Report[index] != 0)
		{
			return TRUE;
		}
	}

	return FALSE;
}

static
UCHAR
DsSxsOutput_LedBit(
	_In_ UCHAR ModeIndex
)
{
	static const UCHAR bits[4] = { DS3_LED_4, DS3_LED_3, DS3_LED_2, DS3_LED_1 };

	return bits[ModeIndex];
}

static
VOID
DsSxsOutput_WriteEffect(
	_Inout_updates_(UnifiedLength) PUCHAR Unified,
	_In_ ULONG UnifiedLength,
	_In_ UCHAR LedIndex,
	_In_reads_(DS_SXS_LED_EFFECT_BYTES) const UCHAR* Effect
)
{
	//
	// DsLed_SetEffect stores LED 1 in the last block and LED 4 in the first.
	//
	const UCHAR slot = (UCHAR)(3 - LedIndex);
	const ULONG offset = DS_SXS_LED_EFFECT_OFFSET + ((ULONG)slot * DS_SXS_LED_EFFECT_BYTES);

	if (LedIndex > 3 || offset + DS_SXS_LED_EFFECT_BYTES > UnifiedLength)
	{
		return;
	}

	RtlCopyMemory(&Unified[offset], Effect, DS_SXS_LED_EFFECT_BYTES);
}

_Must_inspect_result_
static
NTSTATUS
DsSxsOutput_Inspect(
	_In_reads_(ReportLength) const UCHAR* Report,
	_In_ ULONG ReportLength,
	_In_ UCHAR ReportId,
	_Out_ PDS_SXS_OUTPUT Parsed
)
{
	UCHAR modeIndex;

	if (Report == NULL || Parsed == NULL)
	{
		return STATUS_INVALID_PARAMETER;
	}

	RtlZeroMemory(Parsed, sizeof(*Parsed));

	if (ReportId != 0x00)
	{
		return STATUS_NOT_IMPLEMENTED;
	}

	if (ReportLength < 1)
	{
		return STATUS_BUFFER_TOO_SMALL;
	}

	switch (Report[0])
	{
	case 0x01:

		if (ReportLength < 8)
		{
			return STATUS_BUFFER_TOO_SMALL;
		}

		Parsed->Kind = DsSxsOutputKindLeds;

		for (modeIndex = 0; modeIndex < 4; modeIndex++)
		{
			const UCHAR mode = Report[4 + modeIndex];

			if (mode == 0)
			{
				continue;
			}

			Parsed->LedFlags |= DsSxsOutput_LedBit(modeIndex);

			if (mode == 2)
			{
				Parsed->AnyFlashing = TRUE;
			}
		}

		return STATUS_SUCCESS;

	case 0x02:

		if (ReportLength < 8)
		{
			return STATUS_BUFFER_TOO_SMALL;
		}

		Parsed->Kind = DsSxsOutputKindMotors;
		Parsed->SmallDuration = Report[4];
		Parsed->SmallOn = Report[5];
		Parsed->LargeDuration = Report[6];
		Parsed->LargeForce = Report[7];
		//
		// LED flags live at unified[9], which is report byte 12 once the
		// 02 00 00 prefix is skipped. The four effect blocks follow it.
		//
		Parsed->HasFullBody = DsSxsOutput_SpanNonZero(Report, ReportLength, 12, 33);
		return STATUS_SUCCESS;

	case 0x03:
	case 0x09:
	case 0x0A:

		Parsed->Kind = DsSxsOutputKindNoOutput;
		return STATUS_SUCCESS;

	default:

		if (ReportLength < 3)
		{
			return STATUS_BUFFER_TOO_SMALL;
		}

		Parsed->Kind = DsSxsOutputKindPassthrough;
		return STATUS_SUCCESS;
	}
}

static
VOID
DsSxsOutput_WriteLeds(
	_In_ const DS_SXS_OUTPUT* Parsed,
	_Inout_updates_(UnifiedLength) PUCHAR Unified,
	_In_ ULONG UnifiedLength
)
{
	static const UCHAR solid[DS_SXS_LED_EFFECT_BYTES] = DS_SXS_EFFECT_STATIC;
	static const UCHAR flash[DS_SXS_LED_EFFECT_BYTES] = DS_SXS_EFFECT_SLOW_FLASH;
	static const UCHAR none[DS_SXS_LED_EFFECT_BYTES] = DS_SXS_EFFECT_NONE;
	static const UCHAR bits[4] = { DS3_LED_1, DS3_LED_2, DS3_LED_3, DS3_LED_4 };
	const UCHAR* lit = Parsed->AnyFlashing ? flash : solid;
	UCHAR ledIndex;

	if (UnifiedLength <= DS_SXS_LED_FLAGS_OFFSET)
	{
		return;
	}

	Unified[DS_SXS_LED_FLAGS_OFFSET] = Parsed->LedFlags;

	for (ledIndex = 0; ledIndex < 4; ledIndex++)
	{
		const UCHAR* effect = (Parsed->LedFlags & bits[ledIndex]) ? lit : none;

		DsSxsOutput_WriteEffect(Unified, UnifiedLength, ledIndex, effect);
	}
}

_Must_inspect_result_
static
NTSTATUS
DsSxsOutput_Apply(
	_In_reads_(ReportLength) const UCHAR* Report,
	_In_ ULONG ReportLength,
	_In_ UCHAR ReportId,
	_In_ BOOLEAN PreserveLeds,
	_Inout_updates_(UnifiedLength) PUCHAR Unified,
	_In_ ULONG UnifiedLength,
	_Out_ PULONG ReportSize,
	_Out_opt_ PDS_SXS_OUTPUT ParsedOut
)
{
	DS_SXS_OUTPUT parsed;
	UCHAR ledBlock[DS_SXS_LED_BLOCK_LENGTH];
	NTSTATUS status;
	const BOOLEAN haveLeds = PreserveLeds
		&& UnifiedLength >= DS_SXS_LED_BLOCK_OFFSET + DS_SXS_LED_BLOCK_LENGTH;

	if (Unified == NULL || ReportSize == NULL || UnifiedLength == 0)
	{
		return STATUS_INVALID_PARAMETER;
	}

	status = DsSxsOutput_Inspect(Report, ReportLength, ReportId, &parsed);
	if (!NT_SUCCESS(status))
	{
		return status;
	}

	if (ParsedOut != NULL)
	{
		*ParsedOut = parsed;
	}

	if (parsed.Kind == DsSxsOutputKindNoOutput)
	{
		*ReportSize = ReportLength;
		return STATUS_SUCCESS;
	}

	if (haveLeds)
	{
		RtlCopyMemory(ledBlock, &Unified[DS_SXS_LED_BLOCK_OFFSET], DS_SXS_LED_BLOCK_LENGTH);
	}

	if (parsed.Kind == DsSxsOutputKindMotors && !parsed.HasFullBody)
	{
		if (UnifiedLength < 5)
		{
			return STATUS_BUFFER_TOO_SMALL;
		}

		Unified[0] = 0x00;
		Unified[1] = parsed.SmallDuration;
		Unified[2] = parsed.SmallOn;
		Unified[3] = parsed.LargeDuration;
		Unified[4] = parsed.LargeForce;
	}
	else if (parsed.Kind == DsSxsOutputKindLeds)
	{
		if (!PreserveLeds)
		{
			DsSxsOutput_WriteLeds(&parsed, Unified, UnifiedLength);
		}
	}
	else
	{
		ULONG copy = ReportLength - 3;

		if (copy > UnifiedLength)
		{
			copy = UnifiedLength;
		}

		RtlCopyMemory(Unified, &Report[3], copy);

		if (haveLeds)
		{
			RtlCopyMemory(&Unified[DS_SXS_LED_BLOCK_OFFSET], ledBlock, DS_SXS_LED_BLOCK_LENGTH);
		}
	}

	*ReportSize = ReportLength;
	return STATUS_SUCCESS;
}
