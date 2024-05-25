#include "Driver.h"
#include "Driver.tmh"


/*WPP_INIT_TRACING(); (This comment is necessary for WPP Scanner.)*/
#pragma code_seg("INIT")
DMF_DEFAULT_DRIVERENTRY(DriverEntry,
	dshidminiEvtDriverContextCleanup,
	dshidminiEvtDeviceAdd,
	L"DsHidMini")
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
