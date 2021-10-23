// dllmain.cpp : Defines the entry point for the DLL application.
#include "framework.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    DisableThreadLibraryCalls(hModule);

    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        hid_init();
        break;
    case DLL_PROCESS_DETACH:
        hid_exit();
        break;
    }
    return TRUE;
}

