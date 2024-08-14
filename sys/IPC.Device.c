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
	FuncEntry(TRACE_IPC);

	if (MessageHeader->Command.Device == DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO)
	{
		const PDSHM_IPC_MSG_PAIR_TO_REQUEST request = (PDSHM_IPC_MSG_PAIR_TO_REQUEST)MessageHeader;

		TraceVerbose(
			TRACE_IPC,
			"Received pairing request, new host address: %02X:%02X:%02X:%02X:%02X:%02X",
			request->Address.Address[0],
			request->Address.Address[1],
			request->Address.Address[2],
			request->Address.Address[3],
			request->Address.Address[4],
			request->Address.Address[5]
		);

		// TODO: this changes state for runtime settings, improve
		DeviceContext->Configuration.DevicePairingMode = DsDevicePairingModeCustom;
		RtlCopyMemory(&DeviceContext->Configuration.CustomHostAddress, &request->Address, sizeof(BD_ADDR));

		const WDFDEVICE device = WdfObjectContextGetObject(DeviceContext);

		NTSTATUS writeStatus = DsUsb_Ds3PairToNewHost(device);
		NTSTATUS readStatus = STATUS_UNSUCCESSFUL;
		if (!NT_SUCCESS(readStatus = DsUsb_Ds3RequestHostAddress(device)))
		{
			TraceError(
				TRACE_IPC,
				"DsUsb_Ds3RequestHostAddress failed with status %!STATUS!",
				readStatus
			);
		}

		DSHM_IPC_MSG_PAIR_TO_RESPONSE_INIT(
			(PDSHM_IPC_MSG_PAIR_TO_REPLY)MessageHeader,
			MessageHeader->TargetIndex,
			writeStatus,
			readStatus
		);
	}

	// TODO: implement me!

	FuncExitNoReturn(TRACE_IPC);

	return STATUS_SUCCESS;
}
