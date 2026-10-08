#include "Driver.h"
#include "InputReport.tmh"

//
// Writes DS4 gyro, accelerometer and timestamp from the canonical motion sample
// (see DsMotionHid.h for frame and units). Zeroes them while no sample exists.
// 
static
VOID
DSHM_FillDs4Motion(
	_In_ const PDEVICE_CONTEXT DeviceContext,
	_Inout_ PUCHAR Output
)
{
	const PDS_MOTION_STATE motion = &DeviceContext->Motion;
	DS_MOTION_HID_FRAME frame;
	USHORT timestamp = 0;

	RtlZeroMemory(&frame, sizeof(frame));

	if (motion->HasSample)
	{
		LARGE_INTEGER frequency;

		DsMotionHid_ToDeviceFrame(
			motion->Sample.AccelMilliGX,
			motion->Sample.AccelMilliGY,
			motion->Sample.AccelMilliGZ,
			motion->Sample.GyroMilliDps,
			&frame
		);

		QueryPerformanceFrequency(&frequency);
		timestamp = DsMotionHid_Ds4Timestamp(motion->Sample.TimestampQpc.QuadPart, frequency.QuadPart);
	}

	DsMotionHid_WriteDs4Report(&frame, Output);
	Output[DS_MOTION_HID_DS4_OFFSET_TIMESTAMP] = (UCHAR)(timestamp & 0xFF);
	Output[DS_MOTION_HID_DS4_OFFSET_TIMESTAMP + 1] = (UCHAR)(timestamp >> 8);
}

static
BOOLEAN
DSHM_TrySendStagedInputReport(
	_In_ DMF_CONTEXT_DsHidMini* ModuleDeviceContext
)
{
	const NTSTATUS status = DMF_VirtualHidMini_InputReportGenerate(
		ModuleDeviceContext->DmfModuleVirtualHidMini,
		DsHidMini_RetrieveNextInputReport
	);

	if (NT_SUCCESS(status))
	{
		return TRUE;
	}

	if (status != STATUS_NO_MORE_ENTRIES)
	{
		TraceError(
			TRACE_DSHIDMINIDRV,
			"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_VirtualHidMini_InputReportGenerate", status);
	}

	return FALSE;
}

static
VOID
DSHM_EnqueueCgsReport(
	_In_ DMF_CONTEXT_DsHidMini* ModuleDeviceContext,
	_In_ ULONG ReportSize
)
{
	const UCHAR reportId = ModuleDeviceContext->InputReport[0];
	ULONG index;

	//
	// One slot per collection (gamepad / accel / gyro). A newer sample
	// replaces the same report ID in place so gamepad or accel traffic
	// cannot evict a queued gyrometer report.
	//
	for (index = 0; index < ModuleDeviceContext->CgsQueueCount; index++)
	{
		if (ModuleDeviceContext->CgsQueuedReports[index][0] == reportId)
		{
			RtlCopyMemory(
				ModuleDeviceContext->CgsQueuedReports[index],
				ModuleDeviceContext->InputReport,
				ReportSize
			);
			ModuleDeviceContext->CgsQueuedSizes[index] = ReportSize;
			return;
		}
	}

	if (ModuleDeviceContext->CgsQueueCount >= ARRAYSIZE(ModuleDeviceContext->CgsQueuedSizes))
	{
		return;
	}

	index = ModuleDeviceContext->CgsQueueCount;
	RtlCopyMemory(
		ModuleDeviceContext->CgsQueuedReports[index],
		ModuleDeviceContext->InputReport,
		ReportSize
	);
	ModuleDeviceContext->CgsQueuedSizes[index] = ReportSize;
	ModuleDeviceContext->CgsQueueCount++;
}

static
VOID
DSHM_DequeueCgsReportById(
	_In_ DMF_CONTEXT_DsHidMini* ModuleDeviceContext,
	_In_ UCHAR ReportId
)
{
	ULONG index;

	for (index = 0; index < ModuleDeviceContext->CgsQueueCount; index++)
	{
		if (ModuleDeviceContext->CgsQueuedReports[index][0] != ReportId)
		{
			continue;
		}

		const ULONG remaining = ModuleDeviceContext->CgsQueueCount - index - 1;

		if (remaining > 0)
		{
			RtlMoveMemory(
				ModuleDeviceContext->CgsQueuedReports[index],
				ModuleDeviceContext->CgsQueuedReports[index + 1],
				sizeof(ModuleDeviceContext->CgsQueuedReports[0]) * remaining
			);
			RtlMoveMemory(
				&ModuleDeviceContext->CgsQueuedSizes[index],
				&ModuleDeviceContext->CgsQueuedSizes[index + 1],
				sizeof(ModuleDeviceContext->CgsQueuedSizes[0]) * remaining
			);
		}

		ModuleDeviceContext->CgsQueueCount--;
		return;
	}
}

static
VOID
DSHM_FlushCgsQueue(
	_In_ DMF_CONTEXT_DsHidMini* ModuleDeviceContext
)
{
	while (ModuleDeviceContext->CgsQueueCount > 0)
	{
		const ULONG size = ModuleDeviceContext->CgsQueuedSizes[0];

		RtlCopyMemory(
			ModuleDeviceContext->InputReport,
			ModuleDeviceContext->CgsQueuedReports[0],
			size
		);
		ModuleDeviceContext->InputReportSize = size;

		if (!DSHM_TrySendStagedInputReport(ModuleDeviceContext))
		{
			return;
		}

		if (ModuleDeviceContext->CgsQueueCount > 1)
		{
			RtlMoveMemory(
				ModuleDeviceContext->CgsQueuedReports[0],
				ModuleDeviceContext->CgsQueuedReports[1],
				sizeof(ModuleDeviceContext->CgsQueuedReports[0]) * (ModuleDeviceContext->CgsQueueCount - 1)
			);
			RtlMoveMemory(
				&ModuleDeviceContext->CgsQueuedSizes[0],
				&ModuleDeviceContext->CgsQueuedSizes[1],
				sizeof(ModuleDeviceContext->CgsQueuedSizes[0]) * (ModuleDeviceContext->CgsQueueCount - 1)
			);
		}

		ModuleDeviceContext->CgsQueueCount--;
	}
}

//
// Sends one report of the CGS mode; each report has its own size. If no HID
// read is pending the report is queued and retried on the next Generate.
// 
static
VOID
DSHM_GenerateInputReport(
	_In_ DMF_CONTEXT_DsHidMini* ModuleDeviceContext,
	_In_ ULONG ReportSize
)
{
	ModuleDeviceContext->InputReportSize = ReportSize;

	if (DSHM_TrySendStagedInputReport(ModuleDeviceContext))
	{
		DSHM_DequeueCgsReportById(ModuleDeviceContext, ModuleDeviceContext->InputReport[0]);
		DSHM_FlushCgsQueue(ModuleDeviceContext);
	}
	else
	{
		DSHM_EnqueueCgsReport(ModuleDeviceContext, ReportSize);
	}
}

//
// CGS: gamepad report (ID 1) followed by accelerometer and gyrometer
// HID Sensor reports (IDs 0x30 and 0x31)
// 
static
VOID
DSHM_ProcessCgsInputReport(
	_In_ const PDEVICE_CONTEXT DeviceContext,
	_In_ DMF_CONTEXT_DsHidMini* ModuleDeviceContext,
	_In_ const PDS3_RAW_INPUT_REPORT Report
)
{
	const PDS_MOTION_STATE motion = &DeviceContext->Motion;
	DS_MOTION_HID_FRAME frame;

	DSHM_FlushCgsQueue(ModuleDeviceContext);

	DS3_RAW_TO_SDF_HID_INPUT_REPORT(
		Report,
		ModuleDeviceContext->InputReport,
		DsPressureExposureModeDigital,
		DsDPadExposureModeHAT,
		&DeviceContext->Configuration.ThumbSettings,
		&DeviceContext->Configuration.FlipAxis
	);
	DSHM_GenerateInputReport(ModuleDeviceContext, DS3_CGP_HID_INPUT_REPORT_SIZE);

	if (!motion->HasSample)
	{
		return;
	}

	DsMotionHid_ToDeviceFrame(
		motion->Sample.AccelMilliGX,
		motion->Sample.AccelMilliGY,
		motion->Sample.AccelMilliGZ,
		motion->Sample.GyroMilliDps,
		&frame
	);

	if (motion->SensorHid[DS_MOTION_HID_SENSOR_ACCEL].ReportingState != DS_MOTION_HID_REPORTING_STATE_NO_EVENTS
		&& motion->SensorHid[DS_MOTION_HID_SENSOR_ACCEL].ReportingState != DS_MOTION_HID_REPORTING_STATE_NO_EVENTS_WAKE)
	{
		INT32 accel[3];

		for (int i = 0; i < 3; i++)
		{
			accel[i] = DsMotionHid_MilliGToMilliMetersPerSecondSquared(frame.AccelMilliG[i]);
		}

		DsMotionHid_WriteSensorInputReport(
			DS_MOTION_HID_REPORT_ID_ACCEL,
			DS_MOTION_HID_STATUS_READY,
			accel,
			ModuleDeviceContext->InputReport
		);
		DSHM_GenerateInputReport(ModuleDeviceContext, DS_MOTION_HID_SENSOR_INPUT_REPORT_SIZE);
	}

	if (motion->SensorHid[DS_MOTION_HID_SENSOR_GYRO].ReportingState != DS_MOTION_HID_REPORTING_STATE_NO_EVENTS
		&& motion->SensorHid[DS_MOTION_HID_SENSOR_GYRO].ReportingState != DS_MOTION_HID_REPORTING_STATE_NO_EVENTS_WAKE)
	{
		INT32 gyro[3];

		for (int i = 0; i < 3; i++)
		{
			gyro[i] = (frame.GyroMilliDps[i] * DS_MOTION_HID_GYRO_UNIT_PER_DPS) / 1000;
		}

		DsMotionHid_WriteSensorInputReport(
			DS_MOTION_HID_REPORT_ID_GYRO,
			DS_MOTION_HID_STATUS_READY,
			gyro,
			ModuleDeviceContext->InputReport
		);
		DSHM_GenerateInputReport(ModuleDeviceContext, DS_MOTION_HID_SENSOR_INPUT_REPORT_SIZE);
	}
}


//
// Protocol-agnostic function that transforms the raw input report to HID-mode-compatible ones
// 
_Use_decl_annotations_
void
DSHM_ParseInputReport(
	_In_ const PDEVICE_CONTEXT DeviceContext,
	_In_ DMF_CONTEXT_DsHidMini* ModuleDeviceContext,
	_In_ const PDS3_RAW_INPUT_REPORT Report
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

#pragma region IPC Copy

	const WDFDRIVER driver = WdfGetDriver();
	const PDSHM_DRIVER_CONTEXT pDrvCtx = DriverGetContext(driver);

	{
		LONGLONG timeout = 0;

		if (WdfWaitLockAcquire(pDrvCtx->IpcLock, &timeout) == STATUS_SUCCESS)
		{
			if (pDrvCtx->IPC.IsEnabled && pDrvCtx->IPC.SharedRegions.HID.Buffer != NULL)
			{
				/*
				 * Offset calculation puts each devices' input report copy
				 * in their respective position in the memory region, like:
				 *   1st device: (sizeof(IPC_HID_INPUT_REPORT_MESSAGE) * 0) = 0
				 *   2nd device: (sizeof(IPC_HID_INPUT_REPORT_MESSAGE) * 1) = 60
				 *   3rd device: (sizeof(IPC_HID_INPUT_REPORT_MESSAGE) * 2) = 120
				 * and so on
				 */
				const size_t offset = (sizeof(IPC_HID_INPUT_REPORT_MESSAGE) * (DeviceContext->SlotIndex - 1));
				const PIPC_HID_INPUT_REPORT_MESSAGE pHIDBuffer = (PIPC_HID_INPUT_REPORT_MESSAGE)(pDrvCtx->IPC.SharedRegions.HID.Buffer +
					offset);

				// odd generation: readers must retry until the snapshot is stable
				InterlockedIncrement(&pHIDBuffer->SequenceNumber);
				ResetEvent(DeviceContext->IPC.InputReportWaitHandle);

				pHIDBuffer->SlotIndex = DeviceContext->SlotIndex;
				RtlCopyMemory(&pHIDBuffer->InputReport, Report, sizeof(DS3_RAW_INPUT_REPORT));

				// even generation: snapshot is complete; wake every waiter
				InterlockedIncrement(&pHIDBuffer->SequenceNumber);
				SetEvent(DeviceContext->IPC.InputReportWaitHandle);
			}

			if (pDrvCtx->IPC.IsEnabled && pDrvCtx->IPC.SharedRegions.Motion.Buffer != NULL)
			{
				DsMotion_PublishOrClearIpcSnapshot(DeviceContext, FALSE);
			}

			WdfWaitLockRelease(pDrvCtx->IpcLock);
		}
	}

#pragma endregion

#pragma region HID Input Report (SDF, GPJ ID 01) processing

	switch (DeviceContext->Configuration.HidDeviceMode) // NOLINT(clang-diagnostic-switch-enum)
	{
	case DsHidMiniDeviceModeGPJ:

		DS3_RAW_TO_GPJ_HID_INPUT_REPORT_01(
			Report,
			ModuleDeviceContext->InputReport,
			DeviceContext->Configuration.GPJ.PressureExposureMode,
			DeviceContext->Configuration.GPJ.DPadExposureMode,
			&DeviceContext->Configuration.ThumbSettings,
			&DeviceContext->Configuration.FlipAxis
		);

#ifdef DBG
		DumpAsHex(">> MULTI", ModuleDeviceContext->InputReport, DS3_SDF_GPJ_HID_INPUT_REPORT_SIZE);
#endif

		break;
	case DsHidMiniDeviceModeSDF:

		DS3_RAW_TO_SDF_HID_INPUT_REPORT(
			Report,
			ModuleDeviceContext->InputReport,
			DeviceContext->Configuration.SDF.PressureExposureMode,
			DeviceContext->Configuration.SDF.DPadExposureMode,
			&DeviceContext->Configuration.ThumbSettings,
			&DeviceContext->Configuration.FlipAxis
		);

		break;
	case DsHidMiniDeviceModeCGP:

		//
		// Reuses the proven SDF button/axis packing, but forced to digital
		// pressure exposure and HAT D-pad mode: CGP's report descriptor has
		// no pressure-slider elements to write analogue values into, and a
		// fixed, minimal layout is what keeps this mode DirectInput-friendly
		// (see issue #68).
		// 
		DS3_RAW_TO_SDF_HID_INPUT_REPORT(
			Report,
			ModuleDeviceContext->InputReport,
			DsPressureExposureModeDigital,
			DsDPadExposureModeHAT,
			&DeviceContext->Configuration.ThumbSettings,
			&DeviceContext->Configuration.FlipAxis
		);

		break;
	case DsHidMiniDeviceModeCGS:

		//
		// Sends its own set of reports; nothing left to do here
		// 
		DSHM_ProcessCgsInputReport(DeviceContext, ModuleDeviceContext, Report);

		FuncExitNoReturn(TRACE_DSHIDMINIDRV);

		return;
	default:
		break;
	}

	//
	// Notify new Input Report is available
	// 
	NTSTATUS status = DMF_VirtualHidMini_InputReportGenerate(
		ModuleDeviceContext->DmfModuleVirtualHidMini,
		DsHidMini_RetrieveNextInputReport
	);
	if (!NT_SUCCESS(status) && status != STATUS_NO_MORE_ENTRIES)
	{
		TraceError(
			TRACE_DSHIDMINIDRV,
			"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_VirtualHidMini_InputReportGenerate", status);
	}

#pragma endregion

#pragma region HID Input Report (GPJ ID 02) processing

	if (DeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeGPJ
		&& (DeviceContext->Configuration.GPJ.PressureExposureMode & DsPressureExposureModeAnalogue) != 0)
	{
		DS3_RAW_TO_GPJ_HID_INPUT_REPORT_02(
			Report,
			ModuleDeviceContext->InputReport
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			ModuleDeviceContext->DmfModuleVirtualHidMini,
			DsHidMini_RetrieveNextInputReport
		);
		if (!NT_SUCCESS(status) && status != STATUS_NO_MORE_ENTRIES)
		{
			TraceError(
				TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_VirtualHidMini_InputReportGenerate", status);
		}
	}

#pragma endregion

#pragma region HID Input Report (SIXAXIS compatible) processing

	if (DeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		DS3_RAW_TO_SIXAXIS_HID_INPUT_REPORT(
			Report,
			ModuleDeviceContext->InputReport,
			&DeviceContext->Configuration.ThumbSettings,
			&DeviceContext->Configuration.FlipAxis
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			ModuleDeviceContext->DmfModuleVirtualHidMini,
			DsHidMini_RetrieveNextInputReport
		);
		if (!NT_SUCCESS(status) && status != STATUS_NO_MORE_ENTRIES)
		{
			TraceError(
				TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_VirtualHidMini_InputReportGenerate", status);
		}
	}

#pragma endregion

#pragma region HID Input Report (DualShock 4 Rev1 compatible) processing

	if (DeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeDS4WindowsCompatible)
	{
		DS3_RAW_TO_DS4WINDOWS_HID_INPUT_REPORT(
			Report,
			ModuleDeviceContext->InputReport,
			(DeviceContext->ConnectionType == DsDeviceConnectionTypeUsb) ? TRUE : FALSE,
			&DeviceContext->Configuration.ThumbSettings,
			&DeviceContext->Configuration.FlipAxis
		);

		//
		// Gyro, accelerometer and timestamp from the canonical motion sample
		// 
		DSHM_FillDs4Motion(DeviceContext, ModuleDeviceContext->InputReport);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			ModuleDeviceContext->DmfModuleVirtualHidMini,
			DsHidMini_RetrieveNextInputReport
		);
		if (!NT_SUCCESS(status) && status != STATUS_NO_MORE_ENTRIES)
		{
			TraceError(
				TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_VirtualHidMini_InputReportGenerate", status);
		}
	}

#pragma endregion

#pragma region HID Input Report (XINPUT compatible HID device) processing

	if (DeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible)
	{
		DS3_RAW_TO_XINPUTHID_HID_INPUT_REPORT(
			Report,
			// ReSharper disable once CppRedundantCastExpression
			(PXINPUT_HID_INPUT_REPORT)ModuleDeviceContext->InputReport,
			(DeviceContext->ConnectionType == DsDeviceConnectionTypeUsb) ? TRUE : FALSE,
			&DeviceContext->Configuration.ThumbSettings,
			&DeviceContext->Configuration.FlipAxis
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			ModuleDeviceContext->DmfModuleVirtualHidMini,
			DsHidMini_RetrieveNextInputReport
		);
		if (!NT_SUCCESS(status) && status != STATUS_NO_MORE_ENTRIES)
		{
			TraceError(
				TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_VirtualHidMini_InputReportGenerate", status);
		}
	}

#pragma endregion

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}
