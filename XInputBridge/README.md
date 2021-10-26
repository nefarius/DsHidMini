# SCP XInput Bridge (Proxy DLL)

## About

This project brings back the [extended XInput API provided by ScpServer/ScpToolkit](https://github.com/nefarius/ScpToolkit/tree/master/ScpXInputBridge) to DsHidMini as a drop-in replacement for the now abandoned [SCP proxy DLL](https://github.com/nefarius/ScpToolkit/tree/9f4076ad6912002687d1824494258607d859c67e/XInput_Scp). In addition to the common XInput functions, it also provides the `XInputGetExtended` API, which returns all possible pressure values.

## API Overview

A brief summary of the exported library functions.

### `XInputGetExtended`

Reports back `struct _SCP_EXTN` with pressure values. For implementation details see `ScpTypes.h`. A value of `1.0f` represents fully pressed/engaged and `0.0f` represents default/disengaged. For axes, a value of `-1.0f` is equal to most west/south position, `0.0f` represents the centered/resting position and `1.0f` is equal to most east/north position.

### `XInputGetState`

Reports back [`XINPUT_GAMEPAD` structure](https://docs.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_gamepad) with all DS3 buttons and axes mapped identical to the Xbox 360 layout. It does *not* report the PS/Guide button.

### `XInputSetState`

Converts a vibratrion request into a DS3 output report. Translates and scales the rumble motor values accordingly (left gets mapped to strong, right gets mapped to weak motor). The player LED gets set to the (zero-based) `dwUserIndex` the function got invoked with (0 sets player 1, 1 sets player 2 and so forth).

### `XInputGetCapabilities`

Returns the typical Xbox 360 Controller compatible capabilities for every connected player index. Otherwise returns `ERROR_DEVICE_NOT_CONNECTED`.

### `XInputEnable`

Not implemented. Does nothing.

### `XInputGetDSoundAudioDeviceGuids`

Not implemented. Always returns `ERROR_DEVICE_NOT_CONNECTED`.

### `XInputGetBatteryInformation`

Not implemented. Always returns `ERROR_DEVICE_NOT_CONNECTED`.

### `XInputGetKeystroke`

Not implemented. Always returns `ERROR_DEVICE_NOT_CONNECTED`.

### `XInputGetStateEx`

Reports back [`XINPUT_GAMEPAD` structure](https://docs.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_gamepad) with all DS3 buttons and axes mapped identical to the Xbox 360 layout. It *does* report the PS/Guide button.

### `XInputWaitForGuideButton`

Not implemented. Always returns `ERROR_DEVICE_NOT_CONNECTED`.

### `XInputCancelGuideButtonWait`

Not implemented. Always returns `ERROR_DEVICE_NOT_CONNECTED`.

### `XInputPowerOffController`

Not implemented. Always returns `ERROR_DEVICE_NOT_CONNECTED`.

## 3rd party credits

- [araghon007/X1nput](https://github.com/araghon007/X1nput)
  - Xinput hook for Impulse Trigger emulation
- [rpcs3/rpcs3/Input/ds3_pad_handler.cpp](https://github.com/RPCS3/rpcs3/blob/5e436984a2b5753ad340d2c97462bf3be6e86237/rpcs3/Input/ds3_pad_handler.cpp)
- [pcsx2/pcsx2/PAD/Windows/XInputEnum.cpp](https://github.com/PCSX2/pcsx2/blob/6f7890b709d5e3f7f5b824781e493455efc92339/pcsx2/PAD/Windows/XInputEnum.cpp)
- [HIDAPI library for Windows, Linux, FreeBSD and macOS](https://github.com/libusb/hidapi)
  - A Simple library for communicating with USB and Bluetooth HID devices on Linux, Mac and Windows.
