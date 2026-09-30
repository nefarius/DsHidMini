# Dual Controller extension (experimental)

Adds a physical DualShock 4 backend alongside the existing DsHidMini DualShock 3
backend. Both controller types can be connected at the same time, by USB or
Bluetooth. This is a community fork, not an official Nefarius release.

The signed PS3 driver stays unchanged. PS4 is read through Windows native HID
and mapped to an Xbox 360 target using ViGEmBus. `DualController.exe` must remain
running for PS4 Xbox mapping. This is a unified installer with two controller
backends, not a claim that the original DsHidMini driver accepts physical PS4s.

## Build

With .NET 10 SDK, from the repository root:

```powershell
dotnet run --project DualController/Tests -c Release
dotnet publish DualController/Bridge -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o DualController/artifacts/app --configfile DualController/nuget.config
```

On Windows, build the offline installer:

```powershell
./DualController/Installer/Fetch-Dependencies.ps1
& 'C:\Program Files\Inno Setup 7\ISCC.exe' DualController/Installer/DualController.iss
```

The `Dual controller installer` GitHub Actions workflow builds/tests the PS4
bridge, checks Windows HID interop without requiring a controller, verifies
hashes and signatures of pinned installers, then produces the combined setup
and a dependency-verification manifest. It needs no signing secrets. The new
app/setup are unsigned; the included upstream driver packages remain signed.

## Validation and scope

- Protocol tests cover every hat direction, full button mapping, axes, analogue
  triggers, battery fields, truncated frames, Bluetooth CRC corruption and an
  independent Python/zlib CRC vector, USB/Bluetooth output layout, and excluding
  emulated PS3/ViGEm devices from physical-device enumeration.
- Dedicated state, reader, virtual target, and feedback loop per physical PS4.
  No shared mutable input state across controllers; hotplug/retry cleanup sends
  a neutral report and removes the virtual controller.
- Per-device ancestry is checked to avoid recapturing DS3 DS4-mode output or
  virtual DS4s. Serial-based duplicate USB/BT connections prefer USB when the
  serial is available. Physical Sony v1/v2 IDs only; no blanket Sony matching.
- Input timeout disconnects stalled targets. BT output/input use HID CRC seed
  bytes 0xA2/0xA1 and the standard little-endian CRC32 footer.
- The protocol was checked against Sony's `hid-playstation` implementation in
  [Linux](https://github.com/torvalds/linux/blob/master/drivers/hid/hid-playstation.c).
  This C# implementation is independent, not a copy of its code.
- PS4 touchpad gestures, motion mapping, audio, implicit hiding of native input,
  and virtual-input-blocking games are outside this bridge's scope. ViGEm is a
  retired dependency; there is no claim this fork maintains that driver.
- **Real-controller and clean-Windows installation checks are still required.**
  A successful compile and simulated reports do not establish hardware support.

See [INSTALL.txt](INSTALL.txt) for user instructions and known limitations.
The original driver's licensing and notices are retained.
