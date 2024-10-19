#include "Driver.h"
#include "Util.tmh"

#include <time.h>


VOID DumpAsHex(PCSTR Prefix, PVOID Buffer, ULONG BufferLength)
{
#ifdef DBG
	PSTR dumpBuffer;

	const size_t dumpBufferLength = ((BufferLength * sizeof(CHAR)) * 2) + 1;
	dumpBuffer = malloc(dumpBufferLength);
	if (dumpBuffer)
	{
		RtlZeroMemory(dumpBuffer, dumpBufferLength);

		for (ULONG i = 0; i < BufferLength; i++)
		{
			sprintf_s(&dumpBuffer[i * 2], dumpBufferLength, "%02X", ((PUCHAR)Buffer)[i]);
		}

		TraceVerbose(
			TRACE_DSHIDMINIDRV,
			"%s - Buffer length: %04d, buffer content: %s",
			Prefix,
			BufferLength,
			dumpBuffer
		);

		free(dumpBuffer);
	}
#else
	UNREFERENCED_PARAMETER(Prefix);
	UNREFERENCED_PARAMETER(Buffer);
	UNREFERENCED_PARAMETER(BufferLength);
#endif
}

void GenerateRandomEventName(PUCHAR buffer, size_t length)
{
	srand((unsigned int)time(NULL)); // Seed the random number generator

	const char charset[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
	size_t prefix_len = strlen(DSHM_HID_EVENT_NAME_PREFIX);

	// Ensure that the length is greater than the prefix length
	if (length <= prefix_len)
	{
		buffer[0] = '\0'; // Set the buffer to an empty string
		return;
	}

	strcpy_s((char*)buffer, length, DSHM_HID_EVENT_NAME_PREFIX);
	size_t random_part_len = length - prefix_len - 1; // Leave space for the null terminator

	for (size_t i = 0; i < random_part_len; ++i)
	{
		int key = rand() % (int)(sizeof(charset) - 1);
		buffer[prefix_len + i] = charset[key];
	}

	buffer[length - 1] = '\0'; // Null-terminate the string
}
