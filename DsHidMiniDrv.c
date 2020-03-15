#include "Driver.h"
#include "DsHidMiniDrv.tmh"


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
#define DSHIDMINI_DEVICE_STRING          L"DsHidMini device"
#define DSHIDMINI_MANUFACTURER_STRING    L"DsHidMini device Manufacturer string"
#define DSHIDMINI_PRODUCT_STRING         L"DsHidMini device Product string"
#define DSHIDMINI_SERIAL_NUMBER_STRING   L"DsHidMini device Serial Number string"
#define DSHIDMINI_DEVICE_STRING_INDEX    5


// This macro declares the following function:
// DMF_CONTEXT_GET()
//
DMF_MODULE_DECLARE_CONTEXT(DsHidMini)

// This macro declares the following function:
// DMF_CONFIG_GET()
//
DMF_MODULE_DECLARE_CONFIG(DsHidMini)


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

	PAGED_CODE();

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

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

	PAGED_CODE();

	UNREFERENCED_PARAMETER(DmfParentModuleAttributes);

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	moduleConfig = DMF_CONFIG_GET(DmfModule);
	moduleContext = DMF_CONTEXT_GET(DmfModule);

	DMF_CONFIG_VirtualHidMini_AND_ATTRIBUTES_INIT(&vHidCfg,
		&moduleAttributes);

	//
	// TODO: populate properly
	// 

	vHidCfg.VendorId = 0x1337;
	vHidCfg.ProductId = 0x1337;
	vHidCfg.VersionNumber = 0x0101;

	vHidCfg.HidDescriptor = &G_Ds3HidDescriptor_Split_Mode;
	vHidCfg.HidDescriptorLength = sizeof(G_Ds3HidDescriptor_Split_Mode);
	vHidCfg.HidReportDescriptor = G_Ds3HidReportDescriptor_Split_Mode;
	vHidCfg.HidReportDescriptorLength = G_Ds3HidDescriptor_Split_Mode.DescriptorList[0].wReportLength;

	vHidCfg.HidDeviceAttributes.VendorID = 0x1337;
	vHidCfg.HidDeviceAttributes.ProductID = 0x1337;
	vHidCfg.HidDeviceAttributes.VersionNumber = 0x0101;
	vHidCfg.HidDeviceAttributes.Size = sizeof(HID_DEVICE_ATTRIBUTES);

	vHidCfg.GetInputReport = DsHidMini_GetInputReport;

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
	UNREFERENCED_PARAMETER(DmfModule);

	PAGED_CODE();

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Entry");

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSHIDMINIDRV, "%!FUNC! Exit");

	return STATUS_SUCCESS;
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

    dmfModuleParent = DMF_ParentModuleGet(DmfModule);
    moduleContext = DMF_CONTEXT_GET(dmfModuleParent);
        
    *Buffer = (UCHAR*)moduleContext->InputReport;
    *BufferSize = DS3_HID_INPUT_REPORT_SIZE;

    return STATUS_SUCCESS;
}

VOID
DsUsbEvtUsbInterruptPipeReadComplete(
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

	pDeviceContext = DeviceGetContext(Context);
	dmfModule = (DMFMODULE)pDeviceContext->DsHidMiniModule;
	moduleContext = DMF_CONTEXT_GET(dmfModule);
	rdrBuffer = WdfMemoryGetBuffer(Buffer, &rdrBufferLength);

	//
	// Updated Input Report
	// 
	RtlCopyMemory(moduleContext->InputReport, rdrBuffer, rdrBufferLength);

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
