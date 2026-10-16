#pragma once

//
// SXS output classification without WDF. Steam's SDL PS3 HIDAPI driver writes
// a DualShock 3 effect image (report id 0x01, constant 0xFF motor durations).
// RPCS3 and XInputBridge write the sixaxis.sys envelope: 0x02,0x00,0x00 and
// then the 48-byte DS3 body. Windows removes a leading 0x00 report id before
// this runs, so that body already begins at byte 3.
//

#define DS_SXS_UNIFIED_OUTPUT_LENGTH 48
#define DS_SXS_LED_BLOCK_OFFSET 9
//
// Same span the previous SXS handler saved: the LED flag plus four DS_LED
// values, including the padding MSVC inserts after TotalDuration.
//
#define DS_SXS_LED_BLOCK_LENGTH (sizeof(UCHAR) + (sizeof(DS_LED) * 4))

static
BOOLEAN
DsSxsOutput_IsSteamPrefixed(
	_In_reads_(Length) const UCHAR* Report,
	_In_ ULONG Length
)
{
	return Length >= 6
		&& Report[0] == 0x01
		&& Report[1] == 0x01
		&& Report[2] == 0xFF
		&& Report[4] == 0xFF;
}

static
BOOLEAN
DsSxsOutput_IsSteamAligned(
	_In_reads_(Length) const UCHAR* Report,
	_In_ ULONG Length
)
{
	return Length >= 5
		&& Report[0] == 0x01
		&& Report[1] == 0xFF
		&& Report[3] == 0xFF;
}

static
VOID
DsSxsOutput_RestoreLeds(
	_In_ BOOLEAN PreserveLeds,
	_In_reads_bytes_(DS_SXS_LED_BLOCK_LENGTH) const UCHAR* LedBlock,
	_Inout_updates_(UnifiedLength) PUCHAR Unified,
	_In_ ULONG UnifiedLength
)
{
	if (!PreserveLeds || UnifiedLength < DS_SXS_LED_BLOCK_OFFSET + DS_SXS_LED_BLOCK_LENGTH)
	{
		return;
	}

	RtlCopyMemory(&Unified[DS_SXS_LED_BLOCK_OFFSET], LedBlock, DS_SXS_LED_BLOCK_LENGTH);
}

//
// Maps an SDL effect image onto the unified DS3 body. Byte 0 of that image is
// SDL's extra report id, not the DS3 reserved byte, so the copy starts at the
// small-motor duration.
//
static
VOID
DsSxsOutput_CopySteamImage(
	_In_reads_(SourceLength) const UCHAR* Source,
	_In_ ULONG SourceLength,
	_Inout_updates_(UnifiedLength) PUCHAR Unified,
	_In_ ULONG UnifiedLength
)
{
	ULONG copy = SourceLength;

	if (UnifiedLength == 0)
	{
		return;
	}

	Unified[0] = 0x00;
	if (copy > UnifiedLength - 1)
	{
		copy = UnifiedLength - 1;
	}

	if (copy > 0)
	{
		RtlCopyMemory(&Unified[1], Source, copy);
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
	_Out_ PULONG ReportSize
)
{
	UCHAR ledBlock[DS_SXS_LED_BLOCK_LENGTH];
	const BOOLEAN haveLeds = PreserveLeds
		&& UnifiedLength >= DS_SXS_LED_BLOCK_OFFSET + DS_SXS_LED_BLOCK_LENGTH;

	if (Report == NULL || Unified == NULL || ReportSize == NULL || UnifiedLength == 0)
	{
		return STATUS_INVALID_PARAMETER;
	}

	if (haveLeds)
	{
		RtlCopyMemory(ledBlock, &Unified[DS_SXS_LED_BLOCK_OFFSET], DS_SXS_LED_BLOCK_LENGTH);
	}

	if (ReportLength >= 2 && Report[0] == 0x01 && Report[1] == 0x01)
	{
		if (!DsSxsOutput_IsSteamPrefixed(Report, ReportLength))
		{
			return STATUS_BUFFER_TOO_SMALL;
		}

		// Skip both leading 0x01 bytes. The remainder starts at the duration.
		DsSxsOutput_CopySteamImage(&Report[2], ReportLength - 2, Unified, UnifiedLength);
	}
	else if (ReportLength >= 2 && Report[0] == 0x01 && Report[1] == 0xFF)
	{
		if (!DsSxsOutput_IsSteamAligned(Report, ReportLength))
		{
			return STATUS_BUFFER_TOO_SMALL;
		}

		// The single leading 0x01 is SDL's effect-image report id.
		DsSxsOutput_CopySteamImage(&Report[1], ReportLength - 1, Unified, UnifiedLength);
	}
	else if (ReportId == 0x00)
	{
		ULONG copy;

		if (ReportLength < 3)
		{
			return STATUS_BUFFER_TOO_SMALL;
		}

		copy = ReportLength - 3;
		if (copy > UnifiedLength)
		{
			copy = UnifiedLength;
		}

		RtlCopyMemory(Unified, &Report[3], copy);
	}
	else
	{
		return STATUS_NOT_IMPLEMENTED;
	}

	DsSxsOutput_RestoreLeds(haveLeds, ledBlock, Unified, UnifiedLength);
	*ReportSize = ReportLength;
	return STATUS_SUCCESS;
}
