#pragma once

#define DS_DRIVER_CFG_FILE_PATH		"C:\\ProgramData\\DsHidMini.cfg"
#define MAX_INSTANCE_ID_LENGTH		0xC8 // 200

VOID DsConfig_LoadOrCreate(PDEVICE_CONTEXT Context);

VOID DsConfig_Store(PDEVICE_CONTEXT Context);
