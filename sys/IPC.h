#pragma once

#define DSHM_IPC_FILE_MAP_NAME		"Global\\DsHidMiniSharedMemory"
#define DSHM_IPC_MUTEX_NAME			"Global\\DsHidMiniCommandMutex"
#define DSHM_IPC_READ_EVENT_NAME	"Global\\DsHidMiniReadEvent"
#define DSHM_IPC_WRITE_EVENT_NAME	"Global\\DsHidMiniWriteEvent"


//
// Describes the type of IPC message response behavior
// 
typedef enum
{
	//
	// Invalid/reserved, do not use
	// 
	DSHM_IPC_MSG_TYPE_INVALID = 0,
	//
	// Client-to-driver data incoming, no acknowledgment/reply requested
	// 
	DSHM_IPC_MSG_TYPE_REQUEST_ONLY,
	//
	// Client-to-driver data incoming, must be acknowledged by reply
	// 
	DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE,
	//
	// Client requested data, there is nothing to read for the driver
	// 
	DSHM_IPC_MSG_TYPE_RESPONSE_ONLY,
	//
	// Driver-to-client response to a previous DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE
	// 
	DSHM_IPC_MSG_TYPE_REQUEST_REPLY
} DSHM_IPC_MSG_TYPE;

//
// Describes the message receiver
// 
typedef enum
{
	//
	// Invalid/reserved, do not use
	// 
	DSHM_IPC_MSG_TARGET_INVALID = 0,
	//
	// The message is targeted at the driver
	// 
	DSHM_IPC_MSG_TARGET_DRIVER,
	//
	// The message is targeted at a device
	// 
	DSHM_IPC_MSG_TARGET_DEVICE,
	//
	// The message is targeted at the client/caller/app
	// 
	DSHM_IPC_MSG_TARGET_CLIENT
} DSHM_IPC_MSG_TARGET;

//
// Describes a per-driver command
// 
typedef enum
{
	//
	// Invalid/reserved, do not use
	// 
	DSHM_IPC_MSG_CMD_DRIVER_INVALID = 0,
	//
	// Message without payload, useful to check for functionality
	// 
	DSHM_IPC_MSG_CMD_DRIVER_PING
} DSHM_IPC_MSG_CMD_DRIVER;

//
// Describes a per-device command
// 
typedef enum
{
	//
	// Invalid/reserved, do not use
	// 
	DSHM_IPC_MSG_CMD_DEVICE_INVALID = 0,
	//
	// Pair a given device to a new host
	// 
	DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO,
	//
	// Requests a player index update (switch player LED etc.)
	// 
	DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX,
	//
	// Requests a wait handle for input report state changes
	// 
	DSHM_IPC_MSG_CMD_DEVICE_GET_HID_WAIT_HANDLE,
} DSHM_IPC_MSG_CMD_DEVICE;

//
// Prefix of every packet describing the message
// 
typedef struct _DSHM_IPC_MSG_HEADER
{
	//
	// What request-behavior is expected (request, request-reply, ...)
	// 
	DSHM_IPC_MSG_TYPE Type;

	//
	// What component is this message targeting (driver, device, ...)
	// 
	DSHM_IPC_MSG_TARGET Target;

	//
	// What command is this message carrying
	// 
	union
	{
		DSHM_IPC_MSG_CMD_DRIVER Driver;

		DSHM_IPC_MSG_CMD_DEVICE Device;
	} Command;

	//
	// One-based index of which device is this message for
	//   Set to 0 if driver is targeted
	// 
	UINT32 TargetIndex;

	//
	// The size of the entire message (header + payload) in bytes
	//   A size of 0 is invalid
	// 
	UINT32 Size;
} DSHM_IPC_MSG_HEADER, * PDSHM_IPC_MSG_HEADER;

//
// Updates a specified devices' host address
// 
typedef struct _DSHM_IPC_MSG_PAIR_TO_REQUEST
{
	DSHM_IPC_MSG_HEADER Header;

	BD_ADDR Address;
	
} DSHM_IPC_MSG_PAIR_TO_REQUEST, *PDSHM_IPC_MSG_PAIR_TO_REQUEST;

//
// Reply to struct _DSHM_IPC_MSG_PAIR_TO_REQUEST
// 
typedef struct _DSHM_IPC_MSG_PAIR_TO_REPLY
{
	DSHM_IPC_MSG_HEADER Header;

	//
	// NTSTATUS of the set address action
	// 
	NTSTATUS WriteStatus;

	//
	// NTSTATUS of the get address action
	// 
	NTSTATUS ReadStatus;
	
} DSHM_IPC_MSG_PAIR_TO_REPLY, *PDSHM_IPC_MSG_PAIR_TO_REPLY;

//
// Updates the player index of a given device
// 
typedef struct _DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST
{
	DSHM_IPC_MSG_HEADER Header;

	//
	// The new player index to set
	//   Valid values are 1 to 7
	//   
	BYTE PlayerIndex;
	
} DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST, *PDSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST;

//
// Reply to struct _DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST
// 
typedef struct _DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY
{
	DSHM_IPC_MSG_HEADER Header;

	NTSTATUS NtStatus;
	
} DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY, *PDSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY;

//
// Requests the driver host process PID and a wait handle for new input reports
// 
typedef struct _DSHM_IPC_MSG_GET_HID_WAIT_HANDLE_RESPONSE
{
	DSHM_IPC_MSG_HEADER Header;

	//
	// The driver hosting process PID
	// 
	DWORD ProcessId;

	//
	// A handle to an auto-reset event
	// 
	HANDLE WaitHandle;
	
} DSHM_IPC_MSG_GET_HID_WAIT_HANDLE_RESPONSE, *PDSHM_IPC_MSG_GET_HID_WAIT_HANDLE_RESPONSE;

typedef
_Function_class_(EVT_DSHM_IPC_DispatchDeviceMessage)
_IRQL_requires_same_
_IRQL_requires_max_(PASSIVE_LEVEL)
NTSTATUS
EVT_DSHM_IPC_DispatchDeviceMessage(
	_In_ PDEVICE_CONTEXT DeviceContext,
	_In_ PDSHM_IPC_MSG_HEADER MessageHeader
);

typedef EVT_DSHM_IPC_DispatchDeviceMessage *PFN_DSHM_IPC_DispatchDeviceMessage;

#define DSHM_IPC_SIGNAL_WRITE_DONE(_ctx_) \
	SetEvent((_ctx_)->IPC.WriteEvent)

#define DSHM_IPC_MSG_EXPECTS_REPLY(_msg_) \
	((_msg_)->Type == DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE \
		|| (_msg_)->Type == DSHM_IPC_MSG_TYPE_RESPONSE_ONLY)

#define DSHM_IPC_MSG_IS_PING(_msg_) \
	((_msg_)->Type == DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE \
	&& (_msg_)->Target == DSHM_IPC_MSG_TARGET_DRIVER \
	&& (_msg_)->Command.Driver == DSHM_IPC_MSG_CMD_DRIVER_PING \
	&& (_msg_)->TargetIndex == 0 \
	&& (_msg_)->Size == sizeof(DSHM_IPC_MSG_HEADER))

#define DSHM_IPC_MSG_IS_FOR_DEVICE(_msg_) \
	((_msg_)->Type != DSHM_IPC_MSG_TYPE_INVALID \
	&& (_msg_)->Target == DSHM_IPC_MSG_TARGET_DEVICE \
	&& (_msg_)->Command.Device != DSHM_IPC_MSG_CMD_DEVICE_INVALID \
	&& (_msg_)->TargetIndex > 0 \
	&& (_msg_)->TargetIndex < DSHM_MAX_DEVICES \
	&& (_msg_)->Size >= sizeof(DSHM_IPC_MSG_HEADER))

VOID
FORCEINLINE
DSHM_IPC_MSG_PING_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_HEADER Message
)
{
	RtlZeroMemory(Message, sizeof(DSHM_IPC_MSG_HEADER));

	Message->Type = DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
	Message->Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Command.Driver = DSHM_IPC_MSG_CMD_DRIVER_PING;
	Message->TargetIndex = 0;
	Message->Size = sizeof(DSHM_IPC_MSG_HEADER); // no payload
}

VOID
FORCEINLINE
DSHM_IPC_MSG_PAIR_TO_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_PAIR_TO_REPLY Message,
	_In_ UINT32 DeviceIndex,
	_In_ NTSTATUS WriteStatus,
	_In_ NTSTATUS ReadStatus
)
{
	const UINT32 size = sizeof(DSHM_IPC_MSG_PAIR_TO_REPLY);
	RtlZeroMemory(Message, size);

	Message->Header.Type = DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
	Message->Header.Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->WriteStatus = WriteStatus;
	Message->ReadStatus = ReadStatus;
}

VOID
FORCEINLINE
DSHM_IPC_MSG_SET_PLAYER_INDEX_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY Message,
	_In_ UINT32 DeviceIndex,
	_In_ NTSTATUS Status
)
{
	const UINT32 size = sizeof(DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY);
	RtlZeroMemory(Message, size);

	Message->Header.Type = DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
	Message->Header.Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->NtStatus = Status;
}

VOID
FORCEINLINE
DSHM_IPC_MSG_GET_HID_WAIT_HANDLE_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_GET_HID_WAIT_HANDLE_RESPONSE Message,
	_In_ UINT32 DeviceIndex,
	_In_ DWORD ProcessId,
	_In_opt_ HANDLE WaitHandle
)
{
	const UINT32 size = sizeof(DSHM_IPC_MSG_GET_HID_WAIT_HANDLE_RESPONSE);
	RtlZeroMemory(Message, size);

	Message->Header.Type = DSHM_IPC_MSG_TYPE_RESPONSE_ONLY;
	Message->Header.Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_GET_HID_WAIT_HANDLE;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->ProcessId = ProcessId;
	Message->WaitHandle = WaitHandle;
}


NTSTATUS InitIPC(void);

void DestroyIPC(void);
