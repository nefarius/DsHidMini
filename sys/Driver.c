/*++

Module Name:

	driver.c

Abstract:

	This file contains the driver entry points and callbacks.

Environment:

	User-mode Driver Framework 2

--*/

#include "Driver.h"
#include "Driver.tmh"

unsigned int numInstances = 0;


/*WPP_INIT_TRACING(); (This comment is necessary for WPP Scanner.)*/
#pragma code_seg("INIT")
DMF_DEFAULT_DRIVERENTRY(DriverEntry,
	dshidminiEvtDriverContextCleanup,
	dshidminiEvtDeviceAdd,
	L"DsHidMini")
#pragma code_seg()

#if DBG
LONG WINAPI DSHM_InternalExceptionHandler(struct _EXCEPTION_POINTERS* apExceptionInfo)
{
	const HMODULE mhLib = LoadLibraryA("dbghelp.dll");
	const MINIDUMPWRITEDUMP pDump = (MINIDUMPWRITEDUMP)(GetProcAddress(mhLib, "MiniDumpWriteDump"));

	const HANDLE hFile = CreateFileA(
		"C:\\ProgramData\\DsHidMini\\DsHidMini.dmp", // TODO: improve static path!
		GENERIC_WRITE,
		FILE_SHARE_WRITE,
		NULL,
		CREATE_ALWAYS,
		FILE_ATTRIBUTE_NORMAL,
		NULL
	);

	const DWORD flags = MiniDumpWithFullMemory | MiniDumpWithHandleData | MiniDumpWithUnloadedModules |
		MiniDumpWithUnloadedModules | MiniDumpWithProcessThreadData |
		MiniDumpWithFullMemoryInfo | MiniDumpWithThreadInfo |
		MiniDumpWithFullAuxiliaryState | MiniDumpIgnoreInaccessibleMemory |
		MiniDumpWithTokenInformation;

	if (hFile != INVALID_HANDLE_VALUE)
	{
		MINIDUMP_EXCEPTION_INFORMATION ExInfo;
		ExInfo.ThreadId = GetCurrentThreadId();
		ExInfo.ExceptionPointers = apExceptionInfo;
		ExInfo.ClientPointers = FALSE;

		pDump(
			GetCurrentProcess(),
			GetCurrentProcessId(),
			hFile,
			(MINIDUMP_TYPE)flags,
			&ExInfo,
			NULL,
			NULL
		);
		CloseHandle(hFile);
	}

	return EXCEPTION_CONTINUE_SEARCH;
}
#endif
