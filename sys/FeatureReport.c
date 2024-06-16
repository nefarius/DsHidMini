#include "Driver.h"
#include "FeatureReport.tmh"


_Use_decl_annotations_
NTSTATUS
DSHM_GetFeature(
	_In_ const PDEVICE_CONTEXT DeviceContext,
	_In_ const DMF_CONTEXT_DsHidMini* ModuleContext,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	NTSTATUS status = STATUS_NOT_IMPLEMENTED;

	FuncEntry(TRACE_DSHIDMINIDRV);

#ifdef DSHM_FEATURE_FFB

	FFB_ATTRIBUTES ffbEntry = { 0 };

	PPID_POOL_REPORT pPool;
	PPID_BLOCK_LOAD_REPORT pBlockLoad;

	switch (Packet->reportId)
	{
	case PID_POOL_REPORT_ID:

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_POOL_REPORT_ID");

		pPool = (PPID_POOL_REPORT)Packet->reportBuffer;

	/*
	 * Static information about the fictitious device memory pool.
	 * Since we manage everything in software, size constraints
	 * are not an issue and we can report the maximum values.
	 */

		pPool->ReportId = PID_POOL_REPORT_ID;
		pPool->RamPoolSize = 65535;
		pPool->SimultaneousEffectsMax = MAX_EFFECT_BLOCKS;
		pPool->DeviceManagedPool = 1;
		pPool->SharedParameterBlocks = 0;

		*ReportSize = sizeof(PID_POOL_REPORT) - 1;

		status = STATUS_SUCCESS;

		break;

	case PID_BLOCK_LOAD_REPORT_ID:

		pBlockLoad = (PPID_BLOCK_LOAD_REPORT)Packet->reportBuffer;

		pBlockLoad->ReportId = PID_BLOCK_LOAD_REPORT_ID;
		pBlockLoad->RamPoolAvailable = 65535;
		pBlockLoad->BlockLoadStatus = PidBlsFull;

	//
	// Here we should have at least one new effect block index ready
	// 
		for (UCHAR index = 1; index < MAX_EFFECT_BLOCKS; index++)
		{
			status = DMF_HashTable_Read(
				ModuleContext->DmfModuleForceFeedback,
				&index,
				sizeof(UCHAR),
				(PUCHAR)&ffbEntry,
				sizeof(FFB_ATTRIBUTES),
				NULL
			);

			//
			// Find reserved but unclaimed entry
			// 
			if (NT_SUCCESS(status) && ffbEntry.IsReserved && !ffbEntry.IsReported)
			{
				ffbEntry.IsReported = TRUE;

				pBlockLoad->EffectBlockIndex = ffbEntry.EffectBlockIndex;
				pBlockLoad->BlockLoadStatus = PidBlsSuccess;

				status = DMF_HashTable_Write(
					ModuleContext->DmfModuleForceFeedback,
					&index,
					sizeof(UCHAR),
					(PUCHAR)&ffbEntry,
					sizeof(FFB_ATTRIBUTES)
				);

				break;
			}
		}

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_BLOCK_LOAD_REPORT_ID (EffectBlockIndex: %d)",
			pBlockLoad->EffectBlockIndex);

		*ReportSize = sizeof(PID_BLOCK_LOAD_REPORT) - 1;

		break;
	}

#endif

	//
	// SIXAXIS.SYS emulation
	// 
	if (Packet->reportId == 0x00 && DeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		//
		// Copy last received raw report to buffer
		// 
		RtlCopyMemory(
			Packet->reportBuffer,
			&ModuleContext->GetFeatureReport,
			Packet->reportBufferLen < SIXAXIS_HID_GET_FEATURE_REPORT_SIZE ? Packet->reportBufferLen :
			SIXAXIS_HID_GET_FEATURE_REPORT_SIZE
		);

		//
		// Alter report ID header to expected values
		// 
		Packet->reportBuffer[0] = 0x00;
		Packet->reportBuffer[1] = 0x3F;

		*ReportSize = Packet->reportBufferLen - 1;

		status = STATUS_SUCCESS;
	}
	//
	// DS4Windows emulation
	// 
	else if (
		Packet->reportId == 0x12 // Requests device MAC address
		&& Packet->reportBufferLen == 64
		&& DeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeDS4WindowsCompatible
		)
	{
		RtlCopyMemory(
			&Packet->reportBuffer[1],
			&DeviceContext->DeviceAddress,
			sizeof(BD_ADDR)
		);

		//
		// Fix Endianness
		// 
		if (DeviceContext->ConnectionType == DsDeviceConnectionTypeUsb)
		{
			REVERSE_BYTE_ARRAY(&Packet->reportBuffer[1], sizeof(BD_ADDR));
		}

		*ReportSize = Packet->reportBufferLen - 1;

		status = STATUS_SUCCESS;
	}

	if (!NT_SUCCESS(status))
	{
		TraceEvents(
			TRACE_LEVEL_WARNING,
			TRACE_DSHIDMINIDRV,
			"%!FUNC! Not implemented"
		);

		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"-- Packet->reportId: %d, Packet->reportBufferLen: %d",
			Packet->reportId,
			Packet->reportBufferLen
		);
	}

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}
