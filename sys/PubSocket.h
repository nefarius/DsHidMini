#pragma once

#include <pshpack1.h>

struct DS_PUB_SOCKET_PACKET_HEADER
{
	UCHAR Topic[4];

	USHORT ProtocolVersion;

	USHORT PayloadLength;
};

typedef struct _DS3_INPUT_REPORT
{
	USHORT ButtonState;		// Main buttons
	UCHAR PSButtonState;		// PS button
	UCHAR LeftStickX;			// left Joystick X axis 0 - 255, 128 is mid
	UCHAR LeftStickY;			// left Joystick Y axis 0 - 255, 128 is mid
	UCHAR RightStickX;			// right Joystick X axis 0 - 255, 128 is mid
	UCHAR RightStickY;			// right Joystick Y axis 0 - 255, 128 is mid
	UCHAR PressureUp;			// digital Pad Up button Pressure 0 - 255
	UCHAR PressureRight;		// digital Pad Right button Pressure 0 - 255
	UCHAR PressureDown;			// digital Pad Down button Pressure 0 - 255
	UCHAR PressureLeft;			// digital Pad Left button Pressure 0 - 255
	UCHAR PressureL2;			// digital Pad L2 button Pressure 0 - 255
	UCHAR PressureR2;			// digital Pad R2 button Pressure 0 - 255
	UCHAR PressureL1;			// digital Pad L1 button Pressure 0 - 255
	UCHAR PressureR1;			// digital Pad R1 button Pressure 0 - 255
	UCHAR PressureTriangle;		// digital Pad Triangle button Pressure 0 - 255
	UCHAR PressureCircle;		// digital Pad Circle button Pressure 0 - 255
	UCHAR PressureCross;		// digital Pad Cross button Pressure 0 - 255
	UCHAR PressureSquare;		// digital Pad Square button Pressure 0 - 255
	USHORT AccelerometerX;		// X axis accelerometer Big Endian 0 - 1023
	USHORT AccelerometerY;      // Y axis accelerometer Big Endian 0 - 1023
	USHORT AccelerometerZ;		// Z axis accelerometer Big Endian 0 - 1023
	USHORT GyrometerX;			// Z axis Gyro Big Endian 0 - 1023

} DS3_INPUT_REPORT, *PDS3_INPUT_REPORT;

typedef struct _DS_PUB_SOCKET_PACKET
{
	struct DS_PUB_SOCKET_PACKET_HEADER Header;

	struct {
		//
		// Type of connection (wired, wireless)
		// 
		DS_CONNECTION_TYPE ConnectionType;

		//
		// Remote BTH address
		// 
		BD_ADDR HostAddress;

		//
		// Local device BTH address
		// 
		BD_ADDR DeviceAddress;

		//
		// Current reported battery status
		// 
		DS_BATTERY_STATUS BatteryStatus;

		DS_HID_DEVICE_MODE HidDeviceMode;

		USHORT VendorId;

		USHORT ProductId;

		USHORT VersionNumber;
		
	} Meta;

	DS3_INPUT_REPORT InputReport;
	
} DS_PUB_SOCKET_PACKET, *PDS_PUB_SOCKET_PACKET;

#include <poppack.h>

VOID FORCEINLINE DSHIDMINI_DS3_PUB_SOCKET_PACKET_INIT(
	PDS_PUB_SOCKET_PACKET Packet,
	PDEVICE_CONTEXT Context,
	PUCHAR RawInputReport
)
{
	//
	// Zero init
	// 
	RtlZeroMemory(Packet, sizeof(DS_PUB_SOCKET_PACKET));

	//
	// Header init
	// 
	RtlCopyMemory(Packet->Header.Topic, "DSHM", ARRAYSIZE(Packet->Header.Topic));
	Packet->Header.ProtocolVersion = 0x0001;
	Packet->Header.PayloadLength = sizeof(DS_PUB_SOCKET_PACKET) - sizeof(struct DS_PUB_SOCKET_PACKET_HEADER);

	//
	// Meta init
	// 
	Packet->Meta.ConnectionType = Context->ConnectionType;
	Packet->Meta.HostAddress = Context->HostAddress;
	Packet->Meta.DeviceAddress = Context->DeviceAddress;
	Packet->Meta.BatteryStatus = Context->BatteryStatus;
	Packet->Meta.HidDeviceMode = Context->Configuration.HidDeviceMode;
	Packet->Meta.VendorId = Context->Configuration.VendorId;
	Packet->Meta.ProductId = Context->Configuration.ProductId;
	Packet->Meta.VersionNumber = Context->Configuration.VersionNumber;

	//
	// Input report
	// 
	Packet->InputReport.ButtonState = (RawInputReport[2] << 8 | RawInputReport[3]);
	Packet->InputReport.PSButtonState = RawInputReport[4];
	Packet->InputReport.LeftStickX = RawInputReport[6];
	Packet->InputReport.LeftStickY = RawInputReport[7];
	Packet->InputReport.RightStickX = RawInputReport[8];
	Packet->InputReport.RightStickY = RawInputReport[9];
	Packet->InputReport.PressureUp = RawInputReport[14];
	Packet->InputReport.PressureRight = RawInputReport[15];
	Packet->InputReport.PressureDown = RawInputReport[16];
	Packet->InputReport.PressureLeft = RawInputReport[17];
	Packet->InputReport.PressureL2 = RawInputReport[18];
	Packet->InputReport.PressureR2 = RawInputReport[19];
	Packet->InputReport.PressureL1 = RawInputReport[20];
	Packet->InputReport.PressureR1 = RawInputReport[21];
	Packet->InputReport.PressureTriangle = RawInputReport[22];
	Packet->InputReport.PressureCircle = RawInputReport[23];
	Packet->InputReport.PressureCross = RawInputReport[24];
	Packet->InputReport.PressureSquare = RawInputReport[25];
	Packet->InputReport.AccelerometerX = (RawInputReport[40] << 8 | RawInputReport[41]);
	Packet->InputReport.AccelerometerY = (RawInputReport[42] << 8 | RawInputReport[43]);
	Packet->InputReport.AccelerometerZ = (RawInputReport[44] << 8 | RawInputReport[45]);
	Packet->InputReport.GyrometerX = (RawInputReport[46] << 8 | RawInputReport[47]);
}
