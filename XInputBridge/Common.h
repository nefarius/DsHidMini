#pragma once

#define NOMINMAX
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <Windows.h>
#include <initguid.h>
#include <cfgmgr32.h>

//
// Driver-shared types
// 
#include <DsHidMini/ScpTypes.h>
#include <DsHidMini/Ds3Types.h>
#include <DsHidMini/dshmguid.h>

//
// STL
// 
#include <optional>
#include <vector>
#include <string>
#include <memory>
#include <thread>

//
// Logging
// 
#include <spdlog/logger.h>
#include <spdlog/spdlog.h>
