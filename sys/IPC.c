#include "Driver.h"
#include "IPC.tmh"


static DWORD WINAPI ClientDispatchProc(
	_In_ LPVOID lpParameter
);

NTSTATUS InitIPC(void)
{
	FuncEntry(TRACE_IPC);

	const WDFDRIVER driver = WdfGetDriver();
	const PDSHM_DRIVER_CONTEXT context = DriverGetContext(driver);

	PUCHAR pBuf = NULL;
	HANDLE hReadEvent = NULL;
	HANDLE hWriteEvent = NULL;
	HANDLE hMapFile = NULL;
	HANDLE hMutex = NULL;
	HANDLE hThread = NULL;
	HANDLE hThreadTermination = NULL;

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

	// Create a named event for signaling
	hReadEvent = CreateEventA(&sa, FALSE, FALSE, DSHM_IPC_READ_EVENT_NAME);
	if (hReadEvent == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not create event (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	hWriteEvent = CreateEventA(&sa, FALSE, FALSE, DSHM_IPC_WRITE_EVENT_NAME);
	if (hWriteEvent == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not create event (%!WINERROR!).",
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
		&sa, // default security
		PAGE_READWRITE, // read/write access
		0, // maximum object size (high-order DWORD)
		DSHM_IPC_BUFFER_SIZE, // maximum object size (low-order DWORD)
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
	pBuf = MapViewOfFile(
		hMapFile, // handle to map object
		FILE_MAP_ALL_ACCESS, // read/write permission
		0,
		0,
		DSHM_IPC_BUFFER_SIZE
	);

	if (pBuf == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not map view of file (%!WINERROR!).",
			GetLastError()
		);
		goto exitFailure;
	}

	context->IPC.DispatchThreadTermination = hThreadTermination;
	context->IPC.MapFile = hMapFile;
	context->IPC.ConnectMutex = hMutex;
	context->IPC.ReadEvent = hReadEvent;
	context->IPC.WriteEvent = hWriteEvent;
	context->IPC.SharedMemory = pBuf;
	context->IPC.SharedMemorySize = DSHM_IPC_BUFFER_SIZE;

	// 
	// Start thread now that context is initialized at its minimum
	// 
	hThread = CreateThread(
		NULL,
		0,
		ClientDispatchProc,
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
	if (pBuf)
		UnmapViewOfFile(pBuf);

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

	const NTSTATUS status = NTSTATUS_FROM_WIN32(GetLastError());

	FuncExit(TRACE_IPC, "status=%!STATUS!", status);

	return status;
}

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

	if (context->IPC.SharedMemory)
		UnmapViewOfFile(context->IPC.SharedMemory);

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
// Listens for client connection and processes data exchange
// 
static DWORD WINAPI ClientDispatchProc(
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
			PDSHM_IPC_MSG_HEADER header = (PDSHM_IPC_MSG_HEADER)context->IPC.SharedMemory;

			TraceInformation(
				TRACE_IPC,
				"Got message type %d for target %d and command %d",
				header->Type, header->Target, header->Command.Device
			);

			// TODO: implement me!
		}

	} while (TRUE);

	FuncExitNoReturn(TRACE_IPC);

	return ERROR_SUCCESS;
}
