#include "Driver.h"
#include "DsHidMiniDrv.tmh"


PWSTR G_DsHidMini_Strings[] =
{
	L"String 0" // TODO: bug in DMF module? Seems required for serial string to work
};

#define DSHIDMINI_MANUFACTURER_STRING    L"Nefarius Software Solutions e.U."
#define DSHIDMINI_PRODUCT_STRING         L"DS3 Compatible HID Device"
#define DSHIDMINI_XBOX_PRODUCT_STRING    L"Controller (Xbox One For Windows)"
#define DSHIDMINI_SERIAL_NUMBER_STRING   L"" // TODO: use device address?


// This macro declares the following function:
// DMF_CONTEXT_GET()
//
DMF_MODULE_DECLARE_CONTEXT(DsHidMini)

// This module has no config
//
DMF_MODULE_DECLARE_NO_CONFIG(DsHidMini)


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
	NTSTATUS status;
	DMF_MODULE_DESCRIPTOR dsHidMiniDesc;
	DMF_CALLBACKS_DMF dsHidMiniCallbacks;

	PAGED_CODE();

	FuncEntry(TRACE_DSHIDMINIDRV);

	do
	{
		//
		// Set Virtual HID Mini properties
		// 
		DMF_CALLBACKS_DMF_INIT(&dsHidMiniCallbacks);
		dsHidMiniCallbacks.ChildModulesAdd = DMF_DsHidMini_ChildModulesAdd;
		dsHidMiniCallbacks.DeviceOpen = DMF_DsHidMini_Open;

		DMF_MODULE_DESCRIPTOR_INIT_CONTEXT_TYPE(
			dsHidMiniDesc,
			DsHidMini,
			DMF_CONTEXT_DsHidMini,
			DMF_MODULE_OPTIONS_PASSIVE,
			DMF_MODULE_OPEN_OPTION_OPEN_D0EntrySystemPowerUp
		);

		dsHidMiniDesc.CallbacksDmf = &dsHidMiniCallbacks;

		if (!NT_SUCCESS(status = DMF_ModuleCreate(
			Device,
			DmfModuleAttributes,
			ObjectAttributes,
			&dsHidMiniDesc,
			DmfModule)))
		{
			TraceError(
				TRACE_DSHIDMINIDRV,
				"DMF_ModuleCreate failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_ModuleCreate", status);
			break;
		}
	} while (FALSE);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}
#pragma code_seg()

#pragma region DMF Initialization and clean-up

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
	DMF_CONTEXT_DsHidMini* pModCtx;
	DMF_CONFIG_VirtualHidMini vHidCfg;
#ifdef DSHM_FEATURE_FFB
	DMF_CONFIG_HashTable hashCfg;
#endif
	NTSTATUS status;
	DS_HID_DEVICE_MODE hidDeviceMode = DsHidMiniDeviceModeXInputHIDCompatible;

	PAGED_CODE();

	UNREFERENCED_PARAMETER(DmfParentModuleAttributes);

	FuncEntry(TRACE_DSHIDMINIDRV);

	const WDFDEVICE device = DMF_ParentDeviceGet(DmfModule);
	pModCtx = DMF_CONTEXT_GET(DmfModule);

	ULONG requiredSize = 0;
	DEVPROPTYPE propType = DEVPROP_TYPE_EMPTY;
	WDF_DEVICE_PROPERTY_DATA propertyData;
	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RW_HidDeviceMode);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	//
	// We need the DEVPKEY_DsHidMini_RW_HidDeviceMode value here to make some decisions
	// It should be set to an initial value by the INF anyway
	// 
	if (!NT_SUCCESS(status = WdfDeviceQueryPropertyEx(
		device,
		&propertyData,
		sizeof(BYTE),
		(PBYTE)&hidDeviceMode,
		&requiredSize,
		&propType
	)))
	{
		TraceError(
			TRACE_DSHIDMINIDRV,
			"WdfDeviceQueryPropertyEx failed with status %!STATUS!",
			status
		);

		// continue using defaults
	}

	DMF_CONFIG_VirtualHidMini_AND_ATTRIBUTES_INIT(
		&vHidCfg,
		&moduleAttributes
	);

	vHidCfg.GetInputReport = DsHidMini_GetInputReport;
	vHidCfg.GetFeature = DsHidMini_GetFeature;
	vHidCfg.SetFeature = DsHidMini_SetFeature;
	vHidCfg.SetOutputReport = DsHidMini_SetOutputReport;
	vHidCfg.WriteReport = DsHidMini_WriteReport;

	vHidCfg.StringSizeCbManufacturer = sizeof(DSHIDMINI_MANUFACTURER_STRING);
	vHidCfg.StringManufacturer = DSHIDMINI_MANUFACTURER_STRING;

	//
	// In this mode we wanna come as close to the Xbox One as possible
	// 
	if (hidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible)
	{
		vHidCfg.StringProduct = DSHIDMINI_XBOX_PRODUCT_STRING;
		vHidCfg.StringSizeCbProduct = sizeof(DSHIDMINI_XBOX_PRODUCT_STRING);
	}
	else
	{
		vHidCfg.StringProduct = DSHIDMINI_PRODUCT_STRING;
		vHidCfg.StringSizeCbProduct = sizeof(DSHIDMINI_PRODUCT_STRING);
	}

	vHidCfg.StringSizeCbSerialNumber = sizeof(DSHIDMINI_SERIAL_NUMBER_STRING);
	vHidCfg.StringSerialNumber = DSHIDMINI_SERIAL_NUMBER_STRING;

	vHidCfg.Strings = G_DsHidMini_Strings;
	vHidCfg.NumberOfStrings = 1;

	DMF_DmfModuleAdd(
		DmfModuleInit,
		&moduleAttributes,
		WDF_NO_OBJECT_ATTRIBUTES,
		&pModCtx->DmfModuleVirtualHidMini
	);


#ifdef DSHM_FEATURE_FFB
	DMF_CONFIG_HashTable_AND_ATTRIBUTES_INIT(&hashCfg, &moduleAttributes);

	hashCfg.MaximumKeyLength = sizeof(UCHAR);
	hashCfg.MaximumValueLength = sizeof(FFB_ATTRIBUTES);
	hashCfg.MaximumTableSize = MAX_EFFECT_BLOCKS;

	DMF_DmfModuleAdd(
		DmfModuleInit,
		&moduleAttributes,
		WDF_NO_OBJECT_ATTRIBUTES,
		&pModCtx->DmfModuleForceFeedback
	);
#endif

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}
#pragma code_seg()

//
// Read/refresh configuration here after power-up
// 
#pragma code_seg("PAGE")
_Use_decl_annotations_
NTSTATUS
DMF_DsHidMini_Open(
	_In_ DMFMODULE DmfModule
)
{
	NTSTATUS status = STATUS_SUCCESS;

	UNREFERENCED_PARAMETER(DmfModule);

	PAGED_CODE();

	FuncEntry(TRACE_DSHIDMINIDRV);

	const DMF_CONTEXT_DsHidMini* moduleContext = DMF_CONTEXT_GET(DmfModule);
	const WDFDEVICE device = DMF_ParentDeviceGet(DmfModule);
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(device);
	DMF_CONFIG_VirtualHidMini* pHidCfg = DMF_ModuleConfigGet(moduleContext->DmfModuleVirtualHidMini);

	//
	// Load settings
	// 
	ConfigLoadForDevice(pDevCtx, FALSE);

	pHidCfg->VendorId = pDevCtx->VendorId;
	pHidCfg->ProductId = pDevCtx->ProductId;
	pHidCfg->VersionNumber = pDevCtx->VersionNumber;
	pHidCfg->HidDeviceAttributes.VendorID = pDevCtx->VendorId;
	pHidCfg->HidDeviceAttributes.ProductID = pDevCtx->ProductId;
	pHidCfg->HidDeviceAttributes.VersionNumber = pDevCtx->VersionNumber;

	switch (pDevCtx->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeSDF:

		pHidCfg->HidDescriptor = &G_Ds3HidDescriptor_Single_Mode;
		pHidCfg->HidDescriptorLength = sizeof(G_Ds3HidDescriptor_Single_Mode);
		pHidCfg->HidReportDescriptor = G_Ds3HidReportDescriptor_Single_Mode;
		pHidCfg->HidReportDescriptorLength = G_Ds3HidDescriptor_Single_Mode.DescriptorList[0].wReportLength;

		break;
	case DsHidMiniDeviceModeGPJ:

		pHidCfg->HidDescriptor = &G_Ds3HidDescriptor_Split_Mode;
		pHidCfg->HidDescriptorLength = sizeof(G_Ds3HidDescriptor_Split_Mode);
		pHidCfg->HidReportDescriptor = G_Ds3HidReportDescriptor_Split_Mode;
		pHidCfg->HidReportDescriptorLength = G_Ds3HidDescriptor_Split_Mode.DescriptorList[0].wReportLength;

		break;
	case DsHidMiniDeviceModeSixaxisCompatible:

		pHidCfg->HidDescriptor = &G_SixaxisHidDescriptor;
		pHidCfg->HidDescriptorLength = sizeof(G_SixaxisHidDescriptor);
		pHidCfg->HidReportDescriptor = G_SixaxisHidReportDescriptor;
		pHidCfg->HidReportDescriptorLength = G_SixaxisHidDescriptor.DescriptorList[0].wReportLength;

		break;
	case DsHidMiniDeviceModeDS4WindowsCompatible:

		pHidCfg->HidDescriptor = &G_VendorDefinedUSBDS4HidDescriptor;
		pHidCfg->HidDescriptorLength = sizeof(G_VendorDefinedUSBDS4HidDescriptor);
		pHidCfg->HidReportDescriptor = G_VendorDefinedUSBDS4HidReportDescriptor;
		pHidCfg->HidReportDescriptorLength = G_VendorDefinedUSBDS4HidDescriptor.DescriptorList[0].wReportLength;

	//
	// Required to get properly detected by DS4Windows
	// Keep in sync with here: 
	// https://github.com/Ryochan7/DS4Windows/blob/74cdcb06e95af7681ab734bf94994488818067f2/DS4Windows/DS4Library/DS4Devices.cs#L161
	// 
		pHidCfg->VendorId = pDevCtx->VendorId = DS3_DS4WINDOWS_HID_VID;
		pHidCfg->ProductId = pDevCtx->ProductId = DS3_DS4WINDOWS_HID_PID;
		pHidCfg->VersionNumber = pDevCtx->VersionNumber;
		pHidCfg->HidDeviceAttributes.VendorID = pDevCtx->VendorId;
		pHidCfg->HidDeviceAttributes.ProductID = pDevCtx->ProductId;
		pHidCfg->HidDeviceAttributes.VersionNumber = pDevCtx->VersionNumber;

		break;
	case DsHidMiniDeviceModeXInputHIDCompatible:

		pHidCfg->HidDescriptor = &G_XInputHIDCompatible_HidDescriptor;
		pHidCfg->HidDescriptorLength = sizeof(G_XInputHIDCompatible_HidDescriptor);
		pHidCfg->HidReportDescriptor = G_XInputHIDCompatible_HidReportDescriptor;
		pHidCfg->HidReportDescriptorLength = G_XInputHIDCompatible_HidDescriptor.DescriptorList[0].wReportLength;

	//
	// Required for best XUSB/XInput compatibility
	// 
		pHidCfg->VendorId = pDevCtx->VendorId = DS3_XINPUT_HID_VID;
		pHidCfg->ProductId = pDevCtx->ProductId = DS3_XINPUT_HID_PID;
		pHidCfg->VersionNumber = pDevCtx->VersionNumber;
		pHidCfg->HidDeviceAttributes.VendorID = pDevCtx->VendorId;
		pHidCfg->HidDeviceAttributes.ProductID = pDevCtx->ProductId;
		pHidCfg->HidDeviceAttributes.VersionNumber = pDevCtx->VersionNumber;

		break;
	default:

		status = STATUS_INVALID_PARAMETER;

		TraceError(
			TRACE_DSHIDMINIDRV,
			"Unknown HID Device Mode: 0x%02X",
			pDevCtx->Configuration.HidDeviceMode
		);

		goto exit;
	}

	//
	// If not in disabled pairing mode and if on USB then execute pairing process then request currently set host address
	//
	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb && pDevCtx->Configuration.DevicePairingMode != DsDevicePairingModeDisabled)
	{
		DsUsb_Ds3PairToNewHost(device);
		DsUsb_Ds3RequestHostAddress(device);
	}

	//
	// Set currently used HID mode
	// 

	WDF_DEVICE_PROPERTY_DATA propertyData;
	WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RW_HidDeviceMode);
	propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
	propertyData.Lcid = LOCALE_NEUTRAL;

	status = WdfDeviceAssignProperty(
		device,
		&propertyData,
		DEVPROP_TYPE_BYTE,
		sizeof(BYTE),
		&pDevCtx->Configuration.HidDeviceMode
	);

exit:
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSHIDMINIDRV,
			"HID device configuration failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"HID device configuration", status);
	}

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}
#pragma code_seg()

#pragma endregion

#pragma region DMF Virtual HID Mini-specific

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

	UNREFERENCED_PARAMETER(Request);

	FuncEntry(TRACE_DSHIDMINIDRV);

	const DMFMODULE dmfModuleParent = DMF_ParentModuleGet(DmfModule);
	DMF_CONTEXT_DsHidMini* moduleContext = DMF_CONTEXT_GET(dmfModuleParent);
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));

	*Buffer = moduleContext->InputReport;

	switch (pDevCtx->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeSDF:
	case DsHidMiniDeviceModeGPJ:
		*BufferSize = DS3_SDF_GPJ_HID_INPUT_REPORT_SIZE;
		break;
	case DsHidMiniDeviceModeSixaxisCompatible:
		*BufferSize = SIXAXIS_HID_INPUT_REPORT_SIZE;
		break;
	case DsHidMiniDeviceModeDS4WindowsCompatible:
		*BufferSize = DS3_DS4REV1_USB_HID_INPUT_REPORT_SIZE;
		break;
	case DsHidMiniDeviceModeXInputHIDCompatible:
		*BufferSize = XINPUTHID_HID_INPUT_REPORT_SIZE;
		break;
	default:
		TraceError(
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

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}

//
// Handles GET_FEATURE requests
// 
_Use_decl_annotations_
NTSTATUS
DsHidMini_GetFeature(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

	UNREFERENCED_PARAMETER(Request);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));
	const DMF_CONTEXT_DsHidMini* pModCtx = DMF_CONTEXT_GET(DMF_ParentModuleGet(DmfModule));

	NTSTATUS status = DSHM_GetFeature(pDevCtx, pModCtx, Packet, ReportSize);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}

//
// Handles SET_FEATURE requests
// 
_Use_decl_annotations_
NTSTATUS
DsHidMini_SetFeature(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

	UNREFERENCED_PARAMETER(Request);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));
	const DMF_CONTEXT_DsHidMini* pModCtx = DMF_CONTEXT_GET(DMF_ParentModuleGet(DmfModule));
	
	NTSTATUS status = DSHM_SetFeature(pDevCtx, pModCtx, Packet, ReportSize);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}

_Use_decl_annotations_
NTSTATUS
DsHidMini_WriteReport(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	FuncEntry(TRACE_DSHIDMINIDRV);

	UNREFERENCED_PARAMETER(Request);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));
	const DMF_CONTEXT_DsHidMini* pModCtx = DMF_CONTEXT_GET(DMF_ParentModuleGet(DmfModule));

	NTSTATUS status = DSHM_WriteReport(pDevCtx, pModCtx, Packet, ReportSize);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;	
}

#pragma endregion

#pragma region Input Report processing

//
// Process a raw input report depending on HID emulation mode
// 
void Ds_ProcessHidInputReport(PDEVICE_CONTEXT Context, PDS3_RAW_INPUT_REPORT Report)
{

	FuncEntry(TRACE_DSHIDMINIDRV);

	const DMFMODULE dmfModule = (DMFMODULE)Context->DsHidMiniModule;
	DMF_CONTEXT_DsHidMini* pModCtx = DMF_CONTEXT_GET(dmfModule);

#pragma region HID Input Report (SDF, GPJ ID 01) processing

	switch (Context->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeGPJ:

		DS3_RAW_TO_GPJ_HID_INPUT_REPORT_01(
			Report,
			pModCtx->InputReport,
			Context->Configuration.GPJ.PressureExposureMode,
			Context->Configuration.GPJ.DPadExposureMode,
			&Context->Configuration.ThumbSettings,
			&Context->Configuration.FlipAxis
		);

#ifdef DBG
		DumpAsHex(">> MULTI", pModCtx->InputReport, DS3_SDF_GPJ_HID_INPUT_REPORT_SIZE);
#endif

		break;
	case DsHidMiniDeviceModeSDF:

		DS3_RAW_TO_SDF_HID_INPUT_REPORT(
			Report,
			pModCtx->InputReport,
			Context->Configuration.SDF.PressureExposureMode,
			Context->Configuration.SDF.DPadExposureMode,
			&Context->Configuration.ThumbSettings,
			&Context->Configuration.FlipAxis
		);

	/*
#ifdef DBG
	DumpAsHex(">> SINGLE", moduleContext->InputReport, DS3_SPLIT_SINGLE_HID_INPUT_REPORT_SIZE);
#endif
	*/

		break;
	default:
		break;
	}

	//
	// Notify new Input Report is available
	// 
	NTSTATUS status = DMF_VirtualHidMini_InputReportGenerate(
		pModCtx->DmfModuleVirtualHidMini,
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

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeGPJ
		&& (Context->Configuration.GPJ.PressureExposureMode & DsPressureExposureModeAnalogue) != 0)
	{
		DS3_RAW_TO_GPJ_HID_INPUT_REPORT_02(
			Report,
			pModCtx->InputReport
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			pModCtx->DmfModuleVirtualHidMini,
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

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		DS3_RAW_TO_SIXAXIS_HID_INPUT_REPORT(
			Report,
			pModCtx->InputReport,
			&Context->Configuration.ThumbSettings,
			&Context->Configuration.FlipAxis
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			pModCtx->DmfModuleVirtualHidMini,
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

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeDS4WindowsCompatible)
	{
		DS3_RAW_TO_DS4WINDOWS_HID_INPUT_REPORT(
			Report,
			pModCtx->InputReport,
			(Context->ConnectionType == DsDeviceConnectionTypeUsb) ? TRUE : FALSE,
			&Context->Configuration.ThumbSettings,
			&Context->Configuration.FlipAxis
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			pModCtx->DmfModuleVirtualHidMini,
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

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible)
	{
		DS3_RAW_TO_XINPUTHID_HID_INPUT_REPORT(
			Report,
			(PXINPUT_HID_INPUT_REPORT)pModCtx->InputReport,
			&Context->Configuration.ThumbSettings,
			&Context->Configuration.FlipAxis
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			pModCtx->DmfModuleVirtualHidMini,
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

//
// Called when data is available on the USB Interrupt IN pipe.
//  
VOID DsUsb_EvtUsbInterruptPipeReadComplete(
	WDFUSBPIPE Pipe,
	WDFMEMORY Buffer,
	size_t NumBytesTransferred,
	WDFCONTEXT Context
)
{
	LARGE_INTEGER freq, t2;
	DS_BATTERY_STATUS battery;

	UNREFERENCED_PARAMETER(Pipe);

	FuncEntry(TRACE_DSHIDMINIDRV);

	//
	// Validate expected packet size
	// 
	if (NumBytesTransferred < sizeof(DS3_RAW_INPUT_REPORT))
	{
		TraceEvents(
			TRACE_LEVEL_WARNING,
			TRACE_DSHIDMINIDRV,
			"Received %I64d but expected %I64d",
			NumBytesTransferred,
			sizeof(DS3_RAW_INPUT_REPORT)
		);

		FuncExitNoReturn(TRACE_DSHIDMINIDRV);
		return;
	}

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Context);
	DMF_CONTEXT_DsHidMini* pModCtx = DMF_CONTEXT_GET((DMFMODULE)pDevCtx->DsHidMiniModule);
	const PDS3_RAW_INPUT_REPORT pInReport = (PDS3_RAW_INPUT_REPORT)WdfMemoryGetBuffer(Buffer, NULL);

	QueryPerformanceFrequency(&freq);
	LARGE_INTEGER* t1 = &pDevCtx->Connection.Usb.ChargingCycleTimestamp;

#ifdef DBG
	DumpAsHex(">> USB", pInReport, (ULONG)sizeof(DS3_RAW_INPUT_REPORT));
#endif

	//
	// Handle special case of SIXAXIS.SYS emulation
	// 
	if (pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		RtlCopyMemory(
			&pModCtx->GetFeatureReport,
			pInReport,
			sizeof(DS3_RAW_INPUT_REPORT)
		);

		pModCtx->GetFeatureReport.AccelerometerX = 0x03FF - _byteswap_ushort(pModCtx->GetFeatureReport.AccelerometerX);
		pModCtx->GetFeatureReport.AccelerometerY = _byteswap_ushort(pModCtx->GetFeatureReport.AccelerometerY);
		pModCtx->GetFeatureReport.AccelerometerZ = _byteswap_ushort(pModCtx->GetFeatureReport.AccelerometerZ);
		pModCtx->GetFeatureReport.Gyroscope = _byteswap_ushort(pModCtx->GetFeatureReport.Gyroscope);
	}

	battery = (DS_BATTERY_STATUS)pInReport->BatteryStatus;

	//
	// Update battery status property
	// 
	if (battery != pDevCtx->BatteryStatus)
	{
		WDF_DEVICE_PROPERTY_DATA propertyData;

		WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_BatteryStatus);
		propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
		propertyData.Lcid = LOCALE_NEUTRAL;

		(void)WdfDeviceAssignProperty(
			(WDFDEVICE)Context,
			&propertyData,
			DEVPROP_TYPE_BYTE,
			sizeof(BYTE),
			&battery
		);
	}

	const PDS_LED_SETTINGS pLED = &pDevCtx->Configuration.LEDSettings;

	//
	// Check if state has changed to Charged
	// 
	if (battery == DsBatteryStatusCharged
		&& battery != pDevCtx->BatteryStatus)
	{
		pDevCtx->BatteryStatus = battery;

		if (
			(pLED->Authority == DsLEDAuthorityDriver /* Driver wins over Automatic or Application */ ||
				pDevCtx->OutputReport.Mode == Ds3OutputReportModeDriverHandled) &&
			pLED->Mode > DsLEDModeUnknown && pLED->Mode < DsLEDModeCustomPattern
			)
		{
			switch (pLED->Mode)
			{
			case DsLEDModeBatteryIndicatorPlayerIndex:

				DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_4);

				break;
			case DsLEDModeBatteryIndicatorBarGraph:

				DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_1 | DS3_LED_2 | DS3_LED_3 | DS3_LED_4);

				break;
			}

			(void)DSHM_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverLowPriority);
		}
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

		const LONGLONG ms = (t2.QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

		//
		// 1 second passed
		// 
		if (ms > 1000)
		{
			//
			// Reset
			// 
			pDevCtx->Connection.Usb.ChargingCycleTimestamp.QuadPart = 0;

			UCHAR led = DS3_LED_OFF;

			switch (pLED->Mode)
			{
			case DsLEDModeBatteryIndicatorPlayerIndex:

				led = DS3_GET_LED_FLAGS(pDevCtx) << 1;

			//
			// Cycle through
			// 
				if (led > DS3_LED_4 || led < DS3_LED_1)
				{
					led = DS3_LED_1;
				}

				break;
			case DsLEDModeBatteryIndicatorBarGraph:

				led = DS3_GET_LED_FLAGS(pDevCtx);

			//
			// Cycle graph from 1 to 4 and repeat
			// 
				if (led & 0xF0)
				{
					led = DS3_LED_1;
				}
				else
				{
					led |= (!led) ? DS3_LED_1 : led << 1;
				}

				break;
			}

			if (
				(pLED->Authority == DsLEDAuthorityDriver /* Driver wins over Automatic or Application */ ||
					pDevCtx->OutputReport.Mode == Ds3OutputReportModeDriverHandled) &&
				/* validate mode range */
				pLED->Mode > DsLEDModeUnknown && pLED->Mode < DsLEDModeCustomPattern
				)
			{
				DS3_SET_LED_FLAGS(pDevCtx, led);

				(void)DSHM_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverLowPriority);
			}
		}
	}
	else
	{
		pDevCtx->BatteryStatus = battery;
	}

	Ds_ProcessHidInputReport(pDevCtx, pInReport);

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}


_IRQL_requires_max_(DISPATCH_LEVEL)
_IRQL_requires_same_
ContinuousRequestTarget_BufferDisposition
DsBth_HidInterruptReadContinuousRequestCompleted(
	_In_ DMFMODULE DmfModule,
	_In_reads_(OutputBufferSize) VOID* OutputBuffer,
	_In_ size_t OutputBufferSize,
	_In_ VOID* ClientBufferContextOutput,
	_In_ NTSTATUS CompletionStatus)
{
	NTSTATUS status;
	PUCHAR buffer;
	size_t bufferLength;
	PDEVICE_CONTEXT pDevCtx;
	LARGE_INTEGER freq, * t1, t2;
	LONGLONG ms;
	DS_BATTERY_STATUS battery;
	DMF_CONTEXT_DsHidMini* pModCtx;
	PDS3_RAW_INPUT_REPORT pInReport;
	WDFDEVICE device;

	UNREFERENCED_PARAMETER(ClientBufferContextOutput);

	FuncEntry(TRACE_DSHIDMINIDRV);

#ifdef DBG
	TraceVerbose(
		TRACE_DSHIDMINIDRV,
		"++ Completion status: %!STATUS!",
		CompletionStatus
	);
#endif

	if (!NT_SUCCESS(CompletionStatus))
	{
		return ContinuousRequestTarget_BufferDisposition_ContinuousRequestTargetAndStopStreaming;
	}

	device = DMF_ParentDeviceGet(DmfModule);
	pDevCtx = DeviceGetContext(device);
	pModCtx = DMF_CONTEXT_GET((DMFMODULE)pDevCtx->DsHidMiniModule);
	QueryPerformanceFrequency(&freq);

	buffer = (PUCHAR)OutputBuffer;
	bufferLength = OutputBufferSize;

#ifdef DBG
	TraceInformation(TRACE_DSHIDMINIDRV, "!! buffer: 0x%p, bufferLength: %d",
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
		return ContinuousRequestTarget_BufferDisposition_ContinuousRequestTargetAndContinueStreaming;
	}

	//
	// Skip to report ID
	// 
	pInReport = (PDS3_RAW_INPUT_REPORT)&buffer[1];

	//
	// Handle special case of SIXAXIS.SYS emulation
	// 
	if (pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		RtlCopyMemory(
			&pModCtx->GetFeatureReport,
			pInReport,
			sizeof(DS3_RAW_INPUT_REPORT)
		);

		pModCtx->GetFeatureReport.AccelerometerX = 0x03FF - _byteswap_ushort(pModCtx->GetFeatureReport.AccelerometerX);
		pModCtx->GetFeatureReport.AccelerometerY = _byteswap_ushort(pModCtx->GetFeatureReport.AccelerometerY);
		pModCtx->GetFeatureReport.AccelerometerZ = _byteswap_ushort(pModCtx->GetFeatureReport.AccelerometerZ);
		pModCtx->GetFeatureReport.Gyroscope = _byteswap_ushort(pModCtx->GetFeatureReport.Gyroscope);
	}

	//
	// Grab battery info
	// 
	battery = (DS_BATTERY_STATUS)pInReport->BatteryStatus;

	//
	// React if last known state differs from current state
	// 
	if (pDevCtx->BatteryStatus != battery)
	{
		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"Battery status changed to %d",
			battery
		);

		//
		// Don't update value on every report to avoid jitter
		// 

		t1 = &pDevCtx->BatteryStatusTimestamp;

		if (pDevCtx->BatteryStatusTimestamp.QuadPart == 0)
		{
			QueryPerformanceCounter(t1);
		}

		QueryPerformanceCounter(&t2);

		ms = (t2.QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

		//
		// First ever call or time span has elapsed
		// 
		if (pDevCtx->BatteryStatus == DsBatteryStatusNone || ms > 60000)
		{
			TraceVerbose(
				TRACE_DSHIDMINIDRV,
				"Updating battery status to %d",
				battery
			);

			QueryPerformanceCounter(t1);

			//
			// Update battery status property
			// 

			WDF_DEVICE_PROPERTY_DATA propertyData;

			WDF_DEVICE_PROPERTY_DATA_INIT(&propertyData, &DEVPKEY_DsHidMini_RO_BatteryStatus);
			propertyData.Flags |= PLUGPLAY_PROPERTY_PERSISTENT;
			propertyData.Lcid = LOCALE_NEUTRAL;

			(void)WdfDeviceAssignProperty(
				device,
				&propertyData,
				DEVPROP_TYPE_BYTE,
				sizeof(BYTE),
				&battery
			);

			const PDS_LED_SETTINGS pLED = &pDevCtx->Configuration.LEDSettings;

			//
			// Don't send update if not initialized yet or custom pattern
			// 
			if (DS3_GET_LED_FLAGS(pDevCtx) != 0x00)
			{
				if (
					(pLED->Authority == DsLEDAuthorityDriver /* Driver wins over Automatic or Application */ ||
						pDevCtx->OutputReport.Mode == Ds3OutputReportModeDriverHandled) &&
					/* validate mode range */
					pLED->Mode > DsLEDModeUnknown && pLED->Mode < DsLEDModeCustomPattern
					)
				{
					//
					// Restore defaults to undo any (past) flashing animations
					// 
					DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 0);
					DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 1);
					DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 2);
					DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 3);

					switch (pLED->Mode)
					{
					case DsLEDModeBatteryIndicatorPlayerIndex:

						switch (battery)
						{
						case DsBatteryStatusCharged:
						case DsBatteryStatusFull:
							DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_4);
							break;
						case DsBatteryStatusHigh:
							DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_3);
							break;
						case DsBatteryStatusMedium:
							DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_2);
							break;
						case DsBatteryStatusLow:
						case DsBatteryStatusDying:
							DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_1);
							DS3_SET_LED_DURATION(pDevCtx, 0, 0xFF, 15, 127, 127);
							break;
						default:
							break;
						}

						break;
					case DsLEDModeBatteryIndicatorBarGraph:

						switch (battery)
						{
						case DsBatteryStatusCharged:
						case DsBatteryStatusFull:
							DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_1 | DS3_LED_2 | DS3_LED_3 | DS3_LED_4);
							break;
						case DsBatteryStatusHigh:
							DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_1 | DS3_LED_2 | DS3_LED_3);
							break;
						case DsBatteryStatusMedium:
							DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_1 | DS3_LED_2);
							break;
						case DsBatteryStatusLow:
						case DsBatteryStatusDying:
							DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_1);
							DS3_SET_LED_DURATION(pDevCtx, 0, 0xFF, 15, 127, 127);
							break;
						default:
							break;
						}

						break;
					}

					(void)DSHM_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverLowPriority);
				}
			}

			//
			// Update battery status
			// 
			pDevCtx->BatteryStatus = battery;
		}
	}

	//
	// Quick disconnect combo detected
	// 
	if (pDevCtx->Configuration.WirelessDisconnectButtonCombo.IsEnabled)
	{
		int engagedCount = 0;
		for (int buttonIndex = 0; buttonIndex < (int)_countof(pDevCtx->Configuration.WirelessDisconnectButtonCombo.Buttons); buttonIndex++)
		{
			if ((pInReport->Buttons.lButtons >> pDevCtx->Configuration.WirelessDisconnectButtonCombo.Buttons[buttonIndex]) & 1)
			{
				engagedCount++;
			}
		}

		if (engagedCount == _countof(pDevCtx->Configuration.WirelessDisconnectButtonCombo.Buttons))
		{
			TraceEvents(TRACE_LEVEL_INFORMATION,
				TRACE_DSHIDMINIDRV,
				"!! Quick disconnect combination detected"
			);

			t1 = &pDevCtx->Connection.Bth.QuickDisconnectTimestamp;

			if (pDevCtx->Connection.Bth.QuickDisconnectTimestamp.QuadPart == 0)
			{
				QueryPerformanceCounter(t1);
			}

			QueryPerformanceCounter(&t2);

			ms = (t2.QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

			//
			// 1 second passed
			// 
			if (ms > pDevCtx->Configuration.WirelessDisconnectButtonCombo.HoldTime)
			{
				TraceEvents(TRACE_LEVEL_INFORMATION,
					TRACE_DSHIDMINIDRV,
					"!! Sending disconnect request"
				);

				//
				// Send disconnect request
				// 
				status = DsBth_SendDisconnectRequest(pDevCtx);

				if (!NT_SUCCESS(status))
				{
					TraceError(
						TRACE_DSHIDMINIDRV,
						"Sending disconnect request failed with status %!STATUS!",
						status
					);
					EventWriteFailedWithNTStatus(__FUNCTION__, L"DsBth_SendDisconnectRequest", status);
				}

				//
				// No further processing
				// 
				return ContinuousRequestTarget_BufferDisposition_ContinuousRequestTargetAndStopStreaming;
			}
		}
		else
		{
			pDevCtx->Connection.Bth.QuickDisconnectTimestamp.QuadPart = 0;
		}
	}

	//
	// Alternative rumble toggle combo detected
	// 
	if (pDevCtx->Configuration.RumbleSettings.AlternativeMode.ToggleButtonCombo.IsEnabled)
	{
		int engagedCount = 0;
		for (int buttonIndex = 0; buttonIndex < (int)_countof(pDevCtx->Configuration.RumbleSettings.AlternativeMode.ToggleButtonCombo.Buttons); buttonIndex++)
		{
			if ((pInReport->Buttons.lButtons >> pDevCtx->Configuration.RumbleSettings.AlternativeMode.ToggleButtonCombo.Buttons[buttonIndex]) & 1)
			{
				engagedCount++;
			}
		}

		if (engagedCount == _countof(pDevCtx->Configuration.RumbleSettings.AlternativeMode.ToggleButtonCombo.Buttons))
		{
			TraceEvents(TRACE_LEVEL_INFORMATION,
				TRACE_DSHIDMINIDRV,
				"!! Alternative mode toggle combination detected"
			);
			if (pDevCtx->RumbleControlState.AltMode.IsToggleAllowed)
			{
				t1 = &pDevCtx->RumbleControlState.AltMode.QuickToggleTimestamp;

				if (pDevCtx->RumbleControlState.AltMode.QuickToggleTimestamp.QuadPart == 0)
				{
					QueryPerformanceCounter(t1);
				}

				QueryPerformanceCounter(&t2);

				ms = (t2.QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

				//
				// Combo was held for the user defined time period
				// 
				if (ms > pDevCtx->Configuration.WirelessDisconnectButtonCombo.HoldTime)
				{
					TraceEvents(TRACE_LEVEL_INFORMATION,
						TRACE_DSHIDMINIDRV,
						"!! Toggling alternative rumble mode"
					);
					pDevCtx->RumbleControlState.AltMode.IsEnabled = !pDevCtx->RumbleControlState.AltMode.IsEnabled;

					//
					// Send rumble feedback to indicate change in rumble mode
					//
					DS3_SET_LARGE_RUMBLE_DURATION(pDevCtx, 0x30);
					DS3_SET_SMALL_RUMBLE_DURATION(pDevCtx, 0x20);
					DS3_SET_BOTH_RUMBLE_STRENGTH(pDevCtx, 0x00, 0xFF);
					(void)DSHM_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverLowPriority);

					//
					// Restore default rumble duration
					//
					DS3_SET_LARGE_RUMBLE_DURATION(pDevCtx, 0xFF);
					DS3_SET_SMALL_RUMBLE_DURATION(pDevCtx, 0xFF);


					//
					// Register the time the toggle was triggered
					// Disallow toggling again while combo is being held
					//
					t1->QuadPart = t2.QuadPart;
					pDevCtx->RumbleControlState.AltMode.IsToggleAllowed = FALSE;
				}
			}
			else
			{
				TraceEvents(TRACE_LEVEL_INFORMATION,
					TRACE_DSHIDMINIDRV,
					"!! Alternative rumble mode toggling prevented"
				);
			}
		}
		else
		{
			if (pDevCtx->RumbleControlState.AltMode.IsToggleAllowed)
			{
				pDevCtx->RumbleControlState.AltMode.QuickToggleTimestamp.QuadPart = 0;
			}
			else
			{
				t1 = &pDevCtx->RumbleControlState.AltMode.QuickToggleTimestamp;
				QueryPerformanceCounter(&t2);
				ms = (t2.QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);
				if (ms > 1000) // wait 1 second before re-enabling toggle combo after releasing it
				{
					TraceEvents(TRACE_LEVEL_INFORMATION,
						TRACE_DSHIDMINIDRV,
						"!! Prevention time after releasing alt mode toggle combo has passed. Allowing combo again"
					);
					pDevCtx->RumbleControlState.AltMode.IsToggleAllowed = TRUE;
					pDevCtx->RumbleControlState.AltMode.QuickToggleTimestamp.QuadPart = 0;
				}
			}
		}
	}

	//
	// Idle disconnect detection
	// 
	if (!pDevCtx->Configuration.DisableWirelessIdleTimeout && DS3_RAW_IS_IDLE(pInReport))
	{
		t1 = &pDevCtx->Connection.Bth.IdleDisconnectTimestamp;

		if (pDevCtx->Connection.Bth.IdleDisconnectTimestamp.QuadPart == 0)
		{
			QueryPerformanceCounter(t1);
		}

		QueryPerformanceCounter(&t2);

		ms = (t2.QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

		//
		// Timeout has been reached
		// 
		if (ms > pDevCtx->Configuration.WirelessIdleTimeoutPeriodMs)
		{
			TraceEvents(TRACE_LEVEL_INFORMATION,
				TRACE_DSHIDMINIDRV,
				"!! Idle timeout detected, sending disconnect request"
			);

			//
			// Send disconnect request
			// 
			status = DsBth_SendDisconnectRequest(pDevCtx);

			if (!NT_SUCCESS(status))
			{
				TraceError(
					TRACE_DSHIDMINIDRV,
					"Sending disconnect request failed with status %!STATUS!",
					status
				);
				EventWriteFailedWithNTStatus(__FUNCTION__, L"DsBth_SendDisconnectRequest", status);
			}

			//
			// No further processing
			// 
			return ContinuousRequestTarget_BufferDisposition_ContinuousRequestTargetAndStopStreaming;
		}
	}
	else
	{
		pDevCtx->Connection.Bth.IdleDisconnectTimestamp.QuadPart = 0;
	}

	Ds_ProcessHidInputReport(pDevCtx, pInReport);

	return ContinuousRequestTarget_BufferDisposition_ContinuousRequestTargetAndContinueStreaming;
}

#pragma endregion

#pragma region Output Report processing

_IRQL_requires_max_(DISPATCH_LEVEL)
_IRQL_requires_same_
VOID
DsBth_HidControlWriteContinuousRequestCompleted(
	_In_ DMFMODULE DmfModule,
	_Out_writes_(*InputBufferSize) VOID* InputBuffer,
	_Out_ size_t* InputBufferSize,
	_In_ VOID* ClientBuferContextInput
)
{
	UNREFERENCED_PARAMETER(DmfModule);
	UNREFERENCED_PARAMETER(InputBuffer);
	UNREFERENCED_PARAMETER(InputBufferSize);
	UNREFERENCED_PARAMETER(ClientBuferContextInput);

	FuncEntry(TRACE_DSHIDMINIDRV);

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}

#pragma endregion

#pragma region Diagnostics

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

		TraceVerbose(
			TRACE_DSHIDMINIDRV,
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

#pragma endregion
