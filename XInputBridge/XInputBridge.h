// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the XINPUTBRIDGE_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// XINPUTBRIDGE_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef XINPUTBRIDGE_EXPORTS
#define XINPUTBRIDGE_API __declspec(dllexport)
#else
#define XINPUTBRIDGE_API __declspec(dllimport)
#endif

// This class is exported from the dll
class XINPUTBRIDGE_API CXInputBridge {
public:
	CXInputBridge(void);
	// TODO: add your methods here.
};

extern XINPUTBRIDGE_API int nXInputBridge;

XINPUTBRIDGE_API int fnXInputBridge(void);
