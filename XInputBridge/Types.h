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
