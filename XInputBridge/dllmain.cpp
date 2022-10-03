// dllmain.cpp : Defines the entry point for the DLL application.
#include "framework.h"

opentelemetry::exporter::jaeger::JaegerExporterOptions opts;

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	DisableThreadLibraryCalls(hModule);

	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		hid_init();

		{
			auto exporter = jaeger::JaegerExporterFactory::Create(opts);
			auto processor = trace_sdk::SimpleSpanProcessorFactory::Create(std::move(exporter));
			std::shared_ptr<opentelemetry::trace::TracerProvider> provider =
				trace_sdk::TracerProviderFactory::Create(std::move(processor));
			// Set the global trace provider
			trace::Provider::SetTracerProvider(provider);
		}
		break;
	case DLL_PROCESS_DETACH:
		hid_exit();
		break;
	}
	return TRUE;
}

