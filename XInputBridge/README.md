# SCP XInput Bridge (Proxy DLL)

## About

This project brings back the [extended XInput API provided by ScpServer/ScpToolkit](https://github.com/nefarius/ScpToolkit/tree/master/ScpXInputBridge) to DsHidMini as a drop-in replacement for the now abandoned [SCP proxy DLL](https://github.com/nefarius/ScpToolkit/tree/9f4076ad6912002687d1824494258607d859c67e/XInput_Scp). In addition to the common XInput functions, it also provides the `XInputGetExtended` API, which returns all possible pressure values.

## API Overview

A brief summary of the exported library functions.

### `XInputGetExtended`

Reports back `struct _SCP_EXTN` with pressure values. For implementation details see `ScpTypes.h`.

### `XInputGetState`

Reports back [`XINPUT_GAMEPAD` structure](https://docs.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_gamepad) with all DS3 buttons and axes mapped identical to the Xbox 360 layout. It does *not* report the PS/Guide button.

### `XInputSetState`

Converts a vibratrion request into a DS3 output report. Translates and scales the rumble motor values accordingly (left gets mapped to strong, right gets mapped to weak motor). The player LED gets set to the (zero-based) `dwUserIndex` the function got invoked with (0 sets player 1, 1 sets player 2 and so forth).

### `XInputGetCapabilities`

Not implemented. Always returns `ERROR_DEVICE_NOT_CONNECTED`.

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
