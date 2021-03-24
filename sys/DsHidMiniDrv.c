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
		// Set defaults
		// 
		DS_DRIVER_CONFIGURATION_INIT_DEFAULTS(&pDevCtx->Configuration);

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
	case DsHidMiniDeviceModeDualShock4Rev1Compatible:

		pHidCfg->HidDescriptor = &G_DualShock4Rev1HidDescriptor;
		pHidCfg->HidDescriptorLength = sizeof(G_DualShock4Rev1HidDescriptor);
		pHidCfg->HidReportDescriptor = G_DualShock4Rev1HidReportDescriptor;
		pHidCfg->HidReportDescriptorLength = G_DualShock4Rev1HidDescriptor.DescriptorList[0].wReportLength;

		pHidCfg->VendorId = pDevCtx->VendorId = DS3_DS4REV1_HID_VID;
		pHidCfg->ProductId = pDevCtx->ProductId = DS3_DS4REV1_HID_PID;
		pHidCfg->VersionNumber = pDevCtx->VersionNumber;
		pHidCfg->HidDeviceAttributes.VendorID = pDevCtx->VendorId;
		pHidCfg->HidDeviceAttributes.ProductID = pDevCtx->ProductId;
		pHidCfg->HidDeviceAttributes.VersionNumber = pDevCtx->VersionNumber;
		
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
	case DsHidMiniDeviceModeDualShock4Rev1Compatible:
		*BufferSize = DS3_DS4REV1_HID_INPUT_REPORT_SIZE;
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
			pModCtx->GetFeatureReport,
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
		Packet->reportId == 0x12
		&& Packet->reportBufferLen == 64
		&& pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeDualShock4Rev1Compatible
		)
	{
		RtlCopyMemory(
			&Packet->reportBuffer[1],
			&pDevCtx->DeviceAddress,
			sizeof(BD_ADDR)
		);

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

			(void)Ds_SendOutputReport(pDevCtx);
			
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

			(void)Ds_SendOutputReport(pDevCtx);
			
			break;

		case PidEoStop:

			DS3_SET_SMALL_RUMBLE_STRENGTH(pDevCtx, 0);
			DS3_SET_LARGE_RUMBLE_STRENGTH(pDevCtx, 0);
			
			(void)Ds_SendOutputReport(pDevCtx);
			
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

		(void)Ds_SendOutputReport(pDevCtx);

		status = STATUS_SUCCESS;
	}

	//
	// DS4 Rev1 emulation
	// 
	if (Packet->reportId == 0x05 && pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeDualShock4Rev1Compatible)
	{
		//
		// External output report overrides internal behaviour, keep note
		// 
		pDevCtx->OutputReport.Mode = Ds3OutputReportModeWriteReportPassThrough;

		DS3_SET_SMALL_RUMBLE_STRENGTH(pDevCtx, Packet->reportBuffer[4]);
		DS3_SET_LARGE_RUMBLE_STRENGTH(pDevCtx, Packet->reportBuffer[5]);

		// Color values (RGB)
		// 
		UCHAR r = Packet->reportBuffer[6];
		UCHAR g = Packet->reportBuffer[7];
		UCHAR b = Packet->reportBuffer[8];

		//
		// Single color RED intensity indicates battery level (Light only a single Led from 1 to 4)
		// 
		if (g == 0x00 && b == 0x00)
		{
			if (r >= 192)
				DS3_SET_LED(pDevCtx, DS3_LED_4);
			else if (r >= 129) // should be 128 but needs to be 129 for the "color by battery %"  function to properly work in 50%
				DS3_SET_LED(pDevCtx, DS3_LED_3);
			else if (r >= 65) // should be 64 but needs to be 129 for the "color by battery %" function to properly work in 25%
				DS3_SET_LED(pDevCtx, DS3_LED_2);
			else
				DS3_SET_LED(pDevCtx, DS3_LED_1);
		}
		//
		// Single color RED intensity indicates battery level ("Fill" Leds from 1 to 4)
		// 
		else if (g == 0x00 && b == 0xFF)
		{
			if (r >= 196)
				DS3_SET_LED(pDevCtx, 0x1E);
			else if (r >= 129) // should be 128 but needs to be 129 for the "color by battery %"  function to properly work in 50%
				DS3_SET_LED(pDevCtx, 0x0E);
			else if (r >= 65) // should be 64 but needs to be 129 for the "color by battery %" function to properly work in 25%
				DS3_SET_LED(pDevCtx, 0x06);
			else
				DS3_SET_LED(pDevCtx, 0x02);
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
		
		(void)Ds_SendOutputReport(pDevCtx);

		status = STATUS_SUCCESS;
	}

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
 * @param 	Context			The context.
 * @param 	Buffer			The buffer.
 * @param 	BufferLength	Length of the buffer.
 */
void Ds_ProcessHidInputReport(PDEVICE_CONTEXT Context, PUCHAR Buffer, size_t BufferLength)
{
	NTSTATUS status;
	DMFMODULE dmfModule;
	DMF_CONTEXT_DsHidMini* pModCtx;

	UNREFERENCED_PARAMETER(BufferLength);

	FuncEntry(TRACE_DSHIDMINIDRV);

	dmfModule = (DMFMODULE)Context->DsHidMiniModule;
	pModCtx = DMF_CONTEXT_GET(dmfModule);

#pragma region HID Input Report (ID 01) processing

	switch (Context->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeMulti:

		DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_01(
			Buffer,
			pModCtx->InputReport,
			Context->Configuration.MuteDigitalPressureButtons
		);

#ifdef DBG
		DumpAsHex(">> MULTI", pModCtx->InputReport, DS3_SPLIT_SINGLE_HID_INPUT_REPORT_SIZE);
#endif

		break;
	case DsHidMiniDeviceModeSingle:

		DS3_RAW_TO_SINGLE_HID_INPUT_REPORT(
			Buffer,
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
			Buffer,
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
			Buffer,
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

	if (Context->Configuration.HidDeviceMode == DsHidMiniDeviceModeDualShock4Rev1Compatible)
	{
		DS3_RAW_TO_DS4REV1_HID_INPUT_REPORT(
			Buffer,
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
	size_t rdrBufferLength;
	LPVOID rdrBuffer;
	LARGE_INTEGER freq, *t1, t2;
	LONGLONG ms;
	DS_BATTERY_STATUS battery;
	DMF_CONTEXT_DsHidMini* pModCtx;
	PDS3_RAW_INPUT_REPORT pInReport;

	UNREFERENCED_PARAMETER(Pipe);
	UNREFERENCED_PARAMETER(NumBytesTransferred);

	FuncEntry(TRACE_DSHIDMINIDRV);

	pDevCtx = DeviceGetContext(Context);
	pModCtx = DMF_CONTEXT_GET((DMFMODULE)pDevCtx->DsHidMiniModule);
	rdrBuffer = WdfMemoryGetBuffer(Buffer, &rdrBufferLength);

	QueryPerformanceFrequency(&freq);
	t1 = &pDevCtx->Connection.Usb.ChargingCycleTimestamp;

#ifdef DBG
	DumpAsHex(">> USB", rdrBuffer, (ULONG)rdrBufferLength);
#endif

	//
	// Handle special case of SIXAXIS.SYS emulation
	// 
	if (pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		RtlCopyMemory(
			pModCtx->GetFeatureReport,
			rdrBuffer,
			(rdrBufferLength < SIXAXIS_HID_GET_FEATURE_REPORT_SIZE) ? rdrBufferLength :
			SIXAXIS_HID_GET_FEATURE_REPORT_SIZE
		);

		pInReport = (PDS3_RAW_INPUT_REPORT)pModCtx->GetFeatureReport;

		pInReport->AccelerometerX = 0x03FF - _byteswap_ushort(pInReport->AccelerometerX);
		pInReport->AccelerometerY = _byteswap_ushort(pInReport->AccelerometerY);
		pInReport->AccelerometerZ = _byteswap_ushort(pInReport->AccelerometerZ);
		pInReport->Gyroscope = _byteswap_ushort(pInReport->Gyroscope);
	}
	
	battery = (DS_BATTERY_STATUS)((PUCHAR)rdrBuffer)[30];

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

			(void)Ds_SendOutputReport(pDevCtx);
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

				(void)Ds_SendOutputReport(pDevCtx);
			}
		}
	}
	else
	{
		pDevCtx->BatteryStatus = battery;
	}
	
	Ds_ProcessHidInputReport(pDevCtx, rdrBuffer, rdrBufferLength);

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
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
	NTSTATUS status;
	PUCHAR buffer;
	size_t bufferLength;
	PUCHAR inputBuffer;
	WDF_REQUEST_REUSE_PARAMS params;
	PDEVICE_CONTEXT pDevCtx;
	LARGE_INTEGER freq, * t1, t2;
	LONGLONG ms;
	DS_BATTERY_STATUS battery;
	DMF_CONTEXT_DsHidMini* pModCtx;
	PDS3_RAW_INPUT_REPORT pInReport;


	FuncEntry(TRACE_DSHIDMINIDRV);

#ifdef DBG
	TraceVerbose(
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
		TraceVerbose(TRACE_DSHIDMINIDRV, "Device has been disconnected");
		goto Exit;
	}
	
	if (Params->IoStatus.Status == STATUS_CANCELLED)
	{
		TraceVerbose(TRACE_DSHIDMINIDRV, "I/O Target has cancelled requests");
		goto Exit;
	}

	pDevCtx = (PDEVICE_CONTEXT)Context;
	pModCtx = DMF_CONTEXT_GET((DMFMODULE)pDevCtx->DsHidMiniModule);
	QueryPerformanceFrequency(&freq);

	if (!NT_SUCCESS(Params->IoStatus.Status)
		|| Params->Parameters.Ioctl.Output.Length < BTHPS3_SIXAXIS_HID_INPUT_REPORT_SIZE)
	{
		TraceError( TRACE_DSHIDMINIDRV,
			"%!FUNC! failed with status %!STATUS!", Params->IoStatus.Status);

		goto resendRequest;
	}

	buffer = (PUCHAR)WdfMemoryGetBuffer(
		Params->Parameters.Ioctl.Output.Buffer,
		&bufferLength);

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
		goto resendRequest;
	}

	//
	// Skip to report ID
	// 
	inputBuffer = &buffer[1];

	//
	// Handle special case of SIXAXIS.SYS emulation
	// 
	if (pDevCtx->Configuration.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible)
	{
		RtlCopyMemory(
			pModCtx->GetFeatureReport,
			inputBuffer,
			((bufferLength - 1) < SIXAXIS_HID_GET_FEATURE_REPORT_SIZE) ? (bufferLength - 1) :
			SIXAXIS_HID_GET_FEATURE_REPORT_SIZE
		);

		pInReport = (PDS3_RAW_INPUT_REPORT)pModCtx->GetFeatureReport;

		pInReport->AccelerometerX = 0x03FF - _byteswap_ushort(pInReport->AccelerometerX);
		pInReport->AccelerometerY = _byteswap_ushort(pInReport->AccelerometerY);
		pInReport->AccelerometerZ = _byteswap_ushort(pInReport->AccelerometerZ);
		pInReport->Gyroscope = _byteswap_ushort(pInReport->Gyroscope);
	}

	//
	// Grab battery info
	// 
	battery = (DS_BATTERY_STATUS)((PUCHAR)inputBuffer)[30];
		
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
				(WDFDEVICE)WdfObjectContextGetObject(Context),
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
					switch (battery)
					{
					case DsBatteryStatusCharged:
					case DsBatteryStatusFull:
					case DsBatteryStatusHigh:
						DS3_SET_LED(pDevCtx, DS3_LED_4);
						break;
					case DsBatteryStatusMedium:
						DS3_SET_LED(pDevCtx, DS3_LED_3);
						break;
					case DsBatteryStatusLow:
						DS3_SET_LED(pDevCtx, DS3_LED_2);
						break;
					case DsBatteryStatusDying:
						DS3_SET_LED(pDevCtx, DS3_LED_1);
						break;
					default:
						break;
					}

					(void)Ds_SendOutputReport(pDevCtx);
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
	if (((inputBuffer[3] >> 3) & 1U) && ((inputBuffer[3] >> 2) & 1U) && (inputBuffer[4] & 1U))
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
			return;
		}
	}
	else
	{
		pDevCtx->Connection.Bth.QuickDisconnectTimestamp.QuadPart = 0;
	}

	//
	// Idle disconnect detection
	// 
	if (DS3_RAW_IS_IDLE(inputBuffer))
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
			return;
		}
	}
	else
	{
		pDevCtx->Connection.Bth.IdleDisconnectTimestamp.QuadPart = 0;
	}
	
	Ds_ProcessHidInputReport(pDevCtx, inputBuffer, bufferLength - 1);

	resendRequest:

	WDF_REQUEST_REUSE_PARAMS_INIT(
		&params,
		WDF_REQUEST_REUSE_NO_FLAGS,
		STATUS_SUCCESS
	);
	status = WdfRequestReuse(Request, &params);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSHIDMINIDRV,
			"WdfRequestReuse failed with status %!STATUS!",
			status
		);
		return;
	}

	status = WdfIoTargetFormatRequestForIoctl(
		pDevCtx->Connection.Bth.BthIoTarget,
		pDevCtx->Connection.Bth.HidInterrupt.ReadRequest,
		IOCTL_BTHPS3_HID_INTERRUPT_READ,
		NULL,
		NULL,
		pDevCtx->Connection.Bth.HidInterrupt.ReadMemory,
		NULL
	);
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSHIDMINIDRV,
			"WdfIoTargetFormatRequestForIoctl failed with status %!STATUS!",
			status
		);
	}

	WdfRequestSetCompletionRoutine(
		pDevCtx->Connection.Bth.HidInterrupt.ReadRequest,
		DsBth_HidInterruptReadRequestCompletionRoutine,
		pDevCtx
	);

	if (WdfRequestSend(
		pDevCtx->Connection.Bth.HidInterrupt.ReadRequest,
		pDevCtx->Connection.Bth.BthIoTarget,
		WDF_NO_SEND_OPTIONS
	) == FALSE) {
		status = WdfRequestGetStatus(pDevCtx->Connection.Bth.HidInterrupt.ReadRequest);
	}
	if (!NT_SUCCESS(status))
	{
		TraceError(
			TRACE_DSHIDMINIDRV,
			"WdfRequestSend failed with status %!STATUS!",
			status
		);
	}

Exit:

	FuncExitNoReturn(TRACE_DSHIDMINIDRV);
}

#pragma endregion

#pragma region Output Report processing

ScheduledTask_Result_Type
DMF_OutputReportScheduledTaskCallback(
	_In_ DMFMODULE DmfModule,
	_In_ VOID* CallbackContext,
	_In_ WDF_POWER_DEVICE_STATE PreviousState)
{
	UNREFERENCED_PARAMETER(DmfModule);
	UNREFERENCED_PARAMETER(PreviousState);

	NTSTATUS status = STATUS_UNSUCCESSFUL;
	PDEVICE_CONTEXT pDevCtx = (PDEVICE_CONTEXT)CallbackContext;
	PUCHAR buffer = NULL;
	size_t bufferSize = 0;
	LARGE_INTEGER freq, * t1, t2;
	LONGLONG ms;
	
	FuncEntry(TRACE_DSHIDMINIDRV);

	QueryPerformanceFrequency(&freq);
	
	WdfWaitLockAcquire(pDevCtx->OutputReport.Lock, NULL);

	t1 = &pDevCtx->OutputReport.Cache.LastSentTimestamp;
	
	QueryPerformanceCounter(&t2);
	
	//
	// Get main output report buffer protocol-agnostic
	// 
	DS3_GET_UNIFIED_OUTPUT_REPORT_BUFFER(
		pDevCtx,
		&buffer,
		&bufferSize
	);
	
	switch (pDevCtx->ConnectionType)
	{
	case DsDeviceConnectionTypeUsb:

		//
		// Don't send if no state change has occurred
		// 
		if (pDevCtx->Configuration.IsOutputDeduplicatorEnabled > 0
			&& RtlCompareMemory(
				buffer,
				pDevCtx->OutputReport.Cache.LastReport,
				bufferSize
			) == bufferSize)
		{
			break;
		}

		status = USB_WriteInterruptOutSync(
			pDevCtx
		);

		if (NT_SUCCESS(status))
		{
			RtlCopyMemory(
				pDevCtx->OutputReport.Cache.LastReport,
				buffer,
				bufferSize
			);
		}
		
		break;

	case DsDeviceConnectionTypeBth:

		//
		// Don't send if no state change has occurred
		// 
		if (pDevCtx->Configuration.IsOutputDeduplicatorEnabled > 0
			&& RtlCompareMemory(
				buffer,
				pDevCtx->OutputReport.Cache.LastReport,
				bufferSize
			) == bufferSize)
		{
			break;
		}

		//
		// Calculate delay, the smaller the more frequent packets are sent
		// 
		ms = (t2.QuadPart - t1->QuadPart) / (freq.QuadPart / 1000);

		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"Time span since last packet was sent: %I64d ms",
			ms
		);

		//
		// TODO: improve, emergency dropout
		// 
		if (pDevCtx->Configuration.IsOutputRateControlEnabled > 0
			&& ms < pDevCtx->Configuration.OutputRateControlPeriodMs)
		{
			TraceError(
				TRACE_DSHIDMINIDRV,
				"Host is sending too fast, dropping"
			);
			break;
		}
		
		status = DsBth_SendHidControlWriteRequest(pDevCtx);

		if (NT_SUCCESS(status))
		{
			// 
			// Store last successful send
			// 

			QueryPerformanceCounter(t1);
			
			RtlCopyMemory(
				pDevCtx->OutputReport.Cache.LastReport,
				buffer,
				bufferSize
			);
		}
		
		break;

	default:
		status = STATUS_INVALID_PARAMETER;
	}

	WdfWaitLockRelease(pDevCtx->OutputReport.Lock);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);
	
	return NT_SUCCESS(status) ? ScheduledTask_WorkResult_SuccessButTryAgain : ScheduledTask_WorkResult_Fail;
}

#pragma endregion

NTSTATUS Ds_SendOutputReport(PDEVICE_CONTEXT Context)
{
	NTSTATUS status;
	
	FuncEntry(TRACE_DSHIDMINIDRV);
	
	status = DMF_ScheduledTask_ExecuteNow(
		Context->OutputReport.Scheduler,
		Context
	);

	FuncExit(TRACE_DSHIDMINIDRV, "status=%!STATUS!", status);

	return status;
}

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
