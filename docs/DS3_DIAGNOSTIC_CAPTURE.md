# DualShock 3 diagnostic capture on Windows

This guide collects the USB startup exchange, controller identification reports,
input timing, and motion-sensor behavior needed to investigate a DualShock 3 or
SIXAXIS controller. It is written for Windows users who are comfortable
installing utilities and using Device Manager, but no development tools or
commands are required.

Please complete both captures in order:

1. Wireshark + USBPcap while DsHidMini is still installed.
2. The DsHidMini diagnostic tool after temporarily changing this controller to
   WinUSB with Zadig.

Only change the driver for the controller whose USB ID is **054C:0268**. Choosing
a keyboard, mouse, USB hub, or another device in Zadig can make that device stop
working until its driver is restored.

## What to send

Send these three files together in one ZIP:

- the Wireshark `.pcapng` capture;
- the diagnostic `.txt` file;
- the diagnostic `_stream.csv` file.

Also include a photo of the controller's rear label and, if it has already been
opened, clear photos of both sides of the circuit board.

## Privacy notice

USBPcap records traffic from every USB device on the selected USB host
controller. This can include keyboard input, microphone/webcam traffic, storage
activity, and Bluetooth addresses. During the short capture:

- close password managers and other sensitive applications;
- do not type passwords or other private text;
- disconnect unnecessary USB devices where practical;
- do not browse files on USB storage.

The diagnostic tool automatically hides the unique part of the controller
Bluetooth address and the complete paired-host Bluetooth address in its text
report. The Wireshark capture remains unredacted.

## Part 1: capture USB traffic with Wireshark

### Install Wireshark and USBPcap

1. Download the 64-bit Windows installer from
   [wireshark.org](https://www.wireshark.org/download.html).
2. Run the installer.
3. When optional components are offered, make sure **USBPcap** is selected.
   Npcap is unrelated to this USB capture and is not required for this task.
4. Finish the installation and restart Windows if requested.

Do not download USBPcap from an unofficial software-download site. The copy
offered by the Wireshark installer is sufficient.

### Find the correct USBPcap interface

1. Connect the controller directly to the PC by USB. Avoid a USB hub if
   possible.
2. Start Wireshark as Administrator.
3. Open **Capture → Options**.
4. Locate the interfaces named `USBPcap1`, `USBPcap2`, and so on.
5. Use the gear/options button beside each USBPcap interface and inspect
   **Attached USB Devices**. Find the interface containing the controller
   `VID 054C, PID 0268` or `PLAYSTATION(R)3 Controller`.
6. Remember that USBPcap interface, then unplug the controller.

If the attached-device list is unavailable, it is acceptable to select all
USBPcap interfaces. The resulting capture will be larger and may contain more
unrelated/private traffic.

### Record the startup and motion traffic

1. With the controller still unplugged, start a capture on the identified
   USBPcap interface.
2. Approve the administrator prompt if Windows displays one.
3. Plug the controller directly into the same USB port.
4. Wait about five seconds, then press the PS button once.
5. For about 15 seconds:
   - press each face button and move both sticks;
   - tilt the controller forward, backward, left, and right;
   - keep it flat and rotate it left and right.
6. Stop the capture using Wireshark's red stop button.
7. Select **File → Save As** and save the complete capture as
   `ds3-usb-capture.pcapng`.

Do not apply a display filter and export only the visible packets. The device
enumeration and control transfers are needed, so save the complete capture.

## Part 2: run the DsHidMini diagnostic tool

The diagnostic tool reads feature reports and live sensor data that are easier
to analyze outside the normal controller driver. Zadig is used to bind
**this controller only** to Microsoft's WinUSB driver temporarily.

### Before changing the driver

1. Keep the DsHidMini installer available. It is the recovery method if Windows
   does not select DsHidMini automatically afterward.
2. Disconnect every other DualShock 3 or SIXAXIS controller.
3. Download Zadig only from [zadig.akeo.ie](https://zadig.akeo.ie/).
4. Extract the provided `Ds3DiagnosticTool-win-x64.zip` to a normal folder.
   Do not run it from inside the ZIP.

### Temporarily install WinUSB

1. Connect the controller by USB.
2. Right-click Zadig and select **Run as administrator**.
3. In Zadig, select **Options → List All Devices**.
4. Select `PLAYSTATION(R)3 Controller`.
5. Verify the USB ID shown by Zadig is exactly **054C 0268**.
6. In the target-driver box, choose **WinUSB**.
7. Select **Replace Driver** and wait for the successful-installation message.
8. Unplug and reconnect the controller once.

Stop if the selected device or USB ID does not match. Do not experiment with
similarly named entries.

### Make the diagnostic capture

1. Double-click `Run-Diagnostics.cmd`.
2. If Microsoft Defender SmartScreen appears, verify the file came from the ZIP
   supplied for this investigation, then use **More info → Run anyway**.
3. Follow each prompt. The tool asks for six stationary positions:
   - buttons facing up;
   - buttons facing down;
   - left grip down;
   - right grip down;
   - front edge/triggers down;
   - USB port up.
4. For the final yaw test, hold the controller flat and slowly rotate it left
   and right around the vertical axis for five seconds.
5. When the tool reports completion, open its `captures` folder.
6. Keep both newly created files: one `.txt` and one `_stream.csv`.

The diagnostic sends temporary feature and output reports similar to those used
by a PlayStation 3. It does not write controller firmware or permanent settings.

### If the tool reports no WinUSB device

- Recheck that Zadig showed USB ID `054C:0268` and WinUSB.
- Unplug the controller, reconnect it directly to another USB port, and run
  `Run-Diagnostics.cmd` again.
- If it still fails, take a screenshot of the complete command window and
  include it with the report.

## Restore DsHidMini

Do this immediately after collecting the files:

1. Keep the controller connected and press **Windows+X → Device Manager**.
2. Find the controller under **Universal Serial Bus devices**. Its properties
   should show WinUSB/libwdi as the current driver.
3. Right-click only that controller and select **Uninstall device**.
4. Enable **Attempt to remove the driver for this device** if Windows offers
   the checkbox, then select **Uninstall**.
5. Unplug the controller and reconnect it.
6. Open DsHidMini ControlApp and confirm the controller appears again.

If it does not return:

1. Run the current DsHidMini installer and choose repair/install.
2. Unplug and reconnect the controller.
3. If necessary, restart Windows once.

Do not uninstall a USB root hub, host controller, keyboard, or mouse. The
controller can always be recovered by reinstalling DsHidMini; changing this
single device to WinUSB does not alter its firmware.

## Package maintainer instructions

The tool to provide is the existing standalone
[`research/ds3-motion/probe`](../research/ds3-motion/probe/) application,
published as a self-contained Windows x64 single-file executable. It collects:

- the USB device descriptor and endpoint layout;
- Features `0x01`, `0xF2`, `0xF5`, `0xF7`, `0xF8`, and the `0xEF` pages;
- report cadence and raw accelerometer/gyro samples;
- six-orientation averages and a yaw-response sample.

Build the end-user ZIP through NUKE:

```powershell
.\build.cmd PublishDs3DiagnosticTool
```

The resulting file is:

```text
artifacts\Ds3DiagnosticTool-win-x64.zip
```

The ZIP contains the self-contained executable, `Run-Diagnostics.cmd`, this
guide as `README.md`, and the repository license. Wireshark, USBPcap, and Zadig
are intentionally not redistributed in the ZIP; users should obtain their
current signed versions from the official sites linked above.
