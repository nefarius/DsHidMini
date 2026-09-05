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
#include <array>
#include <format>

//
// Scope guard (replaces absl::Cleanup / absl::EqualsIgnoreCase)
//
#include "ScopeGuard.h"
