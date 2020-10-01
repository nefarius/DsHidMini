#pragma once

#define DS_DRIVER_CFG_FILE_PATH		"C:\\ProgramData\\DsHidMini.ini"
#define MAX_INSTANCE_ID_LENGTH		0xC8 // 200

VOID DsConfig_Load(PDEVICE_CONTEXT Context);
