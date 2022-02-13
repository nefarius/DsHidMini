#include "Driver.h"
#include "Configuration.tmh"

#define CONFIG_ENVAR_NAME	"ProgramData"
#define CONFIG_FILE_NAME	"DsHidMini.json"

_Must_inspect_result_
NTSTATUS
ConfigLoadForDeviceAddress(
	_In_ PBD_ADDR Address
)
{
	NTSTATUS status = STATUS_UNSUCCESSFUL;
	CHAR programDataPath[MAX_PATH];
	CHAR configFilePath[MAX_PATH];

	do
	{
		if (GetEnvironmentVariableA(
			CONFIG_ENVAR_NAME,
			programDataPath,
			MAX_PATH
		) == 0)
		{
			status = STATUS_NOT_FOUND;
			break;
		}

		if (sprintf_s(
			configFilePath,
			MAX_PATH / sizeof(WCHAR),
			"%s\\" CONFIG_FILE_NAME,
			programDataPath
		) == -1)
		{
			status = STATUS_NOT_FOUND;
			break;
		}

		

	} while (FALSE);

	return status;
}
