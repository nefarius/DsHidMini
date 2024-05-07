// header.h : include file for standard system include files,
// or project specific include files
//

#pragma once

/*
 * Uncomment to enable building with OpenTelemetry to allow recording performance traces
 * The library size will get inflated quite a bit as it gets statically linked in
 */
//#define SCPLIB_ENABLE_TELEMETRY

#define _SILENCE_ALL_CXX17_DEPRECATION_WARNINGS
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

#if defined(SCPLIB_ENABLE_TELEMETRY)
#include <opentelemetry/exporters/otlp/otlp_http_exporter_factory.h>
#include <opentelemetry/exporters/otlp/otlp_http_exporter_options.h>
#include <opentelemetry/sdk/trace/processor.h>
#include <opentelemetry/sdk/trace/simple_processor_factory.h>
#include <opentelemetry/sdk/trace/tracer_provider_factory.h>
#include <opentelemetry/trace/provider.h>

namespace trace     = opentelemetry::trace;
namespace nostd     = opentelemetry::nostd;
namespace trace_sdk = opentelemetry::sdk::trace;
namespace otlp      = opentelemetry::exporter::otlp;
#endif
