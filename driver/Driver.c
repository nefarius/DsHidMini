#include "Driver.h"
#include "Driver.tmh"


static VOID CALLBACK
DsDriver_HotReloadEventCallback(
	_In_ PVOID lpParameter,
	_In_ BOOLEAN TimerOrWaitFired
);

static void
DsDriver_RegisterConfigWatcher(
	_In_ PDSHM_DRIVER_CONTEXT Context
);

static void
DsDriver_UnregisterConfigWatcher(
	_In_ PDSHM_DRIVER_CONTEXT Context
);

#pragma code_seg("INIT")
NTSTATUS
DriverEntry(
	_In_ PDRIVER_OBJECT DriverObject,
	_In_ PUNICODE_STRING RegistryPath
)
{
	WDF_DRIVER_CONFIG config;
	WDF_OBJECT_ATTRIBUTES attributes;

	//
	// Initialize WPP Tracing
	//
	WPP_INIT_TRACING(DriverObject, RegistryPath);

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DRIVER, "%!FUNC! Entry");

	//
	// Register a cleanup callback so that we can call WPP_CLEANUP when
	// the framework driver object is deleted during driver unload.
	//
	WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, DSHM_DRIVER_CONTEXT);
	attributes.EvtCleanupCallback = dshidminiEvtDriverContextCleanup;

	WDF_DRIVER_CONFIG_INIT(
		&config,
		dshidminiEvtDeviceAdd
	);

	WDFDRIVER driver = NULL;

	NTSTATUS status = WdfDriverCreate(
		DriverObject,
		RegistryPath,
		&attributes,
		&config,
		&driver
	);

	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DRIVER, "WdfDriverCreate failed with status %!STATUS!", status);
		WPP_CLEANUP(DriverObject);
		return status;
	}

	const PDSHM_DRIVER_CONTEXT context = DriverGetContext(driver);

	if (!NT_SUCCESS(status = WdfWaitLockCreate(WDF_NO_OBJECT_ATTRIBUTES, &context->SlotsLock)))
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DRIVER, "WdfWaitLockCreate failed with status %!STATUS!", status);
		WPP_CLEANUP(DriverObject);
		return status;
	}

	if (!NT_SUCCESS(status = WdfWaitLockCreate(WDF_NO_OBJECT_ATTRIBUTES, &context->IpcLock)))
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DRIVER, "WdfWaitLockCreate (IpcLock) failed with status %!STATUS!", status);
		WPP_CLEANUP(DriverObject);
		return status;
	}

	BOOLEAN ipcEnabled = TRUE;
	const NTSTATUS loadStatus = ConfigLoadIpcEnabled(&ipcEnabled);
	if (!NT_SUCCESS(loadStatus))
	{
		TraceEvents(
			TRACE_LEVEL_WARNING,
			TRACE_DRIVER,
			"ConfigLoadIpcEnabled failed with status %!STATUS!, defaulting IPC to enabled",
			loadStatus
		);
		ipcEnabled = TRUE;
	}

	if (!NT_SUCCESS(status = DSHM_IPC_Reconcile(ipcEnabled)))
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DRIVER, "DSHM_IPC_Reconcile failed with status %!STATUS!", status);
		WPP_CLEANUP(DriverObject);
		return status;
	}

	DsDriver_RegisterConfigWatcher(context);

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DRIVER, "%!FUNC! Exit");

	return status;
}
#pragma code_seg()

static VOID CALLBACK
DsDriver_HotReloadEventCallback(
	_In_ PVOID lpParameter,
	_In_ BOOLEAN TimerOrWaitFired
)
{
	UNREFERENCED_PARAMETER(TimerOrWaitFired);

	const PDSHM_DRIVER_CONTEXT context = (PDSHM_DRIVER_CONTEXT)lpParameter;
	BOOLEAN ipcEnabled = TRUE;

	FindNextChangeNotification(context->ConfigurationDirectoryWatcherEvent);

	Sleep(100);

	const NTSTATUS loadStatus = ConfigLoadIpcEnabled(&ipcEnabled);
	if (!NT_SUCCESS(loadStatus))
	{
		TraceEvents(
			TRACE_LEVEL_WARNING,
			TRACE_DRIVER,
			"IPCEnabled hot-reload failed with status %!STATUS!, keeping the previous IPC state",
			loadStatus
		);
		return;
	}

	const NTSTATUS reconcileStatus = DSHM_IPC_Reconcile(ipcEnabled);
	if (!NT_SUCCESS(reconcileStatus))
	{
		TraceEvents(
			TRACE_LEVEL_WARNING,
			TRACE_DRIVER,
			"DSHM_IPC_Reconcile failed with status %!STATUS! during hot-reload",
			reconcileStatus
		);
	}
}

static void
DsDriver_RegisterConfigWatcher(
	_In_ PDSHM_DRIVER_CONTEXT Context
)
{
	CHAR programDataPath[MAX_PATH];
	CHAR configPath[MAX_PATH];

	do
	{
		if (GetEnvironmentVariableA(
			CONFIG_ENV_VAR_NAME,
			programDataPath,
			MAX_PATH
		) == 0)
		{
			break;
		}

		if (sprintf_s(
			configPath,
			ARRAYSIZE(configPath),
			"%s\\%s",
			programDataPath,
			CONFIG_SUB_DIR_NAME
		) == -1)
		{
			break;
		}

		if (GetFileAttributesA(configPath) == INVALID_FILE_ATTRIBUTES)
		{
			if (!CreateDirectoryA(configPath, NULL))
			{
				const DWORD error = GetLastError();
				if (error != ERROR_ALREADY_EXISTS)
				{
					TraceEvents(
						TRACE_LEVEL_WARNING,
						TRACE_DRIVER,
						"CreateDirectoryA(%s) failed with error %!WINERROR!",
						configPath,
						error
					);
					break;
				}
			}
		}

		Context->ConfigurationDirectoryWatcherEvent = FindFirstChangeNotificationA(
			configPath,
			FALSE,
			FILE_NOTIFY_CHANGE_LAST_WRITE
		);

		if (Context->ConfigurationDirectoryWatcherEvent == NULL)
		{
			const DWORD error = GetLastError();
			TraceEvents(
				TRACE_LEVEL_ERROR,
				TRACE_DRIVER,
				"FindFirstChangeNotificationA failed with error %!WINERROR!",
				error
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"FindFirstChangeNotificationA", error);
			break;
		}

		const BOOL ret = RegisterWaitForSingleObject(
			&Context->ConfigurationDirectoryWatcherWaitHandle,
			Context->ConfigurationDirectoryWatcherEvent,
			DsDriver_HotReloadEventCallback,
			Context,
			INFINITE,
			WT_EXECUTELONGFUNCTION
		);

		if (!ret)
		{
			const DWORD error = GetLastError();
			TraceEvents(
				TRACE_LEVEL_ERROR,
				TRACE_DRIVER,
				"RegisterWaitForSingleObject failed with error %!WINERROR!",
				error
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"RegisterWaitForSingleObject", error);
			FindCloseChangeNotification(Context->ConfigurationDirectoryWatcherEvent);
			Context->ConfigurationDirectoryWatcherEvent = NULL;
		}
	} while (FALSE);
}

static void
DsDriver_UnregisterConfigWatcher(
	_In_ PDSHM_DRIVER_CONTEXT Context
)
{
	if (Context->ConfigurationDirectoryWatcherWaitHandle)
	{
		UnregisterWaitEx(Context->ConfigurationDirectoryWatcherWaitHandle, INVALID_HANDLE_VALUE);
		Context->ConfigurationDirectoryWatcherWaitHandle = NULL;
	}

	if (Context->ConfigurationDirectoryWatcherEvent)
	{
		FindCloseChangeNotification(Context->ConfigurationDirectoryWatcherEvent);
		Context->ConfigurationDirectoryWatcherEvent = NULL;
	}
}

#pragma code_seg("PAGED")
void dshidminiEvtDriverContextCleanup(WDFOBJECT DriverObject)
{
	const PDSHM_DRIVER_CONTEXT context = DriverGetContext(DriverObject);

	DsDriver_UnregisterConfigWatcher(context);
	DestroyIPC();

	WPP_CLEANUP(WdfDriverWdmGetDriverObject( (WDFDRIVER) DriverObject));
}
#pragma code_seg()

//
// DllMain initializes and disposes ETW
// 
BOOL APIENTRY DllMain(
	HMODULE hModule,
	DWORD ul_reason_for_call,
	LPVOID lpReserved
)
{
	UNREFERENCED_PARAMETER(lpReserved);

	DisableThreadLibraryCalls(hModule);

	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		EventRegisterNefarius_DsHidMini_Driver();
		break;
	case DLL_PROCESS_DETACH:
		EventUnregisterNefarius_DsHidMini_Driver();
		break;
	}

	return TRUE;
}
