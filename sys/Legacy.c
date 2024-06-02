#include "Driver.h"
#include "Legacy.tmh"


NTSTATUS
DsHidMini_GetInputReport(
	_In_ DMFMODULE DmfModule,
	_In_ WDFREQUEST Request,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
)
{
	const NTSTATUS status = STATUS_NOT_IMPLEMENTED;

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
