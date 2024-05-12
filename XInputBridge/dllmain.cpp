#include "framework.h"

#if defined(SCPLIB_ENABLE_TELEMETRY)
otlp::OtlpHttpExporterOptions opts;
#endif

BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD ul_reason_for_call,
                      LPVOID lpReserved
)
{
    DisableThreadLibraryCalls(hModule);

    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
		ScpLibInitialize();
        hid_init();		

#if defined(SCPLIB_ENABLE_TELEMETRY)
        {
            auto exporter = otlp::OtlpHttpExporterFactory::Create(opts);
            auto processor = trace_sdk::SimpleSpanProcessorFactory::Create(std::move(exporter));
            const std::shared_ptr provider = trace_sdk::TracerProviderFactory::Create(std::move(processor));
            // Set the global trace provider
            trace::Provider::SetTracerProvider(provider);
        }
#endif
        break;
    case DLL_PROCESS_DETACH:
        hid_exit();
		ScpLibDestroy();
        break;
    }
    return TRUE;
}
