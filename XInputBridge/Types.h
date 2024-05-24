#pragma once

//
// Type of the device behind a give user index
// 
enum XI_DEVICE_TYPE
{
	XI_DEVICE_TYPE_NOT_CONNECTED = 0,
	XI_DEVICE_TYPE_DS3,
	XI_DEVICE_TYPE_XUSB,
};

constexpr uint8_t INVALID_X_INPUT_USER_ID = 0xff; // XUSER_INDEX_ANY

/*
 * Source: https://github.com/RPCS3/rpcs3/blob/5e436984a2b5753ad340d2c97462bf3be6e86237/rpcs3/Input/ds3_pad_handler.cpp#L9-L40
 */

struct ds3_rumble
{
	UCHAR padding = 0x00;
	UCHAR small_motor_duration = 0xFF; // 0xff means forever
	UCHAR small_motor_on = 0x00; // 0 or 1 (off/on)
	UCHAR large_motor_duration = 0xFF; // 0xff means forever
	UCHAR large_motor_force = 0x00; // 0 to 255
};

struct ds3_led
{
	UCHAR duration = 0xFF; // total duration, 0xff means forever
	UCHAR interval_duration = 0xFF; // interval duration in deciseconds
	UCHAR enabled = 0x10;
	UCHAR interval_portion_off = 0x00; // in percent (100% = 0xFF)
	UCHAR interval_portion_on = 0xFF; // in percent (100% = 0xFF)
};

struct ds3_output_report
{
	UCHAR report_id = 0x00;
	UCHAR idk_what_this_is[3] = { 0x02, 0x00, 0x00 };
	ds3_rumble rumble;
	UCHAR padding[4] = { 0x00, 0x00, 0x00, 0x00 };
	UCHAR led_enabled = 0x00; // LED 1 = 0x02, LED 2 = 0x04, etc.
	ds3_led led[4];
	ds3_led led_5; // reserved for another LED
};
