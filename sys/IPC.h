#pragma once

#define DSHM_IPC_FILE_MAP_NAME	"Global\\DsHidMiniSharedMemory"
#define DSHM_IPC_EVENT_NAME		"Global\\DsHidMiniWriteEvent"
#define DSHM_IPC_BUFFER_SIZE	1024


void InitIPC();
