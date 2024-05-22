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

//
// Abseil
// 
#include <absl/cleanup/cleanup.h>
#include <absl/strings/match.h>

//
// OpenTelemetry
// 
#if defined(SCPLIB_ENABLE_TELEMETRY)
#define HAVE_ABSEIL // fixes redefinitions of absl types
#include <opentelemetry/exporters/otlp/otlp_http_exporter_factory.h>
#include <opentelemetry/exporters/otlp/otlp_http_exporter_options.h>
#include <opentelemetry/sdk/trace/processor.h>
#include <opentelemetry/sdk/trace/simple_processor_factory.h>
#include <opentelemetry/sdk/trace/tracer_provider_factory.h>
#include <opentelemetry/sdk/resource/resource.h>
#include <opentelemetry/sdk/resource/semantic_conventions.h>
#include <opentelemetry/trace/provider.h>
#include <opentelemetry/sdk/trace/tracer_provider.h>

namespace trace			= opentelemetry::trace;
namespace nostd			= opentelemetry::nostd;
namespace sdktrace		= opentelemetry::sdk::trace;
namespace otlp			= opentelemetry::exporter::otlp;
namespace sdkresource	= opentelemetry::sdk::resource;

#include "opentelemetry/exporters/otlp/otlp_http_log_record_exporter_factory.h"
#include "opentelemetry/exporters/otlp/otlp_http_log_record_exporter_options.h"
#include "opentelemetry/logs/provider.h"
#include "opentelemetry/sdk/common/global_log_handler.h"
#include "opentelemetry/sdk/logs/logger_provider_factory.h"
#include "opentelemetry/sdk/logs/logger_provider.h"
#include "opentelemetry/sdk/logs/processor.h"
#include "opentelemetry/sdk/logs/simple_log_record_processor_factory.h"

namespace sdklogs		= opentelemetry::sdk::logs;
namespace logs			= opentelemetry::logs;
namespace internal_log	= opentelemetry::sdk::common::internal_log;
#endif

