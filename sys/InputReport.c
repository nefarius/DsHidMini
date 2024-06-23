#include "Driver.h"
#include "InputReport.tmh"


_Use_decl_annotations_
void
DSHM_ParseInputReport(
	_In_ const PDEVICE_CONTEXT DeviceContext,
	_In_ DMF_CONTEXT_DsHidMini* ModuleDeviceContext,
	_In_ const PDS3_RAW_INPUT_REPORT Report
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

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
