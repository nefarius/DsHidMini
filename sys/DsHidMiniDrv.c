#include "Driver.h"
#include "DsHidMiniDrv.tmh"
#include <ini.h>


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
// Read device configuration from INI file
// 
static int DsHidMini_ConfigParserHandler(void* user, const char* section, const char* name,
	const char* value)
{
	PDS_DRIVER_CONFIGURATION pconfig = (PDS_DRIVER_CONFIGURATION)user;

#define MATCH(s, n) strcmp(section, s) == 0 && strcmp(name, n) == 0

	if (MATCH("Global", "HidDeviceMode")) {
		pconfig->HidDeviceMode = (DS_HID_DEVICE_MODE)atoi(value);
	}
	else if (MATCH("Global", "MuteDigitalPressureButtons")) {
		pconfig->MuteDigitalPressureButtons = (atoi(value) > 0);
	}
	else if (MATCH("Global", "VendorId")) {
		pconfig->VendorId = (USHORT)strtol(value, NULL, 16);
	}
	else if (MATCH("Global", "ProductId")) {
		pconfig->ProductId = (USHORT)strtol(value, NULL, 16);
	}
	else if (MATCH("Global", "VersionNumber")) {
		pconfig->VersionNumber = (USHORT)strtol(value, NULL, 16);
	}
	else {
		return 0;  /* unknown section/name, error */
	}

	return 1;
}

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
	// Load configuration from INI or use defaults
	// 
	if (ini_parse(
		"C:\\ProgramData\\DsHidMini.ini",
		DsHidMini_ConfigParserHandler,
		&pDevCtx->Configuration) < 0)
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSHIDMINIDRV,
			"Failed to load configuration from \"C:\\ProgramData\\DsHidMini.ini\", using defaults"
		);
	}
#ifdef DBG
	else
	{
		TraceEvents(TRACE_LEVEL_INFORMATION,
			TRACE_DSHIDMINIDRV,
			"!! Configuration loaded");
	}
#endif

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
	DMF_CONFIG_DsHidMini* moduleConfig;
	DMF_CONTEXT_DsHidMini* moduleContext;
	DMF_CONFIG_VirtualHidMini vHidCfg;
	PDEVICE_CONTEXT pDevCtx;

	PAGED_CODE();

	UNREFERENCED_PARAMETER(DmfParentModuleAttributes);

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	moduleConfig = DMF_CONFIG_GET(DmfModule);
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
	UNREFERENCED_PARAMETER(DmfModule);

	PAGED_CODE();

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

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

NTSTATUS

//
// Submit new HID Input Report.
// 
DsHidMini_RetrieveNextInputReport(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_Out_ UCHAR** Buffer,
	_Out_ ULONG* BufferSize
)
{
	DMFMODULE dmfModuleParent;
	DMF_CONTEXT_DsHidMini* moduleContext;

	UNREFERENCED_PARAMETER(Request);

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	dmfModuleParent = DMF_ParentModuleGet(DmfModule);
	moduleContext = DMF_CONTEXT_GET(dmfModuleParent);

	*Buffer = moduleContext->InputReport;
	*BufferSize = DS3_HID_INPUT_REPORT_SIZE;

#ifdef DBG
	DumpAsHex(">> Report", *Buffer, (ULONG)*BufferSize);
#endif

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");

	return STATUS_SUCCESS;
}

NTSTATUS
DsHidMini_GetFeature(
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

	return STATUS_SUCCESS;
}

NTSTATUS
DsHidMini_SetFeature(
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

	return STATUS_SUCCESS;
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

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");

	return STATUS_SUCCESS;
}

NTSTATUS
DsHidMini_WriteReport(
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

	return STATUS_SUCCESS;
}

VOID

//
// Called when data is available on the USB Interrupt IN pipe.
//  
DsUsb_EvtUsbInterruptPipeReadComplete(
	WDFUSBPIPE  Pipe,
	WDFMEMORY   Buffer,
	size_t      NumBytesTransferred,
	WDFCONTEXT  Context
)
{
	NTSTATUS                status;
	PDEVICE_CONTEXT         pDeviceContext;
	size_t                  rdrBufferLength;
	LPVOID                  rdrBuffer;
	DMFMODULE               dmfModule;
	DMF_CONTEXT_DsHidMini* moduleContext;

	UNREFERENCED_PARAMETER(Pipe);
	UNREFERENCED_PARAMETER(NumBytesTransferred);

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	pDeviceContext = DeviceGetContext(Context);
	dmfModule = (DMFMODULE)pDeviceContext->DsHidMiniModule;
	moduleContext = DMF_CONTEXT_GET(dmfModule);
	rdrBuffer = WdfMemoryGetBuffer(Buffer, &rdrBufferLength);

#ifdef DBG
	DumpAsHex(">> USB", rdrBuffer, (ULONG)rdrBufferLength);
#endif

#pragma region HID Input Report (ID 01) processing

	switch (pDeviceContext->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeMulti:

		DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_01(
			rdrBuffer,
			moduleContext->InputReport,
			pDeviceContext->Configuration.MuteDigitalPressureButtons
		);

#ifdef DBG
		DumpAsHex(">> MULTI", moduleContext->InputReport, DS3_HID_INPUT_REPORT_SIZE);
#endif

		break;
	case DsHidMiniDeviceModeSingle:

		DS3_RAW_TO_SINGLE_HID_INPUT_REPORT(
			rdrBuffer,
			moduleContext->InputReport,
			pDeviceContext->Configuration.MuteDigitalPressureButtons
		);

#ifdef DBG
		DumpAsHex(">> SINGLE", moduleContext->InputReport, DS3_HID_INPUT_REPORT_SIZE);
#endif

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
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DSHIDMINIDRV,
			"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
	}

#pragma endregion

#pragma region HID Input Report (ID 02) processing

	if (pDeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeMulti)
	{
		DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_02(
			rdrBuffer,
			moduleContext->InputReport
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			moduleContext->DmfModuleVirtualHidMini,
			DsHidMini_RetrieveNextInputReport
		);
		if (!NT_SUCCESS(status))
		{
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
		}
	}

#pragma endregion

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");
}

VOID DumpAsHex(PCSTR Prefix, PVOID Buffer, ULONG BufferLength)
{
#ifdef DBG
	PSTR   dumpBuffer;
	size_t  dumpBufferLength;
	ULONG   i;

	dumpBufferLength = ((BufferLength * sizeof(CHAR)) * 2) + 1;
	dumpBuffer = malloc(dumpBufferLength);
	if (dumpBuffer)
	{

		RtlZeroMemory(dumpBuffer, dumpBufferLength);

		for (i = 0; i < BufferLength; i++)
		{
			sprintf(&dumpBuffer[i * 2], "%02X", ((PUCHAR)Buffer)[i]);
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
	DMFMODULE                   dmfModule;
	DMF_CONTEXT_DsHidMini*		moduleContext;


	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	UNREFERENCED_PARAMETER(Target);

	pDeviceContext = (PDEVICE_CONTEXT)Context;
	dmfModule = (DMFMODULE)pDeviceContext->DsHidMiniModule;
	moduleContext = DMF_CONTEXT_GET(dmfModule);

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

	inputBuffer = &buffer[1];

#pragma region HID Input Report (ID 01) processing

	switch (pDeviceContext->Configuration.HidDeviceMode)
	{
	case DsHidMiniDeviceModeMulti:
		DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_01(
			inputBuffer,
			moduleContext->InputReport,
			pDeviceContext->Configuration.MuteDigitalPressureButtons
		);
		break;
	case DsHidMiniDeviceModeSingle:
		DS3_RAW_TO_SINGLE_HID_INPUT_REPORT(
			inputBuffer,
			moduleContext->InputReport,
			pDeviceContext->Configuration.MuteDigitalPressureButtons
		);
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
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DSHIDMINIDRV,
			"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
	}

#pragma endregion

#pragma region HID Input Report (ID 02) processing

	if (pDeviceContext->Configuration.HidDeviceMode == DsHidMiniDeviceModeMulti)
	{
		DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_02(
			inputBuffer,
			moduleContext->InputReport
		);

		//
		// Notify new Input Report is available
		// 
		status = DMF_VirtualHidMini_InputReportGenerate(
			moduleContext->DmfModuleVirtualHidMini,
			DsHidMini_RetrieveNextInputReport
		);
		if (!NT_SUCCESS(status))
		{
			TraceEvents(TRACE_LEVEL_ERROR, TRACE_DSHIDMINIDRV,
				"DMF_VirtualHidMini_InputReportGenerate failed with status %!STATUS!", status);
		}
	}

#pragma endregion

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
		pDeviceContext->Connection.Bth.HidInterruptReadRequest,
		IOCTL_BTHPS3_HID_INTERRUPT_READ,
		NULL,
		NULL,
		pDeviceContext->Connection.Bth.HidInterruptReadMemory,
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
		pDeviceContext->Connection.Bth.HidInterruptReadRequest,
		DsBth_HidInterruptReadRequestCompletionRoutine,
		pDeviceContext
	);

	if (WdfRequestSend(
		pDeviceContext->Connection.Bth.HidInterruptReadRequest,
		pDeviceContext->Connection.Bth.BthIoTarget,
		WDF_NO_SEND_OPTIONS
	) == FALSE) {
		status = WdfRequestGetStatus(pDeviceContext->Connection.Bth.HidInterruptReadRequest);
	}
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSHIDMINIDRV,
			"WdfRequestSend failed with status %!STATUS!",
			status
		);
	}

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");
}
