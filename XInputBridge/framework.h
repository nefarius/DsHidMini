// header.h : include file for standard system include files,
// or project specific include files
//

#pragma once

#define NOMINMAX
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <Windows.h>

#include <DsHidMini/ScpTypes.h>
#include <DsHidMini/Ds3Types.h>
#include <hidapi/hidapi.h>

//
// OpenTelemetry
// 

#include "opentelemetry/exporters/jaeger/jaeger_exporter_factory.h"
#include "opentelemetry/sdk/trace/simple_processor_factory.h"
#include "opentelemetry/sdk/trace/tracer_provider_factory.h"
#include "opentelemetry/trace/provider.h"

namespace trace     = opentelemetry::trace;
namespace nostd     = opentelemetry::nostd;
namespace trace_sdk = opentelemetry::sdk::trace;
namespace jaeger    = opentelemetry::exporter::jaeger;
