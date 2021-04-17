// XInputBridge.cpp : Defines the exported functions for the DLL.
//

#include "framework.h"
#include "XInputBridge.h"


// This is an example of an exported variable
XINPUTBRIDGE_API int nXInputBridge=0;

// This is an example of an exported function.
XINPUTBRIDGE_API int fnXInputBridge(void)
{
    return 0;
}

// This is the constructor of a class that has been exported.
CXInputBridge::CXInputBridge()
{
    return;
}
