#include "Common.h"
#include "GlobalState.h"

extern GlobalState G_State;


BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD ul_reason_for_call,
                      LPVOID lpReserved
)
{
	UNREFERENCED_PARAMETER(lpReserved);

	DisableThreadLibraryCalls(hModule);

	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		G_State.Initialize();

#if defined(SCPLIB_ENABLE_TELEMETRY)
		{
			const auto resourceAttributes = sdkresource::ResourceAttributes{
				{ opentelemetry::sdk::resource::SemanticConventions::kServiceName, TRACER_NAME }
			};

			auto resource = sdkresource::Resource::Create(resourceAttributes);
			auto exporter = otlp::OtlpHttpExporterFactory::Create();
			auto processor = sdktrace::SimpleSpanProcessorFactory::Create(std::move(exporter));
			const std::shared_ptr provider = sdktrace::TracerProviderFactory::Create(std::move(processor), std::move(resource));

			// Set the global trace provider
			trace::Provider::SetTracerProvider(provider);
		}
#endif
		break;
	case DLL_PROCESS_DETACH:
		G_State.Destroy();
		break;
	}
	return TRUE;
}
