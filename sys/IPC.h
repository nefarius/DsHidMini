#pragma once

#define DSHM_IPC_FILE_MAP_NAME		"Global\\DsHidMiniSharedMemory"
#define DSHM_IPC_MUTEX_NAME			"Global\\DsHidMiniMutex"
#define DSHM_IPC_READ_EVENT_NAME	"Global\\DsHidMiniReadEvent"
#define DSHM_IPC_WRITE_EVENT_NAME	"Global\\DsHidMiniWriteEvent"
#define DSHM_IPC_BUFFER_SIZE		4096 // 4KB


NTSTATUS InitIPC(void);

void DestroyIPC(void);
