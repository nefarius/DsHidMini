#include "Driver.h"
#include "DsHidMiniDrv.tmh"

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
    UNREFERENCED_PARAMETER(DmfModule);
    UNREFERENCED_PARAMETER(Request);
    UNREFERENCED_PARAMETER(Buffer);
    UNREFERENCED_PARAMETER(BufferSize);
	
    //DMFMODULE dmfModuleParent;
    //DMF_CONTEXT_DsHidMini* moduleContext;
    //HIDMINI_INPUT_REPORT* readReport;
    //
    //UNREFERENCED_PARAMETER(Request);
    //
    //dmfModuleParent = DMF_ParentModuleGet(DmfModule);
    //moduleContext = DMF_CONTEXT_GET(dmfModuleParent);
    //
    //readReport = &moduleContext->ReadReport;
    //
    //// Populate data to return to caller.
    ////
    //readReport->ReportId = CONTROL_FEATURE_REPORT_ID;
    //readReport->Data = moduleContext->DeviceData;
    //
    //// Return to caller.
    ////
    //*Buffer = (UCHAR*)readReport;
    //*BufferSize = sizeof(HIDMINI_INPUT_REPORT);

    return STATUS_SUCCESS;
}
