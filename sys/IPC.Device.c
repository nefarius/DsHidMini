#include "Driver.h"
#include "IPC.Device.tmh"


//
// Processes incoming IPC messages targeted to this device instance
// 
_Use_decl_annotations_
NTSTATUS
DSHM_EvtDispatchDeviceMessage(
	_In_ PDEVICE_CONTEXT DeviceContext,
	_In_ PDSHM_IPC_MSG_HEADER MessageHeader
)
{
	FuncEntry(TRACE_DEVICE);

	UNREFERENCED_PARAMETER(DeviceContext);
	UNREFERENCED_PARAMETER(MessageHeader);

	if (MessageHeader->Command.Device == DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO)
	{
		const PDSHM_IPC_MSG_PAIR_TO_REQUEST request = (PDSHM_IPC_MSG_PAIR_TO_REQUEST)MessageHeader;

		TraceVerbose(
			TRACE_DEVICE,
			"Received pairing request, new host address: %02X:%02X:%02X:%02X:%02X:%02X",
			request->Address.Address[0],
			request->Address.Address[1],
			request->Address.Address[2],
			request->Address.Address[3],
			request->Address.Address[4],
			request->Address.Address[5]
		);

		// TODO: implement me!

		DSHM_IPC_MSG_PAIR_TO_RESPONSE_INIT(
			(PDSHM_IPC_MSG_PAIR_TO_REPLY)MessageHeader,
			MessageHeader->TargetIndex,
			STATUS_NOT_IMPLEMENTED
		);
	}

	// TODO: implement me!

	FuncExitNoReturn(TRACE_DEVICE);

	return STATUS_SUCCESS;
}

