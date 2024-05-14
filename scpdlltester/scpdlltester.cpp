#include <cstdio>
#include <iomanip>
#include <iostream>
#include <Windows.h>
#include "../include/DsHidMini/ScpTypes.h"
#include "../XInputBridge/XInputBridge.h"

int main()
{
	XINPUT_STATE state{};

	while (true)
	{
		DWORD ret = XInputGetState(0, &state);

		if (ret == ERROR_SUCCESS)
		{
			std::cout << "\r" << "Pad 0 connected, packet counter: " << std::setw(5) << state.dwPacketNumber;
		}
		else if (ret == ERROR_DEVICE_NOT_CONNECTED)
		{
			std::cout << "\r" << "Pad 0 not connected" << std::setfill(' ') << std::setw(30);
		}

		Sleep(20);
	}

#if 0
	XINPUT_CAPABILITIES caps = {};

	getchar();

	while (true)
	{
		DWORD ret = XInputGetCapabilities(0, 0, &caps);
		Sleep(200);
	}
#endif

	return ERROR_SUCCESS;
}
