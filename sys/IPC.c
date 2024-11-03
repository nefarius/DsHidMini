#include "Driver.h"
#include "IPC.tmh"


static DWORD WINAPI DSHM_IPC_ClientDispatchProc(
	_In_ LPVOID lpParameter
);

//
// Sets up direct driver process IPC for sideband communication
// 
NTSTATUS InitIPC(void)
{
	FuncEntry(TRACE_IPC);

	DECLARE_CONST_UNICODE_STRING(valNameIPCEnaabled, L"IPCEnabled");

	const WDFDRIVER driver = WdfGetDriver();
	const PDSHM_DRIVER_CONTEXT context = DriverGetContext(driver);
	WDFKEY hKeyParameters = NULL;
	NTSTATUS status;

	PUCHAR pCmdBuf = NULL;
	PUCHAR pHIDBuf = NULL;
	HANDLE hReadEvent = NULL;
	HANDLE hWriteEvent = NULL;
	HANDLE hMapFile = NULL;
	HANDLE hMutex = NULL;
	HANDLE hThread = NULL;
	HANDLE hThreadTermination = NULL;

	if (!NT_SUCCESS(status = WdfDriverOpenParametersRegistryKey(
		driver,
		KEY_READ,
		WDF_NO_OBJECT_ATTRIBUTES,
		&hKeyParameters
	)))
	{
		TraceError(
			TRACE_IPC,
			"WdfDriverOpenParametersRegistryKey failed with status %!STATUS!",
			status
		);
		goto exitFailure;
	}

	if (!NT_SUCCESS(status = WdfRegistryQueryULong(
		hKeyParameters,
		&valNameIPCEnaabled,
		&context->IPC.IsEnabled
	)))
	{
		TraceError(
			TRACE_IPC,
			"WdfRegistryQueryULong failed with status %!STATUS!",
			status
		);
		goto exitFailure;
	}

	//
	// Feature disabled in registry
	// 
	if (!context->IPC.IsEnabled)
	{
		TraceInformation(
			TRACE_IPC,
			"IPC feature disabled, aborting initialization"
		);
		status = STATUS_DEVICE_FEATURE_NOT_SUPPORTED;
		goto exitFailure;
	}

	SYSTEM_INFO sysInfo;
    GetSystemInfo(&sysInfo);
    DWORD pageSize = sysInfo.dwAllocationGranularity;

	DWORD cmdRegionSize = pageSize;
	DWORD hidRegionSize = pageSize;
	DWORD totalRegionSize = cmdRegionSize + hidRegionSize;

	TraceVerbose(
		TRACE_IPC,
		"pageSize = %d, cmdRegionSize = %d, hidRegionSize = %d, totalRegionSize = %d",
		pageSize, cmdRegionSize, hidRegionSize, totalRegionSize
	);	

	SECURITY_DESCRIPTOR sd = { 0 };

	if (!InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION))
	{
		TraceError(
			TRACE_IPC,
			"InitializeSecurityDescriptor failed with error: %!WINERROR!",
			GetLastError()
		);
		goto exitFailure;
	}

	SECURITY_ATTRIBUTES sa = { 0 };
	sa.nLength = sizeof(sa);
	sa.bInheritHandle = TRUE;
	sa.lpSecurityDescriptor = &sd;

	CHAR* szSD = "D:" // Discretionary ACL
	"(D;OICI;GA;;;BG)" // Deny access to Built-in Guests
	"(D;OICI;GA;;;AN)" // Deny access to Anonymous Logon
	"(A;OICI;GRGWGX;;;AU)" // Allow read/write/execute to Authenticated Users
	"(A;OICI;GA;;;BA)"; // Allow full control to Administrators

	if (!ConvertStringSecurityDescriptorToSecurityDescriptorA(
		szSD,
		SDDL_REVISION_1,
		&sa.lpSecurityDescriptor,
		NULL
	))
	{
		TraceError(
			TRACE_IPC,
			"ConvertStringSecurityDescriptorToSecurityDescriptor failed with error: %!WINERROR!",
			GetLastError()
		);
		goto exitFailure;
	}

	hMutex = CreateMutexA(&sa, FALSE, DSHM_IPC_MUTEX_NAME);
	if (hMutex == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not create mutex (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	hReadEvent = CreateEventA(&sa, FALSE, FALSE, DSHM_IPC_READ_EVENT_NAME);
	if (hReadEvent == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not create READ event (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	hWriteEvent = CreateEventA(&sa, FALSE, FALSE, DSHM_IPC_WRITE_EVENT_NAME);
	if (hWriteEvent == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not create WRITE event (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	hThreadTermination = CreateEventA(&sa, FALSE, FALSE, NULL);
	if (hThreadTermination == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not create event (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	// Create a memory-mapped file
	hMapFile = CreateFileMappingA(
		INVALID_HANDLE_VALUE, // use paging file
		&sa, // custom security
		PAGE_READWRITE, // read/write access
		0, // maximum object size (high-order DWORD)
		totalRegionSize, // maximum object size (low-order DWORD)
		DSHM_IPC_FILE_MAP_NAME // name of mapping object
	);

	if (hMapFile == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not create file mapping object (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	// Map a view of the file in the calling process's address space
	pCmdBuf = MapViewOfFile(
		hMapFile, // handle to map object
		FILE_MAP_ALL_ACCESS, // read/write permission
		0,
		0,
		cmdRegionSize
	);

	if (pCmdBuf == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not map view of file CMD REGION (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	// Calculate the nearest page-aligned offset for the HID region
    DWORD alignedOffset = (cmdRegionSize / pageSize) * pageSize; // Page-aligned offset
    DWORD offsetWithinPage = cmdRegionSize % pageSize;           // Offset within the mapped page

	pHIDBuf = MapViewOfFile(
		hMapFile, // handle to map object
		FILE_MAP_ALL_ACCESS, // read/write permission
		0,
		alignedOffset,
		hidRegionSize + offsetWithinPage
	);

	if (pHIDBuf == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not map view of file HID REGION (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	context->IPC.DispatchThreadTermination = hThreadTermination;
	context->IPC.MapFile = hMapFile;
	context->IPC.ConnectMutex = hMutex;
	context->IPC.ReadEvent = hReadEvent;
	context->IPC.WriteEvent = hWriteEvent;

	context->IPC.SharedRegions.Commands.Buffer = pCmdBuf;
	context->IPC.SharedRegions.Commands.BufferSize = cmdRegionSize;

	context->IPC.SharedRegions.HID.Buffer = pHIDBuf;
	context->IPC.SharedRegions.HID.BufferSize = hidRegionSize;

	// 
	// Start thread now that context is initialized at its minimum requirement
	// 
	hThread = CreateThread(
		NULL,
		0,
		DSHM_IPC_ClientDispatchProc,
		context,
		0,
		NULL
	);

	if (hThread == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not create dispatch thread (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	context->IPC.DispatchThread = hThread;

	FuncExitNoReturn(TRACE_IPC);

	return STATUS_SUCCESS;

exitFailure:
	if (pCmdBuf)
		UnmapViewOfFile(pCmdBuf);

	if (pHIDBuf)
		UnmapViewOfFile(pHIDBuf);

	if (hReadEvent)
		CloseHandle(hReadEvent);

	if (hWriteEvent)
		CloseHandle(hWriteEvent);

	if (hMapFile)
		CloseHandle(hMapFile);

	if (hMutex)
		CloseHandle(hMutex);

	if (hThread)
		CloseHandle(hThread);

	if (hThreadTermination)
		CloseHandle(hThreadTermination);

	status = NT_SUCCESS(status) ? status : NTSTATUS_FROM_WIN32(GetLastError());

	FuncExit(TRACE_IPC, "status=%!STATUS!", status);

	return status;
}

//
// Frees IPC resources
// 
void DestroyIPC(void)
{
	FuncEntry(TRACE_IPC);

	const WDFDRIVER driver = WdfGetDriver();
	const PDSHM_DRIVER_CONTEXT context = DriverGetContext(driver);

	//
	// Thread running; signal termination, wait on exit, free resources
	// 
	if (context->IPC.DispatchThread && context->IPC.DispatchThreadTermination)
	{
		SetEvent(context->IPC.DispatchThreadTermination);
		WaitForSingleObject(context->IPC.DispatchThread, 500);
		CloseHandle(context->IPC.DispatchThread);
		CloseHandle(context->IPC.DispatchThreadTermination);
	}

	if (context->IPC.SharedRegions.Commands.Buffer)
		UnmapViewOfFile(context->IPC.SharedRegions.Commands.Buffer);

	if (context->IPC.SharedRegions.HID.Buffer)
		UnmapViewOfFile(context->IPC.SharedRegions.HID.Buffer);

	if (context->IPC.MapFile)
		CloseHandle(context->IPC.MapFile);

	if (context->IPC.ReadEvent)
		CloseHandle(context->IPC.ReadEvent);

	if (context->IPC.WriteEvent)
		CloseHandle(context->IPC.WriteEvent);

	if (context->IPC.ConnectMutex)
		CloseHandle(context->IPC.ConnectMutex);

	FuncExitNoReturn(TRACE_IPC);
}

//
// Processes incoming IPC commands
// 
static NTSTATUS DSHM_IPC_DispatchIncomingCommandMessage(
	_In_ const PDSHM_DRIVER_CONTEXT Context,
	_In_ const PDSHM_IPC_MSG_HEADER Message
)
{
	FuncEntry(TRACE_IPC);

	NTSTATUS status = STATUS_NOT_IMPLEMENTED;

	//
	// Sanity check
	// 
	if (Message->Size < sizeof(DSHM_IPC_MSG_HEADER))
	{
		return STATUS_INVALID_USER_BUFFER;
	}

	//
	// Message outside of region bounds
	// 
	if (Message->Size > Context->IPC.SharedRegions.Commands.BufferSize)
	{
		return STATUS_BUFFER_OVERFLOW;
	}

	//
	// Incoming PING message
	// 
	if (DSHM_IPC_MSG_IS_PING(Message))
	{
		TraceVerbose(
			TRACE_IPC,
			"IPC: PING message received"
		);

		const PDSHM_IPC_MSG_HEADER header = (PDSHM_IPC_MSG_HEADER)Context->IPC.SharedRegions.Commands.Buffer;

		DSHM_IPC_MSG_PING_RESPONSE_INIT(header);

		DSHM_IPC_SIGNAL_WRITE_DONE(Context);

		status = STATUS_SUCCESS;
	}

	//
	// Message is for a device instance
	// 
	if (DSHM_IPC_MSG_IS_FOR_DEVICE(Message))
	{
		const BOOLEAN expectsReply = DSHM_IPC_MSG_EXPECTS_REPLY(Message);
		const PDEVICE_CONTEXT deviceContext = Context->IPC.DeviceDispatchers.Contexts[Message->TargetIndex];
		const PFN_DSHM_IPC_DispatchDeviceMessage callback = Context->IPC.DeviceDispatchers.Callbacks[Message->TargetIndex];

		//
		// Proxy the message dispatching to the targeted device instance
		// 
		if (callback && deviceContext)
		{
			status = callback(deviceContext, Message);
		}
		else
		{
			TraceWarning(
				TRACE_IPC,
				"Device with index %d has no valid callback or context assigned",
				Message->TargetIndex
			);
		}

		if (expectsReply)
		{
			DSHM_IPC_SIGNAL_WRITE_DONE(Context);
		}
	}

	FuncExit(TRACE_IPC, "status=%!STATUS!", status);

	return status;
}

//
// Listens for client connection and processes data exchange
// 
static DWORD WINAPI DSHM_IPC_ClientDispatchProc(
	_In_ LPVOID lpParameter
)
{
	FuncEntry(TRACE_IPC);

	const PDSHM_DRIVER_CONTEXT context = lpParameter;

	const HANDLE waits[] = {
		// driver shutdown signals thread termination
		context->IPC.DispatchThreadTermination,
		// read is signaled when an outside app has finished writing
		context->IPC.ReadEvent
	};

	do
	{
		DWORD waitResult = WaitForMultipleObjects(ARRAYSIZE(waits), waits, FALSE, 1000);

		//
		// Retry indefinitely until an event is signaled
		// 
		if (waitResult == WAIT_TIMEOUT)
			continue;

		//
		// Unexpected result
		// 
		if (waitResult == WAIT_FAILED)
		{
			TraceError(
				TRACE_IPC,
				"Wait failed with error %!WINERROR!",
				GetLastError()
			);
			break;
		}

		//
		// Thread termination signaled, exiting
		// 
		if (waitResult == WAIT_OBJECT_0)
		{
			break;
		}

		//
		// New data is available in the shared region
		// 
		if (waitResult == WAIT_OBJECT_0 + 1)
		{
			//
			// Each valid message is expected to be prefixed with this header
			// 
			const PDSHM_IPC_MSG_HEADER header = (PDSHM_IPC_MSG_HEADER)context->IPC.SharedRegions.Commands.Buffer;

			TraceInformation(
				TRACE_IPC,
				"Got message type %d for target %d and command %d",
				header->Type, header->Target, header->Command.Device
			);

			NTSTATUS status = DSHM_IPC_DispatchIncomingCommandMessage(context, header);

			if (!NT_SUCCESS(status))
			{
				TraceError(
					TRACE_IPC,
					"DSHM_IPC_DispatchIncomingCommandMessage reported non-success status %!STATUS!",
					status
				);

				// TODO: can we do anything else with a failure status?
			}
		}

	} while (TRUE);

	FuncExitNoReturn(TRACE_IPC);

	return ERROR_SUCCESS;
}
