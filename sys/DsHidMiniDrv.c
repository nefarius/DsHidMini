#include "Driver.h"
#include "DsHidMiniDrv.tmh"
#include "Debugging.h"


PWSTR G_DsHidMini_Strings[] =
{
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	L"DsHidMini Device" // TODO: populate properly
};

#define MAXIMUM_STRING_LENGTH           (126 * sizeof(WCHAR))
#define DSHIDMINI_DEVICE_STRING          L"DS3 Compatible HID Device"
#define DSHIDMINI_MANUFACTURER_STRING    L"Nefarius Software Solutions e.U."
#define DSHIDMINI_PRODUCT_STRING         L"DS3 Compatible HID Device"
#define DSHIDMINI_SERIAL_NUMBER_STRING   L"TODO: use device address"
#define DSHIDMINI_DEVICE_STRING_INDEX    5


// This macro declares the following function:
// DMF_CONTEXT_GET()
//
DMF_MODULE_DECLARE_CONTEXT(DsHidMini)

// This macro declares the following function:
// DMF_CONFIG_GET()
//
DMF_MODULE_DECLARE_CONFIG(DsHidMini)



//
// Bootstrap DMF initialization
// 
#pragma code_seg("PAGE")
_IRQL_requires_max_(PASSIVE_LEVEL)
_Must_inspect_result_
NTSTATUS
DMF_DsHidMini_Create(
	_In_ WDFDEVICE Device,
	_In_ DMF_MODULE_ATTRIBUTES* DmfModuleAttributes,
	_In_ WDF_OBJECT_ATTRIBUTES* ObjectAttributes,
	_Out_ DMFMODULE* DmfModule
)
{
	NTSTATUS ntStatus;
	DMF_MODULE_DESCRIPTOR dsHidMiniDesc;
	DMF_CALLBACKS_DMF dsHidMiniCallbacks;
	PDEVICE_CONTEXT pDevCtx;

	PAGED_CODE();

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	pDevCtx = DeviceGetContext(Device);

	//
	// Set defaults
	// 
	DS_DRIVER_CONFIGURATION_INIT_DEFAULTS(&pDevCtx->Configuration);

	//
	// Load volatile configuration
	// 
	DsConfig_Load(pDevCtx);

	DMF_CALLBACKS_DMF_INIT(&dsHidMiniCallbacks);
	dsHidMiniCallbacks.ChildModulesAdd = DMF_DsHidMini_ChildModulesAdd;
	dsHidMiniCallbacks.DeviceOpen = DMF_DsHidMini_Open;
	dsHidMiniCallbacks.DeviceClose = DMF_DsHidMini_Close;

	DMF_MODULE_DESCRIPTOR_INIT_CONTEXT_TYPE(dsHidMiniDesc,
		DsHidMini,
		DMF_CONTEXT_DsHidMini,
		DMF_MODULE_OPTIONS_PASSIVE,
		DMF_MODULE_OPEN_OPTION_OPEN_PrepareHardware);

	dsHidMiniDesc.CallbacksDmf = &dsHidMiniCallbacks;

	ntStatus = DMF_ModuleCreate(Device,
		DmfModuleAttributes,
		ObjectAttributes,
		&dsHidMiniDesc,
		DmfModule);
	if (!NT_SUCCESS(ntStatus))
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DSHIDMINIDRV, "DMF_ModuleCreate fails: ntStatus=%!STATUS!", ntStatus);
		goto Exit;
	}

Exit:

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");

	return(ntStatus);
}
#pragma code_seg()

//
// Bootstraps VirtualHidMini DMF module
// 
#pragma code_seg("PAGE")
_Function_class_(DMF_ChildModulesAdd)
_IRQL_requires_max_(PASSIVE_LEVEL)
VOID
DMF_DsHidMini_ChildModulesAdd(
	_In_ DMFMODULE DmfModule,
	_In_ DMF_MODULE_ATTRIBUTES* DmfParentModuleAttributes,
	_In_ PDMFMODULE_INIT DmfModuleInit
)
{
	DMF_MODULE_ATTRIBUTES moduleAttributes;
	DMF_CONTEXT_DsHidMini* moduleContext;
	DMF_CONFIG_VirtualHidMini vHidCfg;
	PDEVICE_CONTEXT pDevCtx;

	PAGED_CODE();

	UNREFERENCED_PARAMETER(DmfParentModuleAttributes);

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	moduleContext = DMF_CONTEXT_GET(DmfModule);
	pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));

	DMF_CONFIG_VirtualHidMini_AND_ATTRIBUTES_INIT(&vHidCfg,
		&moduleAttributes);

	vHidCfg.VendorId = pDevCtx->Configuration.VendorId;
	vHidCfg.ProductId = pDevCtx->Configuration.ProductId;
	vHidCfg.VersionNumber = pDevCtx->Configuration.VersionNumber;

	switch (pDevCtx->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeSingle:

		vHidCfg.HidDescriptor = &G_Ds3HidDescriptor_Single_Mode;
		vHidCfg.HidDescriptorLength = sizeof(G_Ds3HidDescriptor_Single_Mode);
		vHidCfg.HidReportDescriptor = G_Ds3HidReportDescriptor_Single_Mode;
		vHidCfg.HidReportDescriptorLength = G_Ds3HidDescriptor_Single_Mode.DescriptorList[0].wReportLength;

		break;
	case DsHidMiniDeviceModeMulti:

		vHidCfg.HidDescriptor = &G_Ds3HidDescriptor_Split_Mode;
		vHidCfg.HidDescriptorLength = sizeof(G_Ds3HidDescriptor_Split_Mode);
		vHidCfg.HidReportDescriptor = G_Ds3HidReportDescriptor_Split_Mode;
		vHidCfg.HidReportDescriptorLength = G_Ds3HidDescriptor_Split_Mode.DescriptorList[0].wReportLength;

		break;
	case DsHidMiniDeviceModeSixaxisCompatible:

		vHidCfg.HidDescriptor = &G_SixaxisHidDescriptor;
		vHidCfg.HidDescriptorLength = sizeof(G_SixaxisHidDescriptor);
		vHidCfg.HidReportDescriptor = G_SixaxisHidReportDescriptor;
		vHidCfg.HidReportDescriptorLength = G_SixaxisHidDescriptor.DescriptorList[0].wReportLength;

		break;
	default:
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSHIDMINIDRV,
			"Unknown HID Device Mode: 0x%02X", pDevCtx->Configuration.HidDeviceMode);
		return;
	}

	vHidCfg.HidDeviceAttributes.VendorID = pDevCtx->Configuration.VendorId;
	vHidCfg.HidDeviceAttributes.ProductID = pDevCtx->Configuration.ProductId;
	vHidCfg.HidDeviceAttributes.VersionNumber = pDevCtx->Configuration.VersionNumber;
	vHidCfg.HidDeviceAttributes.Size = sizeof(HID_DEVICE_ATTRIBUTES);

	vHidCfg.GetInputReport = DsHidMini_GetInputReport;
	vHidCfg.GetFeature = DsHidMini_GetFeature;
	vHidCfg.SetFeature = DsHidMini_SetFeature;
	vHidCfg.SetOutputReport = DsHidMini_SetOutputReport;
	vHidCfg.WriteReport = DsHidMini_WriteReport;

	vHidCfg.StringSizeCbManufacturer = sizeof(DSHIDMINI_MANUFACTURER_STRING);
	vHidCfg.StringManufacturer = DSHIDMINI_MANUFACTURER_STRING;
	vHidCfg.StringSizeCbProduct = sizeof(DSHIDMINI_PRODUCT_STRING);
	vHidCfg.StringProduct = DSHIDMINI_PRODUCT_STRING;
	vHidCfg.StringSizeCbSerialNumber = sizeof(DSHIDMINI_SERIAL_NUMBER_STRING);
	vHidCfg.StringSerialNumber = DSHIDMINI_SERIAL_NUMBER_STRING;

	vHidCfg.Strings = G_DsHidMini_Strings;
	vHidCfg.NumberOfStrings = ARRAYSIZE(G_DsHidMini_Strings);

	DMF_DmfModuleAdd(DmfModuleInit,
		&moduleAttributes,
		WDF_NO_OBJECT_ATTRIBUTES,
		&moduleContext->DmfModuleVirtualHidMini);

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");
}
#pragma code_seg()

#pragma code_seg("PAGE")
_Function_class_(DMF_Open)
_IRQL_requires_max_(PASSIVE_LEVEL)
_Must_inspect_result_
static
NTSTATUS
DMF_DsHidMini_Open(
	_In_ DMFMODULE DmfModule
)
{
	NTSTATUS			status = STATUS_SUCCESS;

	UNREFERENCED_PARAMETER(DmfModule);

	PAGED_CODE();

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	//
	// Increase pad instance count
	// 
	numInstances++;

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");

	return status;
}
#pragma code_seg()

#pragma code_seg("PAGE")
_Function_class_(DMF_Close)
_IRQL_requires_max_(PASSIVE_LEVEL)
static
VOID
DMF_DsHidMini_Close(
	_In_ DMFMODULE DmfModule
)
{
	PDEVICE_CONTEXT pDevCtx;

	PAGED_CODE();

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));

	//
	// Decrease pad instance count
	// 
	numInstances--;

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");
}
#pragma code_seg()

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

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");

	return STATUS_UNSUCCESSFUL;
}

//
// Submit new HID Input Report.
// 
NTSTATUS
DsHidMini_RetrieveNextInputReport(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_Out_ UCHAR** Buffer,
	_Out_ ULONG* BufferSize
)
{
	NTSTATUS status = STATUS_SUCCESS;
	DMFMODULE dmfModuleParent;
	DMF_CONTEXT_DsHidMini* moduleContext;
	PDEVICE_CONTEXT pDevCtx;

	UNREFERENCED_PARAMETER(Request);

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	dmfModuleParent = DMF_ParentModuleGet(DmfModule);
	moduleContext = DMF_CONTEXT_GET(dmfModuleParent);
	pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));

	*Buffer = moduleContext->InputReport;

	switch (pDevCtx->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeSingle:
	case DsHidMiniDeviceModeMulti:
		*BufferSize = DS3_HID_INPUT_REPORT_SIZE;
		break;
	case DsHidMiniDeviceModeSixaxisCompatible:
		*BufferSize = SIXAXIS_HID_INPUT_REPORT_SIZE;
		break;
	default:
		TraceEvents(TRACE_LEVEL_WARNING,
			TRACE_DSHIDMINIDRV,
			"Unsupported HID device mode: 0x%04X",
			pDevCtx->Configuration.HidDeviceMode
		);
		status = STATUS_INVALID_PARAMETER;
		break;
	}

	/*
#ifdef DBG
	DumpAsHex(">> Report", *Buffer, (ULONG)*BufferSize);
#endif
	*/

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Exit (%!STATUS!)", status);

	return status;
}

//
// Handles GET_FEATURE requests
// 
NTSTATUS
DsHidMini_GetFeature(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx;

	UNREFERENCED_PARAMETER(Request);


	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));

	PPID_POOL_REPORT pPool;
	PPID_BLOCK_LOAD_REPORT pBlockLoad;

	TraceDbg(TRACE_DSHIDMINIDRV, "-- Packet->reportId: %d, Packet->reportBufferLen: %d",
	         Packet->reportId,
	         Packet->reportBufferLen);


#ifdef DSHM_FEATURE_FFB

	switch (Packet->reportId)
	{
	case PID_POOL_REPORT_ID:

		TraceDbg(TRACE_DSHIDMINIDRV, "!! PID_POOL_REPORT_ID");
		
		pPool = (PPID_POOL_REPORT)Packet->reportBuffer;

		pPool->ReportID = PID_POOL_REPORT_ID;
		pPool->RAMPoolSize = 65535;
		pPool->SimultaneousEffectsMax = MAX_EFFECT_BLOCKS;
		pPool->DeviceManagedPool = 1;
		pPool->SharedParameterBlocks = 0;

		*ReportSize = sizeof(PID_POOL_REPORT);
		
		break;

	case PID_BLOCK_LOAD_REPORT_ID:

		TraceDbg(TRACE_DSHIDMINIDRV, "!! PID_BLOCK_LOAD_REPORT_ID");
				
		pBlockLoad = (PPID_BLOCK_LOAD_REPORT)Packet->reportBuffer;

		pBlockLoad->ReportID = PID_BLOCK_LOAD_REPORT_ID;
		pBlockLoad->EffectBlockIndex = 1; // TODO: just an example
		pBlockLoad->BlockLoadStatus = 1;
		pBlockLoad->RAMPoolAvailable = 65535;

		*ReportSize = sizeof(PID_BLOCK_LOAD_REPORT);

		break;

	default:
		TraceEvents(TRACE_LEVEL_WARNING, 
			TRACE_DSHIDMINIDRV, "%!FUNC! Not implemented");
		break;
	}
	
#endif

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit (%!STATUS!)", status);

	return status;
}

//
// Handles SET_FEATURE requests
// 
NTSTATUS
DsHidMini_SetFeature(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	NTSTATUS status = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevCtx;

	UNREFERENCED_PARAMETER(Request);


	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));

	PPID_CREATE_NEW_EFFECT_REPORT pNewEffect;

	TraceDbg(TRACE_DSHIDMINIDRV, "-- Packet->reportId: %d, Packet->reportBufferLen: %d",
	         Packet->reportId,
	         Packet->reportBufferLen);

	DumpAsHex("-- SET_FEATURE.reportBuffer", Packet->reportBuffer, Packet->reportBufferLen);

#ifdef DSHM_FEATURE_FFB

	switch (Packet->reportId)
	{
	case PID_NEW_EFFECT_REPORT_ID:

		pNewEffect = (PPID_CREATE_NEW_EFFECT_REPORT)Packet->reportBuffer;

		TraceDbg(TRACE_DSHIDMINIDRV, "!! PID_CREATE_NEW_EFFECT_REPORT");

		switch (pNewEffect->EffectType)
		{
		case PidEtConstantForce:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Constant Force");
			break;
		case PidEtRamp:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Ramp");
			break;
		case PidEtSquare:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Square");
			break;
		case PidEtSine:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Sine");
			break;
		case PidEtTriangle:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Triangle");
			break;
		case PidEtSawtoothUp:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Sawtooth Up");
			break;
		case PidEtSawtoothDown:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Sawtooth Down");
			break;
		case PidEtSpring:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Spring");
			break;
		case PidEtDamper:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Damper");
			break;
		case PidEtInertia:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Inertia");
			break;
		case PidEtFriction:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! ET Friction");
			break;
		}

		*ReportSize = Packet->reportBufferLen;
		
		break;
	default:
		TraceEvents(TRACE_LEVEL_WARNING, 
			TRACE_DSHIDMINIDRV, "%!FUNC! Not implemented");
		break;
	}

#endif

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit (%!STATUS!)", status);

	return status;
}

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

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	//
	// TODO: implement me!
	// 

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");

	return STATUS_NOT_IMPLEMENTED;
}

NTSTATUS
DsHidMini_WriteReport(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	NTSTATUS status = STATUS_SUCCESS;
	
	UNREFERENCED_PARAMETER(DmfModule);
	UNREFERENCED_PARAMETER(Request);
	UNREFERENCED_PARAMETER(ReportSize);

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	PPID_DEVICE_CONTROL_REPORT pDeviceControl;
	PPID_DEVICE_GAIN_REPORT pGain;

	TraceDbg(TRACE_DSHIDMINIDRV, "-- Packet->reportId: %d, Packet->reportBufferLen: %d",
	         Packet->reportId,
	         Packet->reportBufferLen);

	DumpAsHex("-- WRITE_REPORT.reportBuffer", Packet->reportBuffer, Packet->reportBufferLen);

#ifdef DSHM_FEATURE_FFB

	switch (Packet->reportId)
	{
	case PID_DEVICE_CONTROL_REPORT_ID:

		pDeviceControl = (PPID_DEVICE_CONTROL_REPORT)Packet->reportBuffer;

		switch (pDeviceControl->Control)
		{
		case PidDcEnableActuators:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! DC Enable Actuators");
			break;
		case PidDcDisableActuators:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! DC Disable Actuators");
			break;
		case PidDcStopAllEffects:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! DC Stop All Effects");
			break;
		case PidDcReset:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! DC Reset");
			break;
		case PidDcPause:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! DC Pause");
			break;
		case PidDcContinue:
			TraceDbg(TRACE_DSHIDMINIDRV, "!! DC Continue");
			break;
		}

		*ReportSize = Packet->reportBufferLen;
		
		break;

	case PID_DEVICE_GAIN_REPORT_ID:

		pGain = (PPID_DEVICE_GAIN_REPORT)Packet->reportBuffer;

		TraceDbg(TRACE_DSHIDMINIDRV, "!! PID_DEVICE_GAIN_REPORT, DeviceGain: %d", pGain->DeviceGain);

		*ReportSize = Packet->reportBufferLen;
		
		break;

	default:
		TraceEvents(TRACE_LEVEL_WARNING, 
			TRACE_DSHIDMINIDRV, "%!FUNC! Not implemented");
		break;
	}
	
#endif

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit (%!STATUS!)", status);

	return status;
}

#pragma region Input Report processing

VOID Ds_ProcessHidInputReport(PDEVICE_CONTEXT Context, PUCHAR Buffer, size_t BufferLength)
{
	NTSTATUS				status;
	DMFMODULE               dmfModule;
	DMF_CONTEXT_DsHidMini* moduleContext;

	UNREFERENCED_PARAMETER(BufferLength);

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	dmfModule = (DMFMODULE)Context->DsHidMiniModule;
	moduleContext = DMF_CONTEXT_GET(dmfModule);

#pragma region HID Input Report (ID 01) processing

	switch (Context->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeMulti:

		DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_01(
			Buffer,
			moduleContext->InputReport,
			Context->Configuration.MuteDigitalPressureButtons
		);

#ifdef DBG
		DumpAsHex(">> MULTI", moduleContext->InputReport, DS3_HID_INPUT_REPORT_SIZE);
#endif

		break;
	case DsHidMiniDeviceModeSingle:

		DS3_RAW_TO_SINGLE_HID_INPUT_REPORT(
			Buffer,
			moduleContext->InputReport,
			Context->Configuration.MuteDigitalPressureButtons
		);

		/*
#ifdef DBG
		DumpAsHex(">> SINGLE", moduleContext->InputReport, DS3_HID_INPUT_REPORT_SIZE);
#endif
		*/

		break;
	default:
		break;
	}

	//
	// Notify new Input Report is available
	// 
	status = DMF_VirtualHidMini_InputReportGenerate(
		moduleContext->DmfModuleVirtualHidMini,
		DsHidMini_RetrieveNextInputReport
	);
	if (!NT_SUCCESS(status) && status != STATUS_NO_MORE_ENTRIES)
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DSHIDMINIDRV,
			"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
	}

#pragma endregion

#pragma region HID Input Report (ID 02) processing

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeMulti)
	{
		DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_02(
			Buffer,
			moduleContext->InputReport
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			moduleContext->DmfModuleVirtualHidMini,
			DsHidMini_RetrieveNextInputReport
		);
		if (!NT_SUCCESS(status) && status != STATUS_NO_MORE_ENTRIES)
		{
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
		}
	}

#pragma endregion

#pragma region HID Input Report (SIXAXIS compatible) processing

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		DS3_RAW_TO_SIXAXIS_HID_INPUT_REPORT(
			Buffer,
			moduleContext->InputReport
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			moduleContext->DmfModuleVirtualHidMini,
			DsHidMini_RetrieveNextInputReport
		);
		if (!NT_SUCCESS(status) && status != STATUS_NO_MORE_ENTRIES)
		{
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
		}
	}

#pragma endregion

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");
}

//
// Called when data is available on the USB Interrupt IN pipe.
//  
VOID DsUsb_EvtUsbInterruptPipeReadComplete(
	WDFUSBPIPE  Pipe,
	WDFMEMORY   Buffer,
	size_t      NumBytesTransferred,
	WDFCONTEXT  Context
)
{
	PDEVICE_CONTEXT         pDevCtx;
	size_t                  rdrBufferLength;
	LPVOID                  rdrBuffer;
	LARGE_INTEGER			freq, * t1, t2;
	LONGLONG				ms;
	DS_BATTERY_STATUS		battery;

	UNREFERENCED_PARAMETER(Pipe);
	UNREFERENCED_PARAMETER(NumBytesTransferred);

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	pDevCtx = DeviceGetContext(Context);
	rdrBuffer = WdfMemoryGetBuffer(Buffer, &rdrBufferLength);

	QueryPerformanceFrequency(&freq);
	t1 = &pDevCtx->Connection.Usb.ChargingCycleTimestamp;

	/*
#ifdef DBG
	DumpAsHex(">> USB", rdrBuffer, (ULONG)rdrBufferLength);
#endif
	*/

	battery = (DS_BATTERY_STATUS)((PUCHAR)rdrBuffer)[30];

	//
	// Check if state has changed to Charged
	// 
	if (battery == DsBatteryStatusCharged
		&& battery != pDevCtx->BatteryStatus)
	{
		pDevCtx->BatteryStatus = battery;

		DS3_USB_SET_LED(pDevCtx->Connection.Usb.OutputReport, DS3_LED_4);

		(void)SendControlRequest(
			pDevCtx,
			BmRequestHostToDevice,
			BmRequestClass,
			SetReport,
			USB_SETUP_VALUE(HidReportRequestTypeOutput, HidReportRequestIdOne),
			0,
			(PVOID)pDevCtx->Connection.Usb.OutputReport,
			DS3_USB_HID_OUTPUT_REPORT_SIZE);
	}
	//
	// If charging, cycle LEDs
	// 
	else if (battery == DsBatteryStatusCharging)
	{
		if (pDevCtx->Connection.Usb.ChargingCycleTimestamp.QuadPart == 0)
		{
			QueryPerformanceCounter(t1);
		}

		QueryPerformanceCounter(&t2);

		ms = (t2.QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

		//
		// 1 second passed
		// 
		if (ms > 1000)
		{
			//
			// Reset
			// 
			pDevCtx->Connection.Usb.ChargingCycleTimestamp.QuadPart = 0;
			
			UCHAR led = DS3_USB_GET_LED(pDevCtx->Connection.Usb.OutputReport) << 1;

			//
			// Cycle through
			// 
			if (led > DS3_LED_4 || led < DS3_LED_1)
			{
				led = DS3_LED_1;
			}

			DS3_USB_SET_LED(pDevCtx->Connection.Usb.OutputReport, led);

			(void)SendControlRequest(
				pDevCtx,
				BmRequestHostToDevice,
				BmRequestClass,
				SetReport,
				USB_SETUP_VALUE(HidReportRequestTypeOutput, HidReportRequestIdOne),
				0,
				(PVOID)pDevCtx->Connection.Usb.OutputReport,
				DS3_USB_HID_OUTPUT_REPORT_SIZE);
		}
	}
	else
	{
		pDevCtx->BatteryStatus = battery;
	}
	
	Ds_ProcessHidInputReport(pDevCtx, rdrBuffer, rdrBufferLength);

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");
}

//
// Called when data is available on the wireless HID Interrupt channel.
// 
void DsBth_HidInterruptReadRequestCompletionRoutine(
	WDFREQUEST Request,
	WDFIOTARGET Target,
	PWDF_REQUEST_COMPLETION_PARAMS Params,
	WDFCONTEXT Context
)
{
	NTSTATUS                    status;
	PUCHAR                      buffer;
	size_t                      bufferLength;
	PUCHAR						inputBuffer;
	WDF_REQUEST_REUSE_PARAMS    params;
	PDEVICE_CONTEXT				pDeviceContext;
	LARGE_INTEGER				freq, * t1, t2;
	LONGLONG					ms;
	DS_BATTERY_STATUS			battery;
	PUCHAR						outputBuffer;


	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	UNREFERENCED_PARAMETER(Target);

#ifdef DBG
	TraceDbg(
		TRACE_DSHIDMINIDRV,
		"++ Completion status: %!STATUS!",
		Params->IoStatus.Status
	);
#endif

	//
	// Device has been disconnected, quit requesting inputs
	// 
	if (Params->IoStatus.Status == STATUS_DEVICE_NOT_CONNECTED)
	{
		goto Exit;
	}

	pDeviceContext = (PDEVICE_CONTEXT)Context;
	QueryPerformanceFrequency(&freq);
	t1 = &pDeviceContext->Connection.Bth.QuickDisconnectTimestamp;

	if (!NT_SUCCESS(Params->IoStatus.Status)
		|| Params->Parameters.Ioctl.Output.Length < BTHPS3_SIXAXIS_HID_INPUT_REPORT_SIZE)
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DSHIDMINIDRV,
			"%!FUNC! failed with status %!STATUS!", Params->IoStatus.Status);

		goto resendRequest;
	}

	buffer = (PUCHAR)WdfMemoryGetBuffer(
		Params->Parameters.Ioctl.Output.Buffer,
		&bufferLength);

#ifdef DBG
	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "!! buffer: 0x%p, bufferLength: %d",
		buffer, (ULONG)bufferLength);

	DumpAsHex(">> BTH", buffer, (ULONG)bufferLength);
#endif

	/*
	* When connected via Bluetooth the Sixaxis occasionally sends
	* a report with the second byte 0xff and the rest zeroed.
	*
	* This report does not reflect the actual state of the
	* controller must be ignored to avoid generating false input
	* events.
	*/
	if (buffer[2] == 0xFF)
	{
		goto resendRequest;
	}

	//
	// Skip to report ID
	// 
	inputBuffer = &buffer[1];

	//
	// Grab battery info
	// 
	battery = (DS_BATTERY_STATUS)((PUCHAR)inputBuffer)[30];

	//
	// React if last known state differs from current state
	// 
	if (pDeviceContext->BatteryStatus != battery)
	{
		TraceDbg(
			TRACE_DSHIDMINIDRV,
			"Battery status changed to %d",
			battery
		);

		outputBuffer = WdfMemoryGetBuffer(
			pDeviceContext->Connection.Bth.HidControl.WriteMemory,
			NULL
		);

		//
		// Don't send update if not initialized yet
		// 
		if (DS3_BTH_GET_LED(outputBuffer) != 0x00)
		{
			switch (battery)
			{
			case DsBatteryStatusCharged:
			case DsBatteryStatusFull:
			case DsBatteryStatusHigh:
				DS3_BTH_SET_LED(outputBuffer, DS3_LED_4);
				break;
			case DsBatteryStatusMedium:
				DS3_BTH_SET_LED(outputBuffer, DS3_LED_3);
				break;
			case DsBatteryStatusLow:
				DS3_BTH_SET_LED(outputBuffer, DS3_LED_2);
				break;
			case DsBatteryStatusDying:
				DS3_BTH_SET_LED(outputBuffer, DS3_LED_1);
				break;
			default:
				break;
			}

			(void)DsBth_SendHidControlWriteRequestAsync(pDeviceContext);
		}

		//
		// Update battery status
		// 
		pDeviceContext->BatteryStatus = battery;
	}

	//
	// Quick disconnect combo (L1 + R1 + PS) detected
	// 
	if (((inputBuffer[3] >> 3) & 1U) && ((inputBuffer[3] >> 2) & 1U) && (inputBuffer[4] & 1U))
	{
		TraceEvents(TRACE_LEVEL_INFORMATION,
			TRACE_DSHIDMINIDRV,
			"!! Quick disconnect combination detected"
		);

		if (pDeviceContext->Connection.Bth.QuickDisconnectTimestamp.QuadPart == 0)
		{
			QueryPerformanceCounter(t1);
		}

		QueryPerformanceCounter(&t2);

		ms = (t2.QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

		//
		// 1 second passed
		// 
		if (ms > 1000)
		{
			TraceEvents(TRACE_LEVEL_INFORMATION,
				TRACE_DSHIDMINIDRV,
				"!! Sending disconnect request"
			);

			//
			// Send disconnect request
			// 
			status = DsBth_SendDisconnectRequest(pDeviceContext);

			if (!NT_SUCCESS(status))
			{
				TraceEvents(TRACE_LEVEL_ERROR,
					TRACE_DSHIDMINIDRV,
					"Sending disconnect request failed with status %!STATUS!",
					status
				);
			}

			//
			// No further processing
			// 
			return;
		}
	}
	else
	{
		pDeviceContext->Connection.Bth.QuickDisconnectTimestamp.QuadPart = 0;
	}

	Ds_ProcessHidInputReport(pDeviceContext, inputBuffer, bufferLength - 1);

	resendRequest:

	WDF_REQUEST_REUSE_PARAMS_INIT(
		&params,
		WDF_REQUEST_REUSE_NO_FLAGS,
		STATUS_SUCCESS
	);
	status = WdfRequestReuse(Request, &params);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSHIDMINIDRV,
			"WdfRequestReuse failed with status %!STATUS!",
			status
		);
		return;
	}

	status = WdfIoTargetFormatRequestForIoctl(
		pDeviceContext->Connection.Bth.BthIoTarget,
		pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest,
		IOCTL_BTHPS3_HID_INTERRUPT_READ,
		NULL,
		NULL,
		pDeviceContext->Connection.Bth.HidInterrupt.ReadMemory,
		NULL
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSHIDMINIDRV,
			"WdfIoTargetFormatRequestForIoctl failed with status %!STATUS!",
			status
		);
	}

	WdfRequestSetCompletionRoutine(
		pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest,
		DsBth_HidInterruptReadRequestCompletionRoutine,
		pDeviceContext
	);

	if (WdfRequestSend(
		pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest,
		pDeviceContext->Connection.Bth.BthIoTarget,
		WDF_NO_SEND_OPTIONS
	) == FALSE) {
		status = WdfRequestGetStatus(pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest);
	}
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSHIDMINIDRV,
			"WdfRequestSend failed with status %!STATUS!",
			status
		);
	}

Exit:

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");
}

#pragma endregion

VOID DumpAsHex(PCSTR Prefix, PVOID Buffer, ULONG BufferLength)
{
#ifdef DBG
	PSTR dumpBuffer;
	size_t dumpBufferLength;
	ULONG i;

	dumpBufferLength = ((BufferLength * sizeof(CHAR)) * 2) + 1;
	dumpBuffer = malloc(dumpBufferLength);
	if (dumpBuffer)
	{
		RtlZeroMemory(dumpBuffer, dumpBufferLength);

		for (i = 0; i < BufferLength; i++)
		{
			sprintf_s(&dumpBuffer[i * 2], dumpBufferLength, "%02X", ((PUCHAR)Buffer)[i]);
		}

		TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV,
		            "%s - Buffer length: %04d, buffer content: %s",
		            Prefix,
		            BufferLength,
		            dumpBuffer
		);

		free(dumpBuffer);
	}
#else
	UNREFERENCED_PARAMETER(Prefix);
	UNREFERENCED_PARAMETER(Buffer);
	UNREFERENCED_PARAMETER(BufferLength);
#endif
}
