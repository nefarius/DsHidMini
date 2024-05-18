#include "Common.h"
#include "GlobalState.h"

extern GlobalState G_State;


BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD ul_reason_for_call,
                      LPVOID lpReserved
)
{
	UNREFERENCED_PARAMETER(lpReserved);

	DisableThreadLibraryCalls(hModule);

	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		G_State.Initialize();
		break;
	case DLL_PROCESS_DETACH:
		G_State.Destroy();
		break;
	}
	return TRUE;
}
