<img src="assets/noun_playstation_128x128.png" align="right" />

# DsHidMini

Virtual HID Mini-user-mode driver for Sony DualShock 3 Controllers

[![Discord](https://img.shields.io/discord/346756263763378176.svg)](https://discord.vigem.org/) [![Website](https://img.shields.io/website-up-down-green-red/https/vigem.org.svg?label=ViGEm.org)](https://vigem.org/)

## Summary

TBD

## Features

- **Bluetooth support** if used in conjunction with [BthPS3](https://github.com/ViGEm/BthPS3)
- Automatically pairs the controller to Windows Bluetooth (if Bluetooth host radio is present)
- Multiple configurable HID Report Descriptors for wide range of compatibility
  - Single Gamepad device exposing all controls including **pressure sensitive buttons**
  - Split/multi device emulation to overcome DirectInput axis limits
  - Sony `sixaxis.sys` emulation (both wired **and wireless**)
- Quick disconnect (on Bluetooth) by pressing `L1 + R1 + PS` together for over one second
- Custom LED states indicate battery charge level
  - Wired: Charging will cycle through 1 to 4, if fully charged will stay on 4
  - Wireless: 4 = Full, 3 = High, 2 = Medium/low, 1 = Low/dying
- **Rumble exposure via Force Feedback**
  - The rumble motors are exposed as Force Feedback effects, allowing for great game compatibility

## How it works

TBD

## How to build

### Prerequisites

- [Step 1: Install Visual Studio 2019](<https://docs.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk#download-icon-step-1-install-visual-studio-2019>)
- [Step 2: Install WDK for Windows 10, version 2004](<https://docs.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk#download-icon-step-2-install-wdk-for-windows-10-version-2004>)
- [Step 3: Clone the Driver Module Framework (DMF)](https://github.com/microsoft/DMF) into the same parent directory.
  - Build the `DmfU` project with Release and Debug configurations for all architectures (x64 and Win32).

You can build individual projects of the solution within Visual Studio.

## Sources & 3rd party credits

- [Eleccelerator Wiki - DualShock 3](http://eleccelerator.com/wiki/index.php?title=DualShock_3)
- [Eleccelerator - USB Descriptor and Request Parser](http://eleccelerator.com/usbdescreqparser/)
- [HID Usage Tables](https://usb.org/sites/default/files/documents/hut1_12v2.pdf)
- [Arduino - felis/USB_Host_Shield_2.0 - PS3 Information](https://github.com/felis/USB_Host_Shield_2.0/wiki/PS3-Information#USB)
- [PS3 and Wiimote Game Controllers on the Arduino Host Shield: Part 2](https://web.archive.org/web/20160326093555/https://www.circuitsathome.com/mcu/ps3-and-wiimote-game-controllers-on-the-arduino-host-shield-part-2)
- [ribbotson/USB-Host](https://github.com/ribbotson/USB-Host/tree/master/ps3/PS3USB)
- [HID: sony: Update device ids](https://patchwork.kernel.org/patch/9367441/)
- [ViGEm/FireShock](https://github.com/ViGEm/FireShock)
- [ViGEm/AirBender](https://github.com/ViGEm/AirBender)
- [ViGEm/WireShock](https://github.com/ViGEm/WireShock)
- [Microsoft/Driver Module Framework (DMF)](https://github.com/microsoft/DMF)
- [Microsoft - DMF - HID minidriver module](https://github.com/microsoft/DMF/issues/69)
- [Microsoft - DMF - VHidMini2DmfK and VHidMini2DmfU Sample Drivers](https://github.com/microsoft/DMF/tree/master/DmfSamples/VHidMini2Dmf)
- [linux/drivers/hid/hid-sony.c](https://github.com/torvalds/linux/blob/master/drivers/hid/hid-sony.c)
- [Summary of the DPInst XML Elements](https://web.archive.org/web/20120623222252/http://msdn.microsoft.com/en-us/library/ff553383.aspx)
- [dpinst XML Element](https://docs.microsoft.com/en-us/windows-hardware/drivers/install/dpinst-xml-element)
- [Driver installation and updating made easy: DPInst.exe](https://docs.microsoft.com/en-us/archive/blogs/svengruenitz/driver-installation-and-updating-made-easy-dpinst-exe)
- [inih (INI Not Invented Here)](https://github.com/benhoyt/inih)
- [The HID Page](http://janaxelson.com/hidpage.htm)
- [uthash - a hash table for C structures](https://github.com/troydhanson/uthash)
- [FirstPlatoLV/EmuController](https://github.com/FirstPlatoLV/EmuController)
- [FontAwesome5](https://github.com/MartinTopfstedt/FontAwesome5)
