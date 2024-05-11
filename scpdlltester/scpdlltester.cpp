#include <Windows.h>
#include "../include/DsHidMini/ScpTypes.h"
#include "../XInputBridge/XInputBridge.h"

int main()
{
	XINPUT_CAPABILITIES caps = {};

	while (true)
	{
		DWORD ret = XInputGetCapabilities(0, 0, &caps);
		Sleep(200);
	}
}
