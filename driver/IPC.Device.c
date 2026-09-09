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

	NTSTATUS status = STATUS_NOT_IMPLEMENTED;

	if (MessageHeader->Command.Device == DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO)
	{
		if (MessageHeader->Size < sizeof(DSHM_IPC_MSG_PAIR_TO_REQUEST))
		{
			DSHM_IPC_MSG_PAIR_TO_RESPONSE_INIT(
				(PDSHM_IPC_MSG_PAIR_TO_REPLY)MessageHeader,
				MessageHeader->TargetIndex,
				STATUS_INVALID_USER_BUFFER,
				STATUS_INVALID_USER_BUFFER
			);

			status = STATUS_SUCCESS;
			FuncExit(TRACE_IPC, "status=%!STATUS!", status);
			return status;
		}

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

		NTSTATUS readStatus = STATUS_UNSUCCESSFUL;
		const NTSTATUS writeStatus = DsUsb_Ds3PairAndVerify(device, &readStatus);
		if (!NT_SUCCESS(readStatus))
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

		status = STATUS_SUCCESS;
	}
	else if (MessageHeader->Command.Device == DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX)
	{
		NTSTATUS applyStatus = STATUS_INVALID_USER_BUFFER;

		if (MessageHeader->Size >= sizeof(DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST))
		{
			const PDSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST request =
				(PDSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST)MessageHeader;

			TraceVerbose(
				TRACE_IPC,
				"Received player-index request: %d",
				request->PlayerIndex
			);

			applyStatus = DsLed_ApplyIpcPlayerIndex(DeviceContext, request->PlayerIndex);
		}

		DSHM_IPC_MSG_SET_PLAYER_INDEX_RESPONSE_INIT(
			(PDSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY)MessageHeader,
			MessageHeader->TargetIndex,
			applyStatus
		);

		status = STATUS_SUCCESS;
	}
	else if (MessageHeader->Command.Device == DSHM_IPC_MSG_CMD_DEVICE_USB_POWER_OFF)
	{
		NTSTATUS indicatorsOffStatus = STATUS_NOT_SUPPORTED;
		NTSTATUS shutdownStatus = STATUS_NOT_SUPPORTED;

		if (MessageHeader->Size < sizeof(DSHM_IPC_MSG_USB_POWER_OFF_REQUEST))
		{
			indicatorsOffStatus = STATUS_INVALID_USER_BUFFER;
			shutdownStatus = STATUS_INVALID_USER_BUFFER;
		}
		else if (DeviceContext->ConnectionType != DsDeviceConnectionTypeUsb)
		{
			TraceWarning(
				TRACE_IPC,
				"USB power-off requested for a non-USB device"
			);
		}
		else
		{
			//
			// PS3 sequence from issue #366: zero the 48-byte output report on
			// EP0, then send Feature 0xF4 disable. Always attempt shutdown even
			// if the first transfer fails.
			// 
			UCHAR zeroOutputReport[48] = { 0 };

			indicatorsOffStatus = DsUsb_Ds3SendOutputReportControl(
				DeviceContext,
				zeroOutputReport,
				ARRAYSIZE(zeroOutputReport)
			);

			if (!NT_SUCCESS(indicatorsOffStatus))
			{
				TraceWarning(
					TRACE_IPC,
					"USB power-off indicators-off report failed with %!STATUS!",
					indicatorsOffStatus
				);
			}

			shutdownStatus = DsUsb_Ds3Shutdown(DeviceContext);

			if (!NT_SUCCESS(shutdownStatus))
			{
				TraceError(
					TRACE_IPC,
					"DsUsb_Ds3Shutdown failed with %!STATUS!",
					shutdownStatus
				);
			}
		}

		DSHM_IPC_MSG_USB_POWER_OFF_RESPONSE_INIT(
			(PDSHM_IPC_MSG_USB_POWER_OFF_REPLY)MessageHeader,
			MessageHeader->TargetIndex,
			indicatorsOffStatus,
			shutdownStatus
		);

		status = STATUS_SUCCESS;
	}
	else if (MessageHeader->Command.Device == DSHM_IPC_MSG_CMD_DEVICE_SET_RUMBLE)
	{
		NTSTATUS applyStatus = STATUS_INVALID_USER_BUFFER;

		if (MessageHeader->Size >= sizeof(DSHM_IPC_MSG_SET_RUMBLE_REQUEST))
		{
			const PDSHM_IPC_MSG_SET_RUMBLE_REQUEST request =
				(PDSHM_IPC_MSG_SET_RUMBLE_REQUEST)MessageHeader;

			TraceVerbose(
				TRACE_IPC,
				"Received rumble request: large=%d small=%d",
				request->LargeMotor,
				request->SmallMotor
			);

			applyStatus = DSHM_SetIpcRumble(
				DeviceContext,
				request->LargeMotor,
				request->SmallMotor
			);
		}

		DSHM_IPC_MSG_SET_RUMBLE_RESPONSE_INIT(
			(PDSHM_IPC_MSG_SET_RUMBLE_REPLY)MessageHeader,
			MessageHeader->TargetIndex,
			applyStatus
		);

		status = STATUS_SUCCESS;
	}
	else if (MessageHeader->Command.Device == DSHM_IPC_MSG_CMD_DEVICE_SET_ALTERNATE_RUMBLE_MODE)
	{
		NTSTATUS applyStatus = STATUS_INVALID_USER_BUFFER;

		if (MessageHeader->Size >= sizeof(DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST))
		{
			const PDSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST request =
				(PDSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST)MessageHeader;

			//
			// Volatile only: do not persist to JSON. A config hot-reload
			// restores AlternativeMode.IsEnabled from disk. Hold the output
			// lock so this cannot tear against DS3_PROCESS_RUMBLE_STRENGTH.
			// 
			WdfWaitLockAcquire(DeviceContext->OutputReport.Lock, NULL);
			DeviceContext->RumbleControlState.AltMode.IsEnabled = request->IsEnabled ? TRUE : FALSE;
			const BOOLEAN isEnabled = DeviceContext->RumbleControlState.AltMode.IsEnabled;
			WdfWaitLockRelease(DeviceContext->OutputReport.Lock);
			applyStatus = STATUS_SUCCESS;

			TraceVerbose(
				TRACE_IPC,
				"Alternate rumble mode is now %!BOOLEAN!",
				isEnabled
			);
		}

		DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_RESPONSE_INIT(
			(PDSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY)MessageHeader,
			MessageHeader->TargetIndex,
			applyStatus
		);

		status = STATUS_SUCCESS;
	}

	FuncExit(TRACE_IPC, "status=%!STATUS!", status);

	return status;
}
