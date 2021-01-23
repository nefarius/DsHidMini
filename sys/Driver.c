/*++

Module Name:

    driver.c

Abstract:

    This file contains the driver entry points and callbacks.

Environment:

    User-mode Driver Framework 2

--*/

#include "driver.h"
#include "driver.tmh"

unsigned int numInstances = 0;


//
// Bootstrap driver
// 
NTSTATUS
DriverEntry(
    _In_ PDRIVER_OBJECT  DriverObject,
    _In_ PUNICODE_STRING RegistryPath
    )
{
    WDF_DRIVER_CONFIG config;
    NTSTATUS status;
    WDF_OBJECT_ATTRIBUTES attributes;

    //
    // Initialize WPP Tracing
    //
#if UMDF_VERSION_MAJOR == 2 && UMDF_VERSION_MINOR == 0
    WPP_INIT_TRACING(MYDRIVER_TRACING_ID);
#else
    WPP_INIT_TRACING( DriverObject, RegistryPath );
#endif

    TraceInformation(TRACE_DRIVER, "%!FUNC! Entry");

    //
    // Register a cleanup callback so that we can call WPP_CLEANUP when
    // the framework driver object is deleted during driver unload.
    //
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.EvtCleanupCallback = dshidminiEvtDriverContextCleanup;

    WDF_DRIVER_CONFIG_INIT(&config,
                           dshidminiEvtDeviceAdd
                           );

    status = WdfDriverCreate(DriverObject,
                             RegistryPath,
                             &attributes,
                             &config,
                             WDF_NO_HANDLE
                             );

    if (!NT_SUCCESS(status)) {
        TraceError( TRACE_DRIVER, "WdfDriverCreate failed %!STATUS!", status);
#if UMDF_VERSION_MAJOR == 2 && UMDF_VERSION_MINOR == 0
        WPP_CLEANUP();
#else
        WPP_CLEANUP(DriverObject);
#endif
        return status;
    }

    TraceInformation(TRACE_DRIVER, "%!FUNC! Exit");

    return status;
}

NTSTATUS
dshidminiEvtDeviceAdd(
    _In_    WDFDRIVER       Driver,
    _Inout_ PWDFDEVICE_INIT DeviceInit
    )
{
    NTSTATUS status;

    UNREFERENCED_PARAMETER(Driver);

    TraceInformation(TRACE_DRIVER, "%!FUNC! Entry");

    status = dshidminiCreateDevice(DeviceInit);

    TraceInformation(TRACE_DRIVER, "%!FUNC! Exit");

    return status;
}

VOID
dshidminiEvtDriverContextCleanup(
    _In_ WDFOBJECT DriverObject
    )
{
    UNREFERENCED_PARAMETER(DriverObject);

    TraceInformation(TRACE_DRIVER, "%!FUNC! Entry");

    //
    // Stop WPP Tracing
    //
#if UMDF_VERSION_MAJOR == 2 && UMDF_VERSION_MINOR == 0
    WPP_CLEANUP();
#else
    WPP_CLEANUP(WdfDriverWdmGetDriverObject((WDFDRIVER)DriverObject));
#endif
}
