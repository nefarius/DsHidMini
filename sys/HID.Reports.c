#include "Driver.h"
#include "HID.Reports.tmh"


_Use_decl_annotations_
NTSTATUS
DsHidMini_GetInputReport(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	UNREFERENCED_PARAMETER(DmfModule);
	UNREFERENCED_PARAMETER(Request);
	UNREFERENCED_PARAMETER(Packet);
	UNREFERENCED_PARAMETER(ReportSize);

	//
	// NOTE: not really used by any modern game
	// 

	ASSERT(FALSE);

	return STATUS_NOT_IMPLEMENTED;
}

_Use_decl_annotations_
NTSTATUS
DsHidMini_SetOutputReport(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	UNREFERENCED_PARAMETER(DmfModule);
	UNREFERENCED_PARAMETER(Request);
	UNREFERENCED_PARAMETER(Packet);
	UNREFERENCED_PARAMETER(ReportSize);

	//
	// NOTE: not really used by any modern game
	// 

	ASSERT(FALSE);

	return STATUS_NOT_IMPLEMENTED;
}

_Use_decl_annotations_
NTSTATUS
DSHM_WriteReport(
	_In_ const PDEVICE_CONTEXT DeviceContext,
	_In_ const DMF_CONTEXT_DsHidMini* ModuleContext,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

	NTSTATUS status = STATUS_NOT_IMPLEMENTED;
	PUCHAR buffer = NULL;
	size_t bufferSize = 0;

#ifdef DSHM_FEATURE_FFB

	FFB_ATTRIBUTES ffbEntry = { 0 };

	PPID_DEVICE_CONTROL_REPORT pDeviceControl;
	PPID_DEVICE_GAIN_REPORT pGain;
	PPID_SET_CONDITION_REPORT pSetCondition;
	PPID_SET_EFFECT_REPORT pSetEffect;
	PPID_SET_PERIODIC_REPORT pSetPeriodic;
	PPID_SET_CONSTANT_FORCE_REPORT pSetConstant;
	PPID_EFFECT_OPERATION_REPORT pEffectOperation;
	PPID_BLOCK_FREE_REPORT pBlockFree;

	UCHAR rumbleValue = 0;

	// ReSharper disable once CppDefaultCaseNotHandledInSwitchStatement
	switch (Packet->reportId)
	{
	case PID_DEVICE_CONTROL_REPORT_ID:

		pDeviceControl = (PPID_DEVICE_CONTROL_REPORT)Packet->reportBuffer;

		switch (pDeviceControl->DeviceControlCommand)
		{
		case PidDcEnableActuators:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! DC Enable Actuators");
			break;
		case PidDcDisableActuators:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! DC Disable Actuators");
			break;
		case PidDcReset:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! DC Reset");

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

				if (NT_SUCCESS(status))
				{
					ffbEntry.IsReserved = FALSE;
					ffbEntry.IsReported = FALSE;

					(void)DMF_HashTable_Write(
						ModuleContext->DmfModuleForceFeedback,
						&index,
						sizeof(UCHAR),
						(PUCHAR)&ffbEntry,
						sizeof(FFB_ATTRIBUTES)
					);

					break;
				}
			}

		// Fall through
		case PidDcStopAllEffects:  // NOLINT(clang-diagnostic-implicit-fallthrough)
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! DC Stop All Effects");
			DS3_SET_BOTH_RUMBLE_STRENGTH(DeviceContext, 0x00, 0x00);

			(void)DSHM_SendOutputReport(DeviceContext, Ds3OutputReportSourceForceFeedback);

			break;
		case PidDcPause:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! DC Pause");
			break;
		case PidDcContinue:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! DC Continue");
			break;
		default:
			break;
		}

		*ReportSize = Packet->reportBufferLen;

		status = STATUS_SUCCESS;

		break;

	case PID_DEVICE_GAIN_REPORT_ID:

		pGain = (PPID_DEVICE_GAIN_REPORT)Packet->reportBuffer;

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_DEVICE_GAIN_REPORT, DeviceGain: %d",
			pGain->DeviceGain);

		*ReportSize = Packet->reportBufferLen;

		status = STATUS_SUCCESS;

		break;

	case PID_SET_CONDITION_REPORT_ID:

		pSetCondition = (PPID_SET_CONDITION_REPORT)Packet->reportBuffer;

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_SET_CONDITION_REPORT, EffectBlockIndex: %d",
			pSetCondition->EffectBlockIndex);

		*ReportSize = Packet->reportBufferLen;

		status = STATUS_SUCCESS;

		break;

	case PID_SET_EFFECT_REPORT_ID:

		pSetEffect = (PPID_SET_EFFECT_REPORT)Packet->reportBuffer;

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! SET_EFFECT_REPORT, EffectBlockIndex: %d, "
			"EffectType: %d, TotalDuration: %d, TriggerRepeatInterval: %d, "
			"SamplePeriod: %d, Gain: %d, TriggerButton: %d, AxesEnableX: %d, AxesEnableY: %d, "
			"DirectionEnable: %d, DirectionInstance1: %d, DirectionInstance2: %d, StartDelay: %d",
			pSetEffect->EffectBlockIndex,
			pSetEffect->EffectType,
			pSetEffect->Duration,
			pSetEffect->TriggerRepeatInterval,
			pSetEffect->SamplePeriod,
			pSetEffect->Gain,
			pSetEffect->TriggerButton,
			pSetEffect->AxesEnableX,
			pSetEffect->AxesEnableY,
			pSetEffect->DirectionEnable,
			pSetEffect->DirectionInstance1,
			pSetEffect->DirectionInstance2,
			pSetEffect->StartDelay);

		*ReportSize = Packet->reportBufferLen;

		status = STATUS_SUCCESS;

		break;

	case PID_SET_PERIODIC_REPORT_ID:

		pSetPeriodic = (PPID_SET_PERIODIC_REPORT)Packet->reportBuffer;

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_SET_PERIODIC_REPORT, "
			"EffectBlockIndex: %d, Magnitude: %d, Offset: %d, Phase: %d, Period: %d",
			pSetPeriodic->EffectBlockIndex,
			pSetPeriodic->Magnitude,
			pSetPeriodic->Offset,
			pSetPeriodic->Phase,
			pSetPeriodic->Period
		);

		*ReportSize = Packet->reportBufferLen;

		status = STATUS_SUCCESS;

		break;

	case PID_SET_CONSTANT_FORCE_REPORT_ID:

		pSetConstant = (PPID_SET_CONSTANT_FORCE_REPORT)Packet->reportBuffer;

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_SET_CONSTANT_FORCE_REPORT, EffectBlockIndex: %d, Magnitude: %d",
			pSetConstant->EffectBlockIndex,
			pSetConstant->Magnitude);

		rumbleValue = (UCHAR)(pSetConstant->Magnitude / 10000.0f * 255.0f);

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! DS3 Rumble Value: %d",
			rumbleValue);

		if (pSetConstant->EffectBlockIndex % 2 == 0)
		{
			DS3_SET_SMALL_RUMBLE_STRENGTH(DeviceContext, rumbleValue);
		}
		else
		{
			DS3_SET_LARGE_RUMBLE_STRENGTH(DeviceContext, rumbleValue);
		}

		*ReportSize = Packet->reportBufferLen;

		status = STATUS_SUCCESS;

		break;

	case PID_EFFECT_OPERATION_REPORT_ID:

		pEffectOperation = (PPID_EFFECT_OPERATION_REPORT)Packet->reportBuffer;

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: %d, "
			"EffectOperation: %d, LoopCount: %d",
			pEffectOperation->EffectBlockIndex,
			pEffectOperation->EffectOperation,
			pEffectOperation->LoopCount);

		switch (pEffectOperation->EffectOperation)
		{
		case PidEoStart:

			(void)DSHM_SendOutputReport(DeviceContext, Ds3OutputReportSourceForceFeedback);

			break;

		case PidEoStop:

			DS3_SET_BOTH_RUMBLE_STRENGTH(DeviceContext, 0x00, 0x00);

			(void)DSHM_SendOutputReport(DeviceContext, Ds3OutputReportSourceForceFeedback);

			break;
		default:
			break;
		}

		*ReportSize = Packet->reportBufferLen;

		status = STATUS_SUCCESS;

		break;

	case PID_BLOCK_FREE_REPORT_ID:

		pBlockFree = (PPID_BLOCK_FREE_REPORT)Packet->reportBuffer;

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_BLOCK_FREE_REPORT, EffectBlockIndex: %d",
			pBlockFree->EffectBlockIndex);

	//
	// Mark as free
	// 
		ffbEntry.IsReserved = FALSE;
		ffbEntry.IsReported = FALSE;

		status = DMF_HashTable_Write(
			ModuleContext->DmfModuleForceFeedback,
			&pBlockFree->EffectBlockIndex,
			sizeof(UCHAR),
			(PUCHAR)&ffbEntry,
			sizeof(FFB_ATTRIBUTES)
		);

		*ReportSize = Packet->reportBufferLen;

		break;
	}

#endif

	//
	// Handle other non-FFB cases
	// 

	//
	// SIXAXIS.SYS emulation
	// 
	if (Packet->reportId == 0x00 && DeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		//
		// External output report overrides internal behaviour, keep note
		// 
		DeviceContext->OutputReport.Mode = Ds3OutputReportModeWriteReportPassThrough;

		//
		// Get raw buffer pointer (connection agnostic)
		// 
		DS3_GET_UNIFIED_OUTPUT_REPORT_BUFFER(
			DeviceContext,
			&buffer,
			&bufferSize
		);

		//
		// Prevent LED states from being overwritten from outside
		// 
		if (DeviceContext->Configuration.LEDSettings.Authority == DsLEDAuthorityDriver)
		{
			UCHAR ledBlock[sizeof(UCHAR) + (sizeof(DS_LED) * 4)];

			//
			// Backup LED states
			// 
			RtlCopyMemory(ledBlock, &buffer[9], ARRAYSIZE(ledBlock));

			//
			// Overwrite with what we received
			// 
			RtlCopyMemory(
				buffer,
				&Packet->reportBuffer[3],
				bufferSize
			);

			//
			// Restore LED states
			// 
			RtlCopyMemory(&buffer[9], ledBlock, ARRAYSIZE(ledBlock));
		}
		else
		{
			//
			// Overwrite with what we received
			// 
			RtlCopyMemory(
				buffer,
				&Packet->reportBuffer[3],
				bufferSize
			);
		}

		(void)DSHM_SendOutputReport(DeviceContext, Ds3OutputReportSourcePassThrough);

		status = STATUS_SUCCESS;
	}

	//
	// DS4Windows emulation
	// 
	if (Packet->reportId == 0x05 && DeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeDS4WindowsCompatible)
	{
		//
		// External output report overrides internal behaviour, keep note
		// 
		DeviceContext->OutputReport.Mode = Ds3OutputReportModeWriteReportPassThrough;

		const BOOL isSetRumble = (Packet->reportBuffer[1] >> 0) & 1U;
		const BOOL isSetColor = (Packet->reportBuffer[1] >> 1) & 1U;
		const BOOL isFlagFlash = (Packet->reportBuffer[1] >> 2) & 1U;

		//
		// Color values (RGB)
		// 
		const UCHAR r = Packet->reportBuffer[6];
		const UCHAR g = Packet->reportBuffer[7];
		const UCHAR b = Packet->reportBuffer[8];

		//
		// Flash Bright and Dark duration
		// 
		const UCHAR fb_dur = Packet->reportBuffer[9];
		const UCHAR fd_dur = Packet->reportBuffer[10];

		const BOOL isFlashOrPulse = isFlagFlash && (fb_dur != 0 || fd_dur != 0);
		BOOL isSetFlashing = FALSE;

		if (isFlashOrPulse) // High Latency DS4Windows function
		{
			// Hard-coded colors used in High Latency warning
			if (r == 0x32 && g == 0x00 && b == 0x00)
			{
				isSetFlashing = TRUE;
			}
		}

		if (isSetRumble)
		{
			DS3_SET_BOTH_RUMBLE_STRENGTH(DeviceContext, Packet->reportBuffer[5], Packet->reportBuffer[4]);
		}

		//
		// Only allowed when when in Automatic or Application setting
		// 
		if (DeviceContext->Configuration.LEDSettings.Authority != DsLEDAuthorityDriver)
		{
			if (isSetColor)
			{
				//
				// Restore defaults to undo any (past) flashing animations
				// 
				DS3_SET_LED_DURATION_DEFAULT(DeviceContext, 0);
				DS3_SET_LED_DURATION_DEFAULT(DeviceContext, 1);
				DS3_SET_LED_DURATION_DEFAULT(DeviceContext, 2);
				DS3_SET_LED_DURATION_DEFAULT(DeviceContext, 3);

				//
				// Single color RED intensity indicates battery level (Light only a single LED from 1 to 4)
				// 
				if (g == 0x00 && b == 0x00)
				{
					if (r >= 202)
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_4);
					else if (r > 148)
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_3);
					else if (r > 94)
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_2);
					else if (r > 64)
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_1);
					else
					{
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_1);
						DS3_SET_LED_DURATION(DeviceContext, 0, 0xFF, 15, 127, 127);
					}
				}
				//
				// Single color RED intensity indicates battery level ("Fill" LEDs from 1 to 4)
				// 
				else if (g == 0x00 && b == 0xFF)
				{
					if (r >= 202)
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_1 | DS3_LED_2 | DS3_LED_3 | DS3_LED_4);
					else if (r > 148)
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_1 | DS3_LED_2 | DS3_LED_3);
					else if (r > 94)
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_1 | DS3_LED_2);
					else if (r > 64)
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_1);
					else
					{
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_1);
						DS3_SET_LED_DURATION(DeviceContext, 0, 0xFF, 15, 127, 127);
					}
				}
				//
				// Decode custom LED status from color RED intensity
				// 
				else if (g == 0xFF && b == 0xFF)
				{
					if (r == 0x00)
						DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_OFF);
					else if (r >= 0x01 && r <= 0x0F)
						DS3_SET_LED_FLAGS(DeviceContext, r << 1);
				}
			}

			if (isSetFlashing)
			{
				//
				// Set to rapidly flash all 4 LEDs
				// 
				DS3_SET_LED_FLAGS(DeviceContext, DS3_LED_1 | DS3_LED_2 | DS3_LED_3 | DS3_LED_4);
				DS3_SET_LED_DURATION(DeviceContext, 0, 0xFF, 3, 127, 127);
				DS3_SET_LED_DURATION(DeviceContext, 1, 0xFF, 3, 127, 127);
				DS3_SET_LED_DURATION(DeviceContext, 2, 0xFF, 3, 127, 127);
				DS3_SET_LED_DURATION(DeviceContext, 3, 0xFF, 3, 127, 127);
			}
		}

		(void)DSHM_SendOutputReport(DeviceContext, Ds3OutputReportSourceDualShock4);

		status = STATUS_SUCCESS;
	}

	//
	// Rumble request from XINPUTHID.SYS
	// 
	if (Packet->reportId == 0x00 && DeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible)
	{
		UCHAR lm = (UCHAR)(Packet->reportBuffer[3] / 100.0f * 255.0f);
		UCHAR rm = (UCHAR)(Packet->reportBuffer[4] / 100.0f * 255.0f);

		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"-- XI FFB LM: %d, RM: %d",
			lm,
			rm
		);

		DS3_SET_BOTH_RUMBLE_STRENGTH(DeviceContext, lm, rm);

		(void)DSHM_SendOutputReport(DeviceContext, Ds3OutputReportSourceXInputHID);

		status = STATUS_SUCCESS;
	}

	//
	// Unknown request, trace details for diagnostics
	// 
	if (!NT_SUCCESS(status))
	{
		TraceEvents(
			TRACE_LEVEL_WARNING,
			TRACE_DSHIDMINIDRV, "%!FUNC! Not implemented"
		);

		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"-- Packet->reportId: %d, Packet->reportBufferLen: %d",
			Packet->reportId,
			Packet->reportBufferLen
		);

		DumpAsHex("-- WRITE_REPORT.reportBuffer", Packet->reportBuffer, Packet->reportBufferLen);
	}

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}
