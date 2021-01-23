/*++

Module Name:

    Internal.h

Abstract:

    This module contains the local type definitions for the
    driver.

Environment:

    Windows User-Mode Driver Framework 2

--*/

//
// Define the tracing flags.
//
// Tracing GUID - 4eefa829-ba06-44ff-906e-8082a387206f
//

#define WPP_CONTROL_GUIDS                                              \
    /* {F59F6F9B-A539-4846-9DE8-EFC2C43D0A91} */ \
    WPP_DEFINE_CONTROL_GUID( \
        DmfLibraryTraceGuid, (F59F6F9B,A539,4846,9DE8,EFC2C43D0A91), \
        WPP_DEFINE_BIT(DMF_TRACE) \
    ) \
                                                                       \
    WPP_DEFINE_CONTROL_GUID(                                           \
        DsHidMiniTraceGuid, (4eefa829,ba06,44ff,906e,8082a387206f),                  \
                                                                       \
        WPP_DEFINE_BIT(MYDRIVER_ALL_INFO)                              \
        WPP_DEFINE_BIT(TRACE_DRIVER)                                   \
        WPP_DEFINE_BIT(TRACE_DEVICE)                                   \
        WPP_DEFINE_BIT(TRACE_DSHID)                                    \
        WPP_DEFINE_BIT(TRACE_DSHIDMINIDRV)                             \
        WPP_DEFINE_BIT(TRACE_POWER)                                    \
        WPP_DEFINE_BIT(TRACE_DSUSB)                                    \
        WPP_DEFINE_BIT(TRACE_DS3)                                      \
        WPP_DEFINE_BIT(TRACE_DSBTH)                                    \
        WPP_DEFINE_BIT(TRACE_CONFIG)                                   \
        )                             


#define WPP_FLAG_LEVEL_LOGGER(flag, level)                              \
    WPP_LEVEL_LOGGER(flag)

#define WPP_FLAG_LEVEL_ENABLED(flag, level)                             \
    (WPP_LEVEL_ENABLED(flag) &&                                         \
     WPP_CONTROL(WPP_BIT_ ## flag).Level >= level)

#define WPP_LEVEL_FLAGS_LOGGER(lvl,flags) \
           WPP_LEVEL_LOGGER(flags)

#define WPP_LEVEL_FLAGS_ENABLED(lvl, flags) \
           (WPP_LEVEL_ENABLED(flags) && WPP_CONTROL(WPP_BIT_ ## flags).Level >= lvl)

//
// This comment block is scanned by the trace preprocessor to define our
// Trace function.
//
// USEPREFIX and USESUFFIX strip all trailing whitespace, so we need to surround
// FuncExit messages with brackets 
//
// begin_wpp config
// FUNC Trace{FLAG=MYDRIVER_ALL_INFO}(LEVEL, MSG, ...);
// FUNC TraceEvents(LEVEL, FLAGS, MSG, ...);
// FUNC FuncEntry{LEVEL=TRACE_LEVEL_VERBOSE}(FLAGS);
// FUNC FuncEntryArguments{LEVEL=TRACE_LEVEL_VERBOSE}(FLAGS, MSG, ...);
// FUNC FuncExit{LEVEL=TRACE_LEVEL_VERBOSE}(FLAGS, MSG, ...);
// FUNC FuncExitVoid{LEVEL=TRACE_LEVEL_VERBOSE}(FLAGS);
// FUNC TraceError{LEVEL=TRACE_LEVEL_ERROR}(FLAGS, MSG, ...);
// FUNC TraceInformation{LEVEL=TRACE_LEVEL_INFORMATION}(FLAGS, MSG, ...);
// FUNC TraceVerbose{LEVEL=TRACE_LEVEL_VERBOSE}(FLAGS, MSG, ...);
// FUNC FuncExitNoReturn{LEVEL=TRACE_LEVEL_VERBOSE}(FLAGS);
// FUNC TraceDbg{LEVEL=TRACE_LEVEL_INFORMATION}(FLAGS, MSG, ...);
// USEPREFIX(FuncEntry, "%!STDPREFIX! [%!FUNC!] --> Entry");
// USEPREFIX(FuncEntryArguments, "%!STDPREFIX! [%!FUNC!] --> Entry <");
// USEPREFIX(FuncExit, "%!STDPREFIX! [%!FUNC!] <-- Exit <");
// USESUFFIX(FuncExit, ">");
// USEPREFIX(FuncExitVoid, "%!STDPREFIX! [%!FUNC!] <-- Exit");
// USEPREFIX(TraceError, "%!STDPREFIX! [%!FUNC!] ERROR:");
// USEPREFIX(TraceEvents, "%!STDPREFIX! [%!FUNC!] ");
// USEPREFIX(TraceInformation, "%!STDPREFIX! [%!FUNC!] ");
// USEPREFIX(TraceVerbose, "%!STDPREFIX! [%!FUNC!] ");
// USEPREFIX(FuncExitNoReturn, "%!STDPREFIX! [%!FUNC!] <--");
// end_wpp

//
//
// Driver specific #defines
//
#if UMDF_VERSION_MAJOR == 2 && UMDF_VERSION_MINOR == 0
    #define MYDRIVER_TRACING_ID      L"Microsoft\\UMDF2.0\\dshidmini V1.0"
#endif