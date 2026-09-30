# DS3 / SIXAXIS motion research

Throwaway tools, dumps and analysis for [issue 217](https://github.com/nefarius/DsHidMini/issues/217).
The findings a driver can rely on, the implementation checklist and the remaining
open measurements live in [`docs/MOTION.md`](../../docs/MOTION.md). This folder is
**not** part of the default NUKE compile target or Visual Studio solution.

These research artifacts produced the calibration and motion implementation now
used by the driver, IPC SDK, and ControlApp. The tools in this directory remain
standalone; NUKE only publishes the end-user diagnostic ZIP on request.

For a non-developer capture procedure, including Wireshark/USBPcap, Zadig,
driver restoration, and the packaged diagnostic tool, see
[`docs/DS3_DIAGNOSTIC_CAPTURE.md`](../../docs/DS3_DIAGNOSTIC_CAPTURE.md).

## Layout

| Path | What |
| --- | --- |
| `probe/` | Standalone .NET 10 WinUSB probe (published by the NUKE `PublishDs3DiagnosticTool` target) |
| `dumps/` | Probe logs + CSV streams. Bluetooth device/host addresses are redacted (`xx`) |
| `dumps/sixaxis-failed-attempts/` | Partial SIXAXIS runs (WinUSB kills the pad after a few seconds) |
| `pcap/` | `tshark` HID-control extractor + extracted PS3 capture text |
| `analysis/` | Offline scripts that reproduce the MOTION.md numbers |
| `ghidra/` | Headless export script + clean-room `ds3cal` algorithm. **No binaries** |

## Bind a pad to WinUSB (Zadig)

1. Plug the DualShock 3 / SIXAXIS over USB.
2. In [Zadig](https://zadig.akeo.ie/), Options → List All Devices, pick
   `PLAYSTATION(R)3 Controller` (`054C:0268`).
3. Replace Driver with **WinUSB**. The Device Interface GUID Zadig writes is
   whatever it generates; the probe enumerates every WinUSB-bound `054C:0268`.
4. After the session, restore DsHidMini via Device Manager: uninstall the WinUSB
   device **without** deleting the driver package, then rescan.

Original SIXAXIS units stop answering every transfer a few seconds after
enumeration under WinUSB (selective suspend; Windows still reports the node
healthy). Use `--wait` and plug the pad only after the probe is already
polling. DsHidMini itself does not have this problem.

## Run the probe

From the repository root:

```
.\build.cmd PublishDs3DiagnosticTool
.\artifacts\Ds3DiagnosticTool-win-x64\Ds3MotionProbe.exe --name <label> [--out <dir>] [--seconds N] [--interactive] [--calbyte] [--no-stream] [--wait]
```

`--out` defaults to a `captures` folder beside the executable. Labels used so far: `ds3-cechzc2e-a1`,
`ds3-cechzc2u-a2`, `ds3-cechzc2e`, `sixaxis`, `fake-ds3`, `obigben-aftermarket`.

| Goal | Command |
| --- | --- |
| Dump only (ID + EEPROM pages) | `--name <label> --no-stream` |
| Six-orientation accel table | `--name <label> --interactive` |
| Yaw sign/scale | `--name <label> --interactive` then, pad **flat**, 90° clockwise from above (record-on-a-turntable, not a steering wheel), pause, 90° back, repeat 2–3 times |
| Cal-byte sensitivity | `--name <label> --calbyte` (skipped automatically if Feature `0x01` has no field `0x07` and is not SIXAXIS-class) |
| Catch a SIXAXIS | `--name sixaxis --wait` and plug after it starts polling |

## Analysis scripts

From `research/ds3-motion/analysis/`:

```
powershell -ExecutionPolicy Bypass -File .\Pad-Matrix.ps1
powershell -ExecutionPolicy Bypass -File .\Check-Accel.ps1
powershell -ExecutionPolicy Bypass -File .\Analyze-Yaw4.ps1
```

They read `../dumps` and reprint the pad matrix, the accelerometer-formula check
and the yaw integrals documented in MOTION.md.

## Pcap extractor

Needs Wireshark's `tshark`. Input pcaps are the CircumSpector captures under
`GameControllerResearch` (not in this repo):

```
powershell -ExecutionPolicy Bypass -File .\pcap\Extract-HidControl.ps1 -Pcap <file.pcap> -Out <out.txt>
```

## Ghidra

See [`ghidra/README.md`](ghidra/README.md). `ds3cal.dll` and `sixaxis.sys` are
**not** committed (Sony / third-party binaries).
