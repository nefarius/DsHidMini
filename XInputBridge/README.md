# XInput Bridge

Drop-in **XInput proxy DLL** for [DsHidMini](../README.md). It replaces `XInput1_3.dll` so games and apps get standard XInput for up to 8 controllers while DsHidMini DualShock 3 pads (in [SXS mode](https://docs.nefarius.at/projects/DsHidMini/HID-Device-Modes-Explained/#sxs)) are handled by the bridge and real Xbox/XInput devices are forwarded to the system `XInput1_3.dll`. The bridge also exposes **XInputGetExtended** for DS3 pressure-sensitive (analog) button and axis data.

## Overview

- **Role:** Implements the XInput 1.3 API plus selected XInput 1.4 functions (XInput 1.3+ / XInput 1.4–compatible); built as `XInput1_3.dll` so apps load it instead of the system DLL (e.g. via game directory or [x360ce](https://www.x360ce.com/)). Functions such as `XInputWaitForGuideButton`, `XInputCancelGuideButtonWait`, and `XInputPowerOffController` are proxied to the system DLL.
- **Per–user index:** Each of the 4 XInput “slots” (plus extended handling) is either:
  - **DsHidMini DS3** (SXS HID device) → bridge reads HID via [HIDAPI](https://github.com/libusb/hidapi), maps to XInput state/rumble and optionally exposes extended data, or
  - **Real XUSB device** → call is proxied to `C:\Windows\System32\XInput1_3.dll`.
- **Extended API:** `XInputGetExtended` returns a struct (see `include/DsHidMini/ScpTypes.h`) with normalized float values for all DS3 axes and buttons (1.0 = full press / full axis, 0.0 = released / center). Only meaningful for slots occupied by a DsHidMini DS3.

This project revives the [extended XInput API from ScpToolkit](https://github.com/nefarius/ScpToolkit/tree/master/ScpXInputBridge) as a maintained replacement for the legacy [SCP proxy DLL](https://github.com/nefarius/ScpToolkit/tree/9f4076ad6912002687d1824494258607d859c67e/XInput_Scp).

## For users

Usage, setup, and deployment are documented on the project site:  
**[DsHidMini – SCP XInput Bridge](https://docs.nefarius.at/projects/DsHidMini/SCP-XInput-Bridge/).**

## API summary

| Function | DsHidMini DS3 (SXS) | Real XUSB |
|----------|----------------------|-----------|
| **XInputGetExtended** | Returns `SCP_EXTN` pressure/axis data. | Not applicable (DS3-only API). |
| **XInputGetState** | Mapped from HID; Xbox layout; no Guide/PS. | Proxied to system DLL. |
| **XInputGetStateEx** | Same as above; *does* report PS/Guide. | Proxied to system DLL. |
| **XInputSetState** | Rumble → DS3 output report; player LED set from user index. | Proxied to system DLL. |
| **XInputGetCapabilities** | Xbox 360–style capabilities. | Proxied to system DLL. |
| **XInputEnable** | Proxied to system DLL. | Proxied to system DLL. |
| **XInputGetDSoundAudioDeviceGuids** | Proxied. | Proxied. |
| **XInputGetBatteryInformation** | Proxied. | Proxied. |
| **XInputGetKeystroke** | Proxied. | Proxied. |
| **XInputWaitForGuideButton** | Proxied. | Proxied. |
| **XInputCancelGuideButtonWait** | Proxied. | Proxied. |
| **XInputPowerOffController** | Proxied. | Proxied. |

- **Thumbsticks:** A jitter-compensation dead zone is applied when the bridge handles a DS3.
- **Errors:** If the given user index has no device (neither DS3 nor XUSB), functions return `ERROR_DEVICE_NOT_CONNECTED`.

`XInputGetExtended` and the `SCP_EXTN` layout are described in the main repo under `include/DsHidMini/ScpTypes.h`.

## Project layout

| Area | Description |
|------|-------------|
| `dllmain.cpp` | DLL entry; initializes/destroys global state. |
| `GlobalState.*` | Singleton: device list, callbacks, proxy to real XInput1_3. |
| `GlobalState.XInput.cpp` | XInput API implementation (get/set state, capabilities, etc.). |
| `DeviceState.*` | Per-slot state: DS3 (HID handle) vs XUSB (real user index), packet number, last report. |
| `XInputBridge.cpp` / `.h` | Export declarations and shared XInput type definitions. |
| `Common.h`, `Macros.h`, `Types.h`, `UniUtil.h` | Shared types and helpers. |
| `XInput.def` | Export ordinals (matches XInput 1.3 + ordinal 200 for GetExtended). |

Dependencies (via [vcpkg](https://vcpkg.io/)): **hidapi**, **abseil**, **winreg**.

## Build

- **IDE:** Visual Studio 2022 with C++ desktop workload.
- **vcpkg:** Install [vcpkg](https://vcpkg.io/en/docs/getting-started.html), then integrate (e.g. `vcpkg integrate install`). The project uses the vcpkg MSBuild integration; triplets are set in the vcxproj (e.g. `x64-windows-static` for x64).
- **Solution:** Open the repository solution (e.g. `dshidmini.sln`) and build the **XInputBridge** project. Supported platforms: **Win32**, **x64**, **ARM64**.

Output is `XInput1_3.dll`; deploy next to the game executable (or use a loader like x360ce) so the game loads this DLL instead of the system XInput.

## Known limitations

- **Wireless Xbox 360 + Microsoft dongle:** Multiple wireless 360 pads appear as one device instance, so per-pad arrival/removal notifications may not work correctly. Behavior with this setup is not guaranteed; testing and feedback welcome.

## For developers

The bridge is maintained and stable. You can use it in your own projects if you need simple access to DS3 pressure data via `XInputGetExtended` while keeping standard XInput for other controllers.

## References and credits

- [ScpToolkit / ScpXInputBridge](https://github.com/nefarius/ScpToolkit/tree/master/ScpXInputBridge) — extended XInput API origin
- [X1nput](https://github.com/araghon007/X1nput), [OpenXInput](https://github.com/Nemirtingas/OpenXinput) — XInput proxy ideas
- [RPCS3 ds3_pad_handler](https://github.com/RPCS3/rpcs3/blob/master/rpcs3/Input/ds3_pad_handler.cpp), [PCSX2 XInputEnum](https://github.com/PCSX2/pcsx2/blob/master/pcsx2/PAD/Windows/XInputEnum.cpp) — reference implementations
- [HIDAPI](https://github.com/libusb/hidapi), [Abseil](https://abseil.io/), [WinReg](https://github.com/GiovanniDicanio/WinReg) — dependencies
- [RawInputDemo](https://github.com/DJm00n/RawInputDemo) — Raw Input / device enumeration reference
