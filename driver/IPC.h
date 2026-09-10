#pragma once

#define DSHM_IPC_FILE_MAP_NAME		"Global\\DsHidMiniSharedMemory"
#define DSHM_IPC_MUTEX_NAME			"Global\\DsHidMiniCommandMutex"
#define DSHM_IPC_READ_EVENT_NAME	"Global\\DsHidMiniReadEvent"
#define DSHM_IPC_WRITE_EVENT_NAME	"Global\\DsHidMiniWriteEvent"
//
// Per-device manual-reset event: Global\DsHidMiniHidReportEvent{1-based slot index}
// Must stay in sync with SDK DsHidMiniInterop.HidReportWaitEventNamePrefix
// 
#define DSHM_IPC_HID_REPORT_EVENT_PREFIX	"Global\\DsHidMiniHidReportEvent"
#define DSHM_IPC_HID_REPORT_EVENT_NAME_CCH	64


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
	// Sends the console USB power-off sequence (zero output report + disable)
	// 
	DSHM_IPC_MSG_CMD_DEVICE_USB_POWER_OFF,
	//
	// Updates rumble motor strengths (volatile, not persisted)
	// 
	DSHM_IPC_MSG_CMD_DEVICE_SET_RUMBLE,
	//
	// Enables or disables alternative rumble mode at runtime
	// 
	DSHM_IPC_MSG_CMD_DEVICE_SET_ALTERNATE_RUMBLE_MODE,
	//
	// Pair a given device to the active local Bluetooth radio
	// 
	DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO_CURRENT_HOST,
	//
	// Disconnect a currently wireless device from the host radio
	// 
	DSHM_IPC_MSG_CMD_DEVICE_DISCONNECT_BLUETOOTH,
	//
	// Apply a full volatile LED pattern (flags + four effect blocks)
	// 
	DSHM_IPC_MSG_CMD_DEVICE_SET_LED_PATTERN,
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
// Requests the console-style USB power-off sequence (issue #366)
// 
typedef struct _DSHM_IPC_MSG_USB_POWER_OFF_REQUEST
{
	DSHM_IPC_MSG_HEADER Header;

} DSHM_IPC_MSG_USB_POWER_OFF_REQUEST, *PDSHM_IPC_MSG_USB_POWER_OFF_REQUEST;

//
// Reply to struct _DSHM_IPC_MSG_USB_POWER_OFF_REQUEST
// 
typedef struct _DSHM_IPC_MSG_USB_POWER_OFF_REPLY
{
	DSHM_IPC_MSG_HEADER Header;

	//
	// NTSTATUS of the 48-byte zero output report (LEDs/rumble off)
	// 
	NTSTATUS IndicatorsOffStatus;

	//
	// NTSTATUS of the Feature 0xF4 disable transfer
	// 
	NTSTATUS ShutdownStatus;

} DSHM_IPC_MSG_USB_POWER_OFF_REPLY, *PDSHM_IPC_MSG_USB_POWER_OFF_REPLY;

//
// Updates rumble motor strengths on a given device
// 
typedef struct _DSHM_IPC_MSG_SET_RUMBLE_REQUEST
{
	DSHM_IPC_MSG_HEADER Header;

	//
	// Heavy / left motor strength (0-255)
	// 
	UCHAR LargeMotor;

	//
	// Light / right motor strength (0-255)
	// 
	UCHAR SmallMotor;

} DSHM_IPC_MSG_SET_RUMBLE_REQUEST, *PDSHM_IPC_MSG_SET_RUMBLE_REQUEST;

//
// Reply to struct _DSHM_IPC_MSG_SET_RUMBLE_REQUEST
// 
typedef struct _DSHM_IPC_MSG_SET_RUMBLE_REPLY
{
	DSHM_IPC_MSG_HEADER Header;

	NTSTATUS NtStatus;

} DSHM_IPC_MSG_SET_RUMBLE_REPLY, *PDSHM_IPC_MSG_SET_RUMBLE_REPLY;

//
// Toggles alternative rumble mode for a given device (volatile)
// 
typedef struct _DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST
{
	DSHM_IPC_MSG_HEADER Header;

	//
	// TRUE enables alternative rumble mode; FALSE restores normal processing
	// 
	BOOLEAN IsEnabled;

} DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST, *PDSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST;

//
// Reply to struct _DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST
// 
typedef struct _DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY
{
	DSHM_IPC_MSG_HEADER Header;

	NTSTATUS NtStatus;

} DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY, *PDSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY;

//
// One DS3 LED effect block for IPC. Explicit reserved byte keeps the
// USHORT naturally aligned so C and C# Sequential layouts stay 6 bytes.
// 
typedef struct _DSHM_IPC_LED_EFFECT
{
	UCHAR TotalDuration;
	UCHAR Reserved;
	USHORT BasePortionDuration;
	UCHAR OffPortionMultiplier;
	UCHAR OnPortionMultiplier;

} DSHM_IPC_LED_EFFECT, *PDSHM_IPC_LED_EFFECT;

//
// Pair a given device to the active local Bluetooth radio (no address payload)
// 
typedef struct _DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REQUEST
{
	DSHM_IPC_MSG_HEADER Header;

} DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REQUEST, *PDSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REQUEST;

//
// Reply to struct _DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REQUEST
// 
typedef struct _DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REPLY
{
	DSHM_IPC_MSG_HEADER Header;

	NTSTATUS WriteStatus;

	NTSTATUS ReadStatus;

} DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REPLY, *PDSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REPLY;

//
// Disconnect a currently wireless device from the host radio
// 
typedef struct _DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REQUEST
{
	DSHM_IPC_MSG_HEADER Header;

} DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REQUEST, *PDSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REQUEST;

//
// Reply to struct _DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REQUEST
// 
typedef struct _DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REPLY
{
	DSHM_IPC_MSG_HEADER Header;

	NTSTATUS NtStatus;

} DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REPLY, *PDSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REPLY;

//
// Apply a full volatile LED pattern (flags + four independent effects)
// 
typedef struct _DSHM_IPC_MSG_SET_LED_PATTERN_REQUEST
{
	DSHM_IPC_MSG_HEADER Header;

	//
	// DS3 LED flags byte (DS3_LED_1..4 and/or DS3_LED_OFF). Reserved bits
	// are rejected by the driver.
	// 
	UCHAR Flags;

	//
	// Pad so Player1 starts on a 4-byte boundary (header is 20 bytes).
	// 
	UCHAR Reserved[3];

	DSHM_IPC_LED_EFFECT Player1;
	DSHM_IPC_LED_EFFECT Player2;
	DSHM_IPC_LED_EFFECT Player3;
	DSHM_IPC_LED_EFFECT Player4;

} DSHM_IPC_MSG_SET_LED_PATTERN_REQUEST, *PDSHM_IPC_MSG_SET_LED_PATTERN_REQUEST;

//
// Reply to struct _DSHM_IPC_MSG_SET_LED_PATTERN_REQUEST
// 
typedef struct _DSHM_IPC_MSG_SET_LED_PATTERN_REPLY
{
	DSHM_IPC_MSG_HEADER Header;

	NTSTATUS NtStatus;

} DSHM_IPC_MSG_SET_LED_PATTERN_REPLY, *PDSHM_IPC_MSG_SET_LED_PATTERN_REPLY;

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
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->NtStatus = Status;
}

VOID
FORCEINLINE
DSHM_IPC_MSG_USB_POWER_OFF_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_USB_POWER_OFF_REPLY Message,
	_In_ UINT32 DeviceIndex,
	_In_ NTSTATUS IndicatorsOffStatus,
	_In_ NTSTATUS ShutdownStatus
)
{
	const UINT32 size = sizeof(DSHM_IPC_MSG_USB_POWER_OFF_REPLY);
	RtlZeroMemory(Message, size);

	Message->Header.Type = DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
	Message->Header.Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_USB_POWER_OFF;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->IndicatorsOffStatus = IndicatorsOffStatus;
	Message->ShutdownStatus = ShutdownStatus;
}

VOID
FORCEINLINE
DSHM_IPC_MSG_SET_RUMBLE_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_SET_RUMBLE_REPLY Message,
	_In_ UINT32 DeviceIndex,
	_In_ NTSTATUS Status
)
{
	const UINT32 size = sizeof(DSHM_IPC_MSG_SET_RUMBLE_REPLY);
	RtlZeroMemory(Message, size);

	Message->Header.Type = DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
	Message->Header.Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_SET_RUMBLE;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->NtStatus = Status;
}

VOID
FORCEINLINE
DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY Message,
	_In_ UINT32 DeviceIndex,
	_In_ NTSTATUS Status
)
{
	const UINT32 size = sizeof(DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY);
	RtlZeroMemory(Message, size);

	Message->Header.Type = DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
	Message->Header.Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_SET_ALTERNATE_RUMBLE_MODE;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->NtStatus = Status;
}

VOID
FORCEINLINE
DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REPLY Message,
	_In_ UINT32 DeviceIndex,
	_In_ NTSTATUS WriteStatus,
	_In_ NTSTATUS ReadStatus
)
{
	const UINT32 size = sizeof(DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REPLY);
	RtlZeroMemory(Message, size);

	Message->Header.Type = DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
	Message->Header.Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO_CURRENT_HOST;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->WriteStatus = WriteStatus;
	Message->ReadStatus = ReadStatus;
}

VOID
FORCEINLINE
DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REPLY Message,
	_In_ UINT32 DeviceIndex,
	_In_ NTSTATUS Status
)
{
	const UINT32 size = sizeof(DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REPLY);
	RtlZeroMemory(Message, size);

	Message->Header.Type = DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
	Message->Header.Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_DISCONNECT_BLUETOOTH;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->NtStatus = Status;
}

VOID
FORCEINLINE
DSHM_IPC_MSG_SET_LED_PATTERN_RESPONSE_INIT(
	_Inout_ PDSHM_IPC_MSG_SET_LED_PATTERN_REPLY Message,
	_In_ UINT32 DeviceIndex,
	_In_ NTSTATUS Status
)
{
	const UINT32 size = sizeof(DSHM_IPC_MSG_SET_LED_PATTERN_REPLY);
	RtlZeroMemory(Message, size);

	Message->Header.Type = DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
	Message->Header.Target = DSHM_IPC_MSG_TARGET_CLIENT;
	Message->Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE_SET_LED_PATTERN;
	Message->Header.TargetIndex = DeviceIndex;
	Message->Header.Size = size;

	Message->NtStatus = Status;
}


NTSTATUS DSHM_IPC_Reconcile(
	_In_ BOOLEAN Enabled
);

void DestroyIPC(void);
