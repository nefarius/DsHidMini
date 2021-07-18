#include "Driver.h"
#include "DsHidMiniDrv.tmh"


PWSTR G_DsHidMini_Strings[] =
{
	L"String 0" // TODO: bug in DMF module? Seems required for serial string to work
};

#define DSHIDMINI_DEVICE_STRING          L"DS3 Compatible HID Device"
#define DSHIDMINI_MANUFACTURER_STRING    L"Nefarius Software Solutions e.U."
#define DSHIDMINI_PRODUCT_STRING         L"DS3 Compatible HID Device"
#define DSHIDMINI_SERIAL_NUMBER_STRING   L"TODO: use device address"


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
	NTSTATUS status;
	DMF_MODULE_DESCRIPTOR dsHidMiniDesc;
	DMF_CALLBACKS_DMF dsHidMiniCallbacks;
	PDEVICE_CONTEXT pDevCtx;
		

	PAGED_CODE();

	FuncEntry(TRACE_DSHIDMINIDRV);

	do
	{
		pDevCtx = DeviceGetContext(Device);

		//
		// Set Virtual HID Mini properties
		// 
		DMF_CALLBACKS_DMF_INIT(&dsHidMiniCallbacks);
		dsHidMiniCallbacks.ChildModulesAdd = DMF_DsHidMini_ChildModulesAdd;
		dsHidMiniCallbacks.DeviceOpen = DMF_DsHidMini_Open;
		dsHidMiniCallbacks.DeviceClose = DMF_DsHidMini_Close;

		DMF_MODULE_DESCRIPTOR_INIT_CONTEXT_TYPE(
			dsHidMiniDesc,
			DsHidMini,
			DMF_CONTEXT_DsHidMini,
			DMF_MODULE_OPTIONS_PASSIVE,
			DMF_MODULE_OPEN_OPTION_OPEN_D0EntrySystemPowerUp
		);

		dsHidMiniDesc.CallbacksDmf = &dsHidMiniCallbacks;

		status = DMF_ModuleCreate(
			Device,
			DmfModuleAttributes,
			ObjectAttributes,
			&dsHidMiniDesc,
			DmfModule);
		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_DSHIDMINIDRV,
				"DMF_ModuleCreate failed with status %!STATUS!",
				status
			);
			break;
		}
	}
	while (FALSE);

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
	DMF_CONTEXT_DsHidMini* moduleContext;
	DMF_CONFIG_VirtualHidMini vHidCfg;
	PDEVICE_CONTEXT pDevCtx;

	
	PAGED_CODE();

	UNREFERENCED_PARAMETER(DmfParentModuleAttributes);

	FuncEntry(TRACE_DSHIDMINIDRV);

	moduleContext = DMF_CONTEXT_GET(DmfModule);
	pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));

	DMF_CONFIG_VirtualHidMini_AND_ATTRIBUTES_INIT(&vHidCfg,
		&moduleAttributes);

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
	vHidCfg.NumberOfStrings = 1;

	DMF_DmfModuleAdd(
		DmfModuleInit,
		&moduleAttributes,
		WDF_NO_OBJECT_ATTRIBUTES,
		&moduleContext->DmfModuleVirtualHidMini
	);

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}
#pragma code_seg()

//
// Read/refresh configuration here after power-up
// 
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
	NTSTATUS status = STATUS_SUCCESS;
	DMF_CONTEXT_DsHidMini* moduleContext;
	DMF_CONFIG_VirtualHidMini* pHidCfg;
	PDEVICE_CONTEXT pDevCtx;
	
	UNREFERENCED_PARAMETER(DmfModule);

	PAGED_CODE();

	FuncEntry(TRACE_DSHIDMINIDRV);

	moduleContext = DMF_CONTEXT_GET(DmfModule);
	pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));
	pHidCfg = DMF_ModuleConfigGet(moduleContext->DmfModuleVirtualHidMini);

	//
	// Update settings queried in PrepareHardware
	// 
	
	pHidCfg->VendorId = pDevCtx->VendorId;
	pHidCfg->ProductId = pDevCtx->ProductId;
	pHidCfg->VersionNumber = pDevCtx->VersionNumber;
	pHidCfg->HidDeviceAttributes.VendorID = pDevCtx->VendorId;
	pHidCfg->HidDeviceAttributes.ProductID = pDevCtx->ProductId;
	pHidCfg->HidDeviceAttributes.VersionNumber = pDevCtx->VersionNumber;

	switch (pDevCtx->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeSingle:

		pHidCfg->HidDescriptor = &G_Ds3HidDescriptor_Single_Mode;
		pHidCfg->HidDescriptorLength = sizeof(G_Ds3HidDescriptor_Single_Mode);
		pHidCfg->HidReportDescriptor = G_Ds3HidReportDescriptor_Single_Mode;
		pHidCfg->HidReportDescriptorLength = G_Ds3HidDescriptor_Single_Mode.DescriptorList[0].wReportLength;

		break;
	case DsHidMiniDeviceModeMulti:

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

		break;
	default:
		TraceError(
			TRACE_DSHIDMINIDRV,
			"Unknown HID Device Mode: 0x%02X", pDevCtx->Configuration.HidDeviceMode);
	}
	
	//
	// Increase pad instance count (TODO: unused)
	// 
	numInstances++;

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

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

	FuncEntry(TRACE_DSHIDMINIDRV);

	pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));

	//
	// TODO: free resources if necessary, save configuration
	// 
	
	UNREFERENCED_PARAMETER(pDevCtx);

	//
	// Decrease pad instance count
	// 
	numInstances--;

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}

#pragma code_seg()

#pragma endregion

#pragma region DMF Virtual HID Mini-specific

NTSTATUS
DsHidMini_GetInputReport(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	NTSTATUS status = STATUS_NOT_IMPLEMENTED;
	
	UNREFERENCED_PARAMETER(DmfModule);
	UNREFERENCED_PARAMETER(Request);
	UNREFERENCED_PARAMETER(Packet);
	UNREFERENCED_PARAMETER(ReportSize);

	FuncEntry(TRACE_DSHIDMINIDRV);

	//
	// NOTE: not really used by any modern game
	// 
	
	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
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

	FuncEntry(TRACE_DSHIDMINIDRV);

	dmfModuleParent = DMF_ParentModuleGet(DmfModule);
	moduleContext = DMF_CONTEXT_GET(dmfModuleParent);
	pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));

	*Buffer = moduleContext->InputReport;

	switch (pDevCtx->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeSingle:
	case DsHidMiniDeviceModeMulti:
		*BufferSize = DS3_SPLIT_SINGLE_HID_INPUT_REPORT_SIZE;
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
NTSTATUS
DsHidMini_GetFeature(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	NTSTATUS status = STATUS_NOT_IMPLEMENTED;

	UNREFERENCED_PARAMETER(Request);

	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));
	DMF_CONTEXT_DsHidMini* pModCtx = DMF_CONTEXT_GET(DMF_ParentModuleGet(DmfModule));
	
	FuncEntry(TRACE_DSHIDMINIDRV);

#ifdef DSHM_FEATURE_FFB

	PFFB_ATTRIBUTES pEntry = NULL;
	
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
		for (pEntry = pModCtx->FfbAttributes; pEntry != NULL; pEntry = pEntry->hh.next)
		{
			/** Skip if already in use. */
			if (pEntry->Reported)
				continue;

			pEntry->Reported = TRUE; // mark as claimed/in use
			pBlockLoad->EffectBlockIndex = pEntry->EffectBlockIndex;
			pBlockLoad->BlockLoadStatus = PidBlsSuccess;
			break;
		}

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_BLOCK_LOAD_REPORT_ID (EffectBlockIndex: %d)",
			pBlockLoad->EffectBlockIndex);
		
		*ReportSize = sizeof(PID_BLOCK_LOAD_REPORT) - 1;

		status = STATUS_SUCCESS;
		
		break;
	}
	
#endif

	if (Packet->reportId == 0x00 && pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		//
		// Copy last received raw report to buffer
		// 
		RtlCopyMemory(
			Packet->reportBuffer,
			&pModCtx->GetFeatureReport,
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
	else if (
		Packet->reportId == 0x12 // Requests device MAC address
		&& Packet->reportBufferLen == 64
		&& pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeDS4WindowsCompatible
	)
	{
		RtlCopyMemory(
			&Packet->reportBuffer[1],
			&pDevCtx->DeviceAddress,
			sizeof(BD_ADDR)
		);

		//
		// Fix Endianness
		// 
		if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
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

	UNREFERENCED_PARAMETER(Request);


	FuncEntry(TRACE_DSHIDMINIDRV);

	DMF_CONTEXT_DsHidMini* pModCtx = DMF_CONTEXT_GET(DMF_ParentModuleGet(DmfModule));

	UNREFERENCED_PARAMETER(pModCtx);


#ifdef DSHM_FEATURE_FFB

	PFFB_ATTRIBUTES pEntry = NULL;
	
	PPID_NEW_EFFECT_REPORT pNewEffect;
	
	switch (Packet->reportId)
	{
	case PID_NEW_EFFECT_REPORT_ID:

		pNewEffect = (PPID_NEW_EFFECT_REPORT)Packet->reportBuffer;

		TraceVerbose(TRACE_DSHIDMINIDRV, "!! PID_CREATE_NEW_EFFECT_REPORT");

		//
		// Look for free effect block index and allocate new entry
		// 
		for (UCHAR index = 1; index < MAX_EFFECT_BLOCKS; index++)
		{
			HASH_FIND(hh, pModCtx->FfbAttributes, &index, sizeof(UCHAR), pEntry);

			if (pEntry == NULL)
			{
				pEntry = (PFFB_ATTRIBUTES)malloc(sizeof(FFB_ATTRIBUTES));
				pEntry->EffectBlockIndex = index; // next free index
				pEntry->EffectType = pNewEffect->EffectType; // effect type requested
				pEntry->Reported = FALSE; // index allocated but not claimed yet
				HASH_ADD(hh, pModCtx->FfbAttributes, EffectBlockIndex, sizeof(UCHAR), pEntry);
				break;
			}
		}

		//
		// Whoops, guess we're full!
		// 
		if (pEntry == NULL)
		{
			status = STATUS_INVALID_PARAMETER;
			break;
		}

		switch (pNewEffect->EffectType)
		{
		case PidEtConstantForce:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Constant Force");
			break;
		case PidEtRamp:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Ramp");
			break;
		case PidEtSquare:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Square");
			break;
		case PidEtSine:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Sine");
			break;
		case PidEtTriangle:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Triangle");
			break;
		case PidEtSawtoothUp:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Sawtooth Up");
			break;
		case PidEtSawtoothDown:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Sawtooth Down");
			break;
		case PidEtSpring:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Spring");
			break;
		case PidEtDamper:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Damper");
			break;
		case PidEtInertia:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Inertia");
			break;
		case PidEtFriction:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! ET Friction");
			break;
		}

		*ReportSize = Packet->reportBufferLen;
		
		break;
	default:
		TraceEvents(TRACE_LEVEL_WARNING,
		            TRACE_DSHIDMINIDRV, "%!FUNC! Not implemented");

		TraceVerbose(TRACE_DSHIDMINIDRV, "-- Packet->reportId: %d, Packet->reportBufferLen: %d",
		         Packet->reportId,
		         Packet->reportBufferLen);

		DumpAsHex("-- SET_FEATURE.reportBuffer", Packet->reportBuffer, Packet->reportBufferLen);
		break;
	}

#endif

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

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
	NTSTATUS status = STATUS_NOT_IMPLEMENTED;
	
	UNREFERENCED_PARAMETER(DmfModule);
	UNREFERENCED_PARAMETER(Request);
	UNREFERENCED_PARAMETER(Packet);
	UNREFERENCED_PARAMETER(ReportSize);

	FuncEntry(TRACE_DSHIDMINIDRV);

	//
	// NOTE: not really used by any modern game
	// 

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}

NTSTATUS
DsHidMini_WriteReport(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	NTSTATUS status = STATUS_NOT_IMPLEMENTED;
	
	UNREFERENCED_PARAMETER(Request);
	UNREFERENCED_PARAMETER(ReportSize);

	FuncEntry(TRACE_DSHIDMINIDRV);

	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(DMF_ParentDeviceGet(DmfModule));
	DMF_CONTEXT_DsHidMini* pModCtx = DMF_CONTEXT_GET(DMF_ParentModuleGet(DmfModule));
	PUCHAR buffer = NULL;
	size_t bufferSize = 0;
	
#ifdef DSHM_FEATURE_FFB
	
	PFFB_ATTRIBUTES pEntry = NULL;
	
	PPID_DEVICE_CONTROL_REPORT pDeviceControl;
	PPID_DEVICE_GAIN_REPORT pGain;
	PPID_SET_CONDITION_REPORT pSetCondition;
	PPID_SET_EFFECT_REPORT pSetEffect;
	PPID_SET_PERIODIC_REPORT pSetPeriodic;
	PPID_SET_CONSTANT_FORCE_REPORT pSetConstant;
	PPID_EFFECT_OPERATION_REPORT pEffectOperation;
	PPID_BLOCK_FREE_REPORT pBlockFree;

	UCHAR rumbleValue = 0;
	
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

			HASH_CLEAR(hh, pModCtx->FfbAttributes);
			// Fall through
		case PidDcStopAllEffects:
			TraceVerbose(TRACE_DSHIDMINIDRV, "!! DC Stop All Effects");
			DS3_SET_SMALL_RUMBLE_STRENGTH(pDevCtx, 0);
			DS3_SET_LARGE_RUMBLE_STRENGTH(pDevCtx, 0);

			(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceForceFeedback);
			
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
		         "EffectType: %d, Duration: %d, TriggerRepeatInterval: %d, "
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
			DS3_SET_SMALL_RUMBLE_STRENGTH(pDevCtx, rumbleValue);
		}
		else
		{
			DS3_SET_LARGE_RUMBLE_STRENGTH(pDevCtx, rumbleValue);
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

			(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceForceFeedback);
			
			break;

		case PidEoStop:

			DS3_SET_SMALL_RUMBLE_STRENGTH(pDevCtx, 0);
			DS3_SET_LARGE_RUMBLE_STRENGTH(pDevCtx, 0);
			
			(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceForceFeedback);
			
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
		// Lookup our entry in the list
		// 
		HASH_FIND(hh, pModCtx->FfbAttributes, &pBlockFree->EffectBlockIndex, sizeof(UCHAR), pEntry);

		//
		// Remove from list
		// 
		HASH_DEL(pModCtx->FfbAttributes, pEntry);

		//
		// Free memory
		// 
		free(pEntry);
		
		*ReportSize = Packet->reportBufferLen;

		status = STATUS_SUCCESS;
		
		break;
	}
	
#endif

	//
	// Handle other non-FFB cases
	// 

	//
	// SIXAXIS.SYS emulation
	// 
	if (Packet->reportId == 0x00 && pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		//
		// External output report overrides internal behaviour, keep note
		// 
		pDevCtx->OutputReport.Mode = Ds3OutputReportModeWriteReportPassThrough;

		//
		// Get raw buffer pointer (connection agnostic)
		// 
		DS3_GET_UNIFIED_OUTPUT_REPORT_BUFFER(
			pDevCtx,
			&buffer,
			&bufferSize
		);

		//
		// Overwrite with what we received
		// 
		RtlCopyMemory(
			buffer,
			&Packet->reportBuffer[3],
			bufferSize
		);

		(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourcePassThrough);

		status = STATUS_SUCCESS;
	}

	//
	// DS4Windows emulation
	// 
	if (Packet->reportId == 0x05 && pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeDS4WindowsCompatible)
	{
		//
		// External output report overrides internal behaviour, keep note
		// 
		pDevCtx->OutputReport.Mode = Ds3OutputReportModeWriteReportPassThrough;

		BOOL isSetRumble = (Packet->reportBuffer[1] >> 0) & 1U;
		BOOL isSetColor = (Packet->reportBuffer[1] >> 1) & 1U;
		BOOL isFlagFlash = (Packet->reportBuffer[1] >> 2) & 1U;

		//
		// Color values (RGB)
		// 
		UCHAR r = Packet->reportBuffer[6];
		UCHAR g = Packet->reportBuffer[7];
		UCHAR b = Packet->reportBuffer[8];

		//
		// Flash Bright and Dark duration
		// 
		UCHAR fb_dur = Packet->reportBuffer[9];
		UCHAR fd_dur = Packet->reportBuffer[10];

		BOOL isFlashOrPulse = isFlagFlash && (fb_dur != 0 || fd_dur != 0);
		BOOL isSetFlashing = FALSE;

		if (isFlashOrPulse) // High Latency DS4Windows function
		{
			if (r == 0x32 && g == 0x00 && b == 0x00)
			{
				// Hard-coded colors used in High Latency warning
				isSetFlashing = TRUE;
			}
		}

		if (isSetRumble)
		{
			DS3_SET_SMALL_RUMBLE_STRENGTH(pDevCtx, Packet->reportBuffer[4]);
			DS3_SET_LARGE_RUMBLE_STRENGTH(pDevCtx, Packet->reportBuffer[5]);
		}

		if (isSetColor)
		{
			//
			// Restore defaults to undo any (past) flashing animations
			// 
			DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 0);
			DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 1);
			DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 2);
			DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 3);

			//
			// Single color RED intensity indicates battery level (Light only a single LED from 1 to 4)
			// 
			if (g == 0x00 && b == 0x00)
			{
				if (r >= 202)
					DS3_SET_LED(pDevCtx, DS3_LED_4);
				else if (r > 148)
					DS3_SET_LED(pDevCtx, DS3_LED_3);
				else if (r > 94)
					DS3_SET_LED(pDevCtx, DS3_LED_2);
				else if (r > 64)
					DS3_SET_LED(pDevCtx, DS3_LED_1);
				else {
					DS3_SET_LED(pDevCtx, DS3_LED_1);
					DS3_SET_LED_DURATION(pDevCtx, 0, 0xFF, 15, 127, 127);
				}
			}
			//
			// Single color RED intensity indicates battery level ("Fill" LEDs from 1 to 4)
			// 
			else if (g == 0x00 && b == 0xFF)
			{
				if (r >= 202)
					DS3_SET_LED(pDevCtx, DS3_LED_1 | DS3_LED_2 | DS3_LED_3 | DS3_LED_4);
				else if (r > 148)
					DS3_SET_LED(pDevCtx, DS3_LED_1 | DS3_LED_2 | DS3_LED_3);
				else if (r > 94)
					DS3_SET_LED(pDevCtx, DS3_LED_1 | DS3_LED_2);
				else if (r > 64)
					DS3_SET_LED(pDevCtx, DS3_LED_1);
				else {
					DS3_SET_LED(pDevCtx, DS3_LED_1);
					DS3_SET_LED_DURATION(pDevCtx, 0, 0xFF, 15, 127, 127);
				}
			}
			//
			// Decode custom LED status from color RED intensity
			// 
			else if (g == 0xFF && b == 0xFF)
			{
				if (r == 0x00)
					DS3_SET_LED(pDevCtx, DS3_LED_OFF);
				else if (r >= 0x01 && r <= 0x0F)
					DS3_SET_LED(pDevCtx, r << 1);
			}
		}

		if (isSetFlashing)
		{
			//
			// Set to rapidly flash all 4 LEDs
			// 
			DS3_SET_LED(pDevCtx, DS3_LED_1 | DS3_LED_2 | DS3_LED_3 | DS3_LED_4);
			DS3_SET_LED_DURATION(pDevCtx, 0, 0xFF, 3, 127, 127);
			DS3_SET_LED_DURATION(pDevCtx, 1, 0xFF, 3, 127, 127);
			DS3_SET_LED_DURATION(pDevCtx, 2, 0xFF, 3, 127, 127);
			DS3_SET_LED_DURATION(pDevCtx, 3, 0xFF, 3, 127, 127);
		}

		(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDualShock4);

		status = STATUS_SUCCESS;
	}

	//
	// Rumble request from XINPUTHID.SYS
	// 
	if (Packet->reportId == 0x00 && pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible)
	{
		DS3_SET_SMALL_RUMBLE_STRENGTH(pDevCtx, Packet->reportBuffer[3]);
		DS3_SET_LARGE_RUMBLE_STRENGTH(pDevCtx, Packet->reportBuffer[4]);

		(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDualShock4);

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

#pragma endregion

#pragma region Input Report processing

/**
 * Process a raw input report depending on HID emulation mode.
 *
 * @author	Benjamin "Nefarius" Höglinger-Stelzer
 * @date	25.02.2021
 *
 * @param 	Context	The context.
 * @param 	Report 	The report.
 */
void Ds_ProcessHidInputReport(PDEVICE_CONTEXT Context, PDS3_RAW_INPUT_REPORT Report)
{
	NTSTATUS status;
	DMFMODULE dmfModule;
	DMF_CONTEXT_DsHidMini* pModCtx;

	FuncEntry(TRACE_DSHIDMINIDRV);

	dmfModule = (DMFMODULE)Context->DsHidMiniModule;
	pModCtx = DMF_CONTEXT_GET(dmfModule);

#pragma region HID Input Report (ID 01) processing

	switch (Context->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeMulti:

		DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_01(
			Report,
			pModCtx->InputReport,
			Context->Configuration.MuteDigitalPressureButtons
		);

#ifdef DBG
		DumpAsHex(">> MULTI", pModCtx->InputReport, DS3_SPLIT_SINGLE_HID_INPUT_REPORT_SIZE);
#endif

		break;
	case DsHidMiniDeviceModeSingle:

		DS3_RAW_TO_SINGLE_HID_INPUT_REPORT(
			Report,
			pModCtx->InputReport,
			Context->Configuration.MuteDigitalPressureButtons
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
	status = DMF_VirtualHidMini_InputReportGenerate(
		pModCtx->DmfModuleVirtualHidMini,
		DsHidMini_RetrieveNextInputReport
	);
	if (!NT_SUCCESS(status) && status != STATUS_NO_MORE_ENTRIES)
	{
		TraceError( TRACE_DSHIDMINIDRV,
			"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
	}

#pragma endregion

#pragma region HID Input Report (ID 02) processing

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeMulti)
	{
		DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_02(
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
			TraceError( TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
		}
	}

#pragma endregion

#pragma region HID Input Report (SIXAXIS compatible) processing

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		DS3_RAW_TO_SIXAXIS_HID_INPUT_REPORT(
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
			TraceError( TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
		}
	}

#pragma endregion

#pragma region HID Input Report (DualShock 4 Rev1 compatible) processing

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeDS4WindowsCompatible)
	{
		DS3_RAW_TO_DS4REV1_HID_INPUT_REPORT(
			Report,
			pModCtx->InputReport,
			(Context->ConnectionType == DsDeviceConnectionTypeUsb) ? TRUE : FALSE
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
			TraceError(TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
		}
	}

#pragma endregion

#pragma region HID Input Report (XINPUT compatible HID device) processing

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible)
	{
		DS3_RAW_TO_XINPUTHID_HID_INPUT_REPORT(
			Report,
			(PXINPUT_HID_INPUT_REPORT)pModCtx->InputReport
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
			TraceError(TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
		}
	}

#pragma endregion
	
	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
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
	PDEVICE_CONTEXT pDevCtx;
	LARGE_INTEGER freq, *t1, t2;
	LONGLONG ms;
	DS_BATTERY_STATUS battery;
	DMF_CONTEXT_DsHidMini* pModCtx;
	PDS3_RAW_INPUT_REPORT pInReport;

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

	pDevCtx = DeviceGetContext(Context);
	pModCtx = DMF_CONTEXT_GET((DMFMODULE)pDevCtx->DsHidMiniModule);
	pInReport = (PDS3_RAW_INPUT_REPORT)WdfMemoryGetBuffer(Buffer, NULL);

	QueryPerformanceFrequency(&freq);
	t1 = &pDevCtx->Connection.Usb.ChargingCycleTimestamp;
	
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

	//
	// Check if state has changed to Charged
	// 
	if (battery == DsBatteryStatusCharged
		&& battery != pDevCtx->BatteryStatus)
	{
		pDevCtx->BatteryStatus = battery;

		if (pDevCtx->OutputReport.Mode == Ds3OutputReportModeDriverHandled)
		{
			DS3_SET_LED(pDevCtx, DS3_LED_4);

			(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverLowPriority);
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
			
			UCHAR led = DS3_GET_LED(pDevCtx) << 1;

			//
			// Cycle through
			// 
			if (led > DS3_LED_4 || led < DS3_LED_1)
			{
				led = DS3_LED_1;
			}

			if (pDevCtx->OutputReport.Mode == Ds3OutputReportModeDriverHandled)
			{
				DS3_SET_LED(pDevCtx, led);

				(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverLowPriority);
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

			//
			// Don't send update if not initialized yet
			// 
			if (DS3_GET_LED(pDevCtx) != 0x00)
			{
				if (pDevCtx->OutputReport.Mode == Ds3OutputReportModeDriverHandled)
				{
					//
					// Restore defaults to undo any (past) flashing animations
					// 
					DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 0);
					DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 1);
					DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 2);
					DS3_SET_LED_DURATION_DEFAULT(pDevCtx, 3);

					switch (battery)
					{
					case DsBatteryStatusCharged:
					case DsBatteryStatusFull:
						DS3_SET_LED(pDevCtx, DS3_LED_4);
						break;
					case DsBatteryStatusHigh:
						DS3_SET_LED(pDevCtx, DS3_LED_3);
						break;
					case DsBatteryStatusMedium:
						DS3_SET_LED(pDevCtx, DS3_LED_2);
						break;
					case DsBatteryStatusLow:
					case DsBatteryStatusDying:
						DS3_SET_LED(pDevCtx, DS3_LED_1);
						DS3_SET_LED_DURATION(pDevCtx, 0, 0xFF, 15, 127, 127);
						break;
					default:
						break;
					}

					(void)Ds_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverLowPriority);
				}
			}

			//
			// Update battery status
			// 
			pDevCtx->BatteryStatus = battery;
		}
	}

	//
	// Quick disconnect combo (L1 + R1 + PS) detected
	// 
	if (
		pInReport->Buttons.Individual.L1
		&& pInReport->Buttons.Individual.R1
		&& pInReport->Buttons.Individual.PS
	)
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
		if (ms > 1000)
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

	//
	// Idle disconnect detection
	// 
	if (DS3_RAW_IS_IDLE(pInReport))
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

//
// Callback invoked when new output report packet is due to being processed
// 
_IRQL_requires_max_(PASSIVE_LEVEL)
_IRQL_requires_same_
ThreadedBufferQueue_BufferDisposition
DMF_EvtExecuteOutputPacketReceived(
	_In_ DMFMODULE DmfModule,
	_In_ UCHAR* ClientWorkBuffer,
	_In_ ULONG ClientWorkBufferSize,
	_In_ VOID* ClientWorkBufferContext,
	_Out_ NTSTATUS* NtStatus
)
{
	ThreadedBufferQueue_BufferDisposition retval = ThreadedBufferQueue_BufferDisposition_WorkComplete;
	NTSTATUS status = STATUS_UNSUCCESSFUL;
	WDFDEVICE device = DMF_ParentDeviceGet(DmfModule);
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(device);
	PDS_OUTPUT_REPORT_CONTEXT pRepCtx = (PDS_OUTPUT_REPORT_CONTEXT)ClientWorkBufferContext;
	size_t bufferSize = pRepCtx->BufferSize;

	WDF_MEMORY_DESCRIPTOR memoryDesc;
	LARGE_INTEGER freq, *t1, *t2;
	LONGLONG ms;
	ULONGLONG timeout;
	size_t bytesWritten;
	
	UNREFERENCED_PARAMETER(ClientWorkBufferSize);
	UNREFERENCED_PARAMETER(NtStatus);

	FuncEntry(TRACE_DSHIDMINIDRV);

	QueryPerformanceFrequency(&freq);

	//
	// Last successful send timestamp
	// 
	t1 = &pDevCtx->OutputReport.Cache.LastSentTimestamp;

	//
	// Current request received timestamp
	// 
	t2 = &pRepCtx->ReceivedTimestamp;

	WDF_MEMORY_DESCRIPTOR_INIT_BUFFER(
		&memoryDesc,
		ClientWorkBuffer,
		(ULONG)bufferSize
	);

	switch (pDevCtx->ConnectionType)
	{
#pragma region DsDeviceConnectionTypeUsb
		
	case DsDeviceConnectionTypeUsb:

		//
		// Don't send if no state change has occurred
		// 
		if (pRepCtx->ReportSource > Ds3OutputReportSourceDriverHighPriority
			&& pDevCtx->Configuration.IsOutputDeduplicatorEnabled > 0
			&& RtlCompareMemory(
				ClientWorkBuffer,
				pDevCtx->OutputReport.Cache.LastReport,
				bufferSize
			) == bufferSize)
		{
			break;
		}

		status = USB_WriteInterruptOutSync(
			pDevCtx,
			&memoryDesc
		);

		if (NT_SUCCESS(status))
		{
			RtlCopyMemory(
				pDevCtx->OutputReport.Cache.LastReport,
				ClientWorkBuffer,
				bufferSize
			);
		}

		break;

#pragma endregion

#pragma region DsDeviceConnectionTypeBth
		
	case DsDeviceConnectionTypeBth:

		//
		// Don't send if no state change has occurred
		// 
		if (pRepCtx->ReportSource > Ds3OutputReportSourceDriverHighPriority
			&& pDevCtx->Configuration.IsOutputDeduplicatorEnabled > 0
			&& RtlCompareMemory(
				ClientWorkBuffer,
				pDevCtx->OutputReport.Cache.LastReport,
				bufferSize
			) == bufferSize)
		{
			break;
		}

		//
		// Calculate delay, the smaller the more frequent packets are sent
		// 
		ms = (t2->QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"Time span since last packet was sent: %I64d ms",
			ms
		);

		//
		// Rate limit condition has been detected
		// 
		if (pRepCtx->ReportSource > Ds3OutputReportSourceDriverHighPriority
			&& pDevCtx->Configuration.IsOutputRateControlEnabled > 0
			&& ms < pDevCtx->Configuration.OutputRateControlPeriodMs)
		{
			timeout = pDevCtx->Configuration.OutputRateControlPeriodMs - ms;
			
			TraceVerbose(
				TRACE_DSHIDMINIDRV,
				"Rate control triggered, delaying buffer 0x%p for %I64u ms",
				ClientWorkBuffer,
				timeout
			);

			//
			// Protect, must not run in parallel
			// 
			WdfWaitLockAcquire(pDevCtx->OutputReport.Cache.Lock, NULL);
			{
				//
				// We're still in overload condition, drop previous, if any
				// 
				if (pDevCtx->OutputReport.Cache.PendingClientBuffer)
				{
					TraceVerbose(
						TRACE_DSHIDMINIDRV,
						"Rate control still engaged, replacing buffer 0x%p with 0x%p",
						pDevCtx->OutputReport.Cache.PendingClientBuffer,
						ClientWorkBuffer
					);
					
					DMF_ThreadedBufferQueue_WorkCompleted(
						pDevCtx->OutputReport.Worker,
						pDevCtx->OutputReport.Cache.PendingClientBuffer,
						STATUS_INVALID_DEVICE_REQUEST // Has no impact
					);
				}

				//
				// Overwrite after old one has been cancelled
				// 
				pDevCtx->OutputReport.Cache.PendingClientBuffer = ClientWorkBuffer;
				pDevCtx->OutputReport.Cache.PendingClientBufferContext = ClientWorkBufferContext;

				//
				// Kick off delay send timer callback
				// 
				if (!pDevCtx->OutputReport.Cache.IsScheduled)
				{
					pDevCtx->OutputReport.Cache.IsScheduled = WdfTimerStart(
						pDevCtx->OutputReport.Cache.SendDelayTimer,
						WDF_REL_TIMEOUT_IN_MS(timeout)
					);
				}
			}
			WdfWaitLockRelease(pDevCtx->OutputReport.Cache.Lock);

			status = STATUS_PENDING; // Has no impact, just for trace
			
			//
			// Keep buffer slot occupied
			// 
			retval = ThreadedBufferQueue_BufferDisposition_WorkPending;
			
			break;
		}

		status = DMF_DefaultTarget_SendSynchronously(
			pDevCtx->Connection.Bth.HidControl.OutputWriterModule,
			ClientWorkBuffer,
			bufferSize,
			NULL,
			0,
			ContinuousRequestTarget_RequestType_Ioctl,
			IOCTL_BTHPS3_HID_CONTROL_WRITE,
			0,
			&bytesWritten
		);

		if (NT_SUCCESS(status))
		{
			// 
			// Store last successful send
			// 

			QueryPerformanceCounter(t1);

			RtlCopyMemory(
				pDevCtx->OutputReport.Cache.LastReport,
				ClientWorkBuffer,
				bufferSize
			);
		}

		break;

#pragma endregion

	default:
		status = STATUS_INVALID_PARAMETER;
	}

	*NtStatus = status;
	
	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);
	
	return retval;
}

//
// Callback invoked after delay timer elapsed
// 
void
DSHM_OutputReportDelayTimerElapsed(
	WDFTIMER Timer
)
{
	NTSTATUS status;
	WDFDEVICE device = WdfTimerGetParentObject(Timer);
	PDEVICE_CONTEXT pDevCtx = DeviceGetContext(device);
	PUCHAR sourceBuffer = pDevCtx->OutputReport.Cache.PendingClientBuffer;
	PDS_OUTPUT_REPORT_CONTEXT pRepCtx = pDevCtx->OutputReport.Cache.PendingClientBufferContext;
	PUCHAR targetBuffer;
	PDS_OUTPUT_REPORT_CONTEXT targetBufferContext;

	FuncEntry(TRACE_DSHIDMINIDRV);

	//
	// Protected region
	// 
	WdfWaitLockAcquire(pDevCtx->OutputReport.Cache.Lock, NULL);
	{
		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"Processing delayed buffer 0x%p",
			sourceBuffer
		);

		//
		// Re-queue last cached buffer with high priority
		// 
		
		status = DMF_ThreadedBufferQueue_Fetch(
			pDevCtx->OutputReport.Worker,
			(PVOID*)&targetBuffer,
			(PVOID*)&targetBufferContext
		);

		if (NT_SUCCESS(status))
		{
			RtlCopyMemory(targetBuffer, sourceBuffer, pRepCtx->BufferSize);

			targetBufferContext->BufferSize = pRepCtx->BufferSize;
			targetBufferContext->ReceivedTimestamp = pRepCtx->ReceivedTimestamp;

			//
			// Set to high priority, bypasses rate control for this buffer
			// 
			targetBufferContext->ReportSource = Ds3OutputReportSourceDriverHighPriority;

			DMF_ThreadedBufferQueue_Enqueue(
				pDevCtx->OutputReport.Worker,
				targetBuffer
			);
		}

		//
		// Mark original buffer as completed
		// 
		DMF_ThreadedBufferQueue_WorkCompleted(
			pDevCtx->OutputReport.Worker,
			pDevCtx->OutputReport.Cache.PendingClientBuffer,
			status
		);

		pDevCtx->OutputReport.Cache.PendingClientBuffer = NULL;

		pDevCtx->OutputReport.Cache.IsScheduled = FALSE;
	}
	WdfWaitLockRelease(pDevCtx->OutputReport.Cache.Lock);

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}

//
// Enqueues current output report buffer to get sent to device.
//
NTSTATUS 
Ds_SendOutputReport(
	PDEVICE_CONTEXT Context, 
	DS_OUTPUT_REPORT_SOURCE Source
)
{
	NTSTATUS status;
	PUCHAR sourceBuffer, sendBuffer;
	size_t sourceBufferLength;
	PDS_OUTPUT_REPORT_CONTEXT sendContext;

	FuncEntry(TRACE_DSHIDMINIDRV);
	
	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	do {
		//
		// Grab new buffer to send
		//
		status = DMF_ThreadedBufferQueue_Fetch(
			Context->OutputReport.Worker,
			(PVOID*)&sendBuffer,
			(PVOID*)&sendContext
		);

		if (!NT_SUCCESS(status)) {
			TraceError(
				TRACE_DSHIDMINIDRV,
				"DMF_ThreadedBufferQueue_Fetch failed with status %!STATUS!",
				status
			);

			//
			// TODO: react to different status codes
			//

			break;
		}

		//
		// Get full report (including IDs etc.)
		//
		DS3_GET_RAW_OUTPUT_REPORT_BUFFER(
			Context,
			&sourceBuffer,
			&sourceBufferLength
		);

		// 
		// Timestamp arrival
		//
		QueryPerformanceCounter(&sendContext->ReceivedTimestamp);
		// 
		// Real buffer length
		// 
		sendContext->BufferSize = sourceBufferLength;
		//
		// Store origin
		//
		sendContext->ReportSource = Source;

		//
		// Copy current report to buffer
		//
		RtlCopyMemory(sendBuffer, sourceBuffer, sourceBufferLength);
		
		//
		// Enqueue current report
		//
		DMF_ThreadedBufferQueue_Enqueue(
			Context->OutputReport.Worker,
			sendBuffer
		);

	} while (FALSE);

	WdfWaitLockRelease(Context->OutputReport.Lock);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
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
