#include "Driver.h"
#include "IPC.tmh"


void InitIPC()
{
	HANDLE hMapFile;
	LPCTSTR pBuf;
	HANDLE hEvent;

	// Create a named event for signaling
	hEvent = CreateEventA(NULL, FALSE, FALSE, DSHM_IPC_EVENT_NAME);
	if (hEvent == NULL)
	{
		hEvent = OpenEventA(EVENT_MODIFY_STATE | SYNCHRONIZE, FALSE, DSHM_IPC_EVENT_NAME);

		if (hEvent == NULL)
		{
			TraceError(
				TRACE_IPC,
				"Could not create or open event (%!WINERROR!).",
				GetLastError()
			);
			return;
		}
	}

	// Create a memory-mapped file
	hMapFile = CreateFileMappingA(
		INVALID_HANDLE_VALUE, // use paging file
		NULL, // default security
		PAGE_READWRITE, // read/write access
		0, // maximum object size (high-order DWORD)
		DSHM_IPC_BUFFER_SIZE, // maximum object size (low-order DWORD)
		DSHM_IPC_FILE_MAP_NAME); // name of mapping object

	if (hMapFile == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not create file mapping object (%!WINERROR!).",
			GetLastError()
		);
		return;
	}

	// Map a view of the file in the calling process's address space
	pBuf = (LPTSTR)MapViewOfFile(hMapFile, // handle to map object
		FILE_MAP_ALL_ACCESS, // read/write permission
		0,
		0,
		DSHM_IPC_BUFFER_SIZE);

	if (pBuf == NULL)
	{
		TraceError(
			TRACE_IPC,
			"Could not map view of file (%!WINERROR!).", GetLastError()
		);
		CloseHandle(hMapFile);
		return;
	}

#pragma warning(disable: 4996)
	// Write to the memory-mapped file
	sprintf((char*)pBuf, "Hello from the C program!");
	TraceInformation(
		TRACE_IPC,
		"IPC message success!"
	);
#pragma warning(default: 4996)

	// Signal the event to notify the other process
	SetEvent(hEvent);

	Sleep(200);

	// Clean up
	UnmapViewOfFile(pBuf);
	CloseHandle(hMapFile);
	CloseHandle(hEvent);
}
