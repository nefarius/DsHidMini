# DualShock 3 / SIXAXIS motion sensors

Research notes and implemented behavior for [issue #217](https://github.com/nefarius/DsHidMini/issues/217)
(motion support). Feature `0x01` is parsed and published as device properties
(see [What DsHidMini publishes](#what-dshidmini-publishes)). USB motion
calibration, Sony-compatible correction, IPC telemetry, and the ControlApp
viewer are implemented; see [What DsHidMini does today](#what-dshidmini-does-today).

Sources of truth used, in order of authority:

1. Six PS3-console-to-pad USB link-layer captures
   ([CircumSpector/Research, "Sony DualShock 3"](https://github.com/CircumSpector/Research/tree/master/Sony%20DualShock%203)):
   three DualShock 3 pads on CECHZC2E-A1, CECHZC2E-B1 and CECHZC2U-A2 consoles
   plus one original SIXAXIS, dissected with a local `tshark` (recipe in
   [`PS3_USB_STARTUP.md`](PS3_USB_STARTUP.md#tshark-recipe)).
2. A clean-room decompilation (Ghidra 12.1.3, headless) of **Sony's own
   `sixaxis.sys`** (x64, 28,424 bytes, validly Authenticode-signed by "Sony
   Computer Entertainment Inc."; SHA-256
   `b040b0a3a519d8d43e21a02fb9f2a52300f40f07226e18c4ba4e61c6fc380a51`). This is
   the authoritative source for everything in this document and supersedes the
   guesswork in the public references. See
   [What `sixaxis.sys` actually does](#what-sixaxissys-actually-does).
3. A clean-room decompilation of rajkosto's closed-source `ds3cal.dll` (x86 and
   x64 builds are identical) from the
   [rajkosto/ScpToolkit fork](https://github.com/rajkosto/ScpToolkit/tree/master/ScpControl/ds3cal).
   Its gyro auto-zero parameter block is **field-for-field identical** to the one
   in `sixaxis.sys`, so the two implement the same algorithm; `ds3cal.dll` is
   effectively a user-mode port of Sony's calibrator.
4. Live dumps from **eight physical pads** - four genuine DualShock 3s, two
   SIXAXIS-family units and two counterfeits - each bound to WinUSB via Zadig
   and read with a throwaway probe (see [Tooling](#tooling)). These cover a
   six-orientation accelerometer table, a yaw sweep of known direction and a
   cal-byte sweep, and three of them turn out to be the same physical units
   that produced the PS3 captures above. Per-pad detail is in the
   [Pad matrix](#pad-matrix).
5. Public references: RPCS3 `ds3_pad_handler.cpp`,
   [psdevwiki DualShock 3](https://www.psdevwiki.com/ps3/DualShock_3),
   [mclab SIXAXIS notes](https://mclab.uunyan.com/lab/sixaxis/sxs004.htm),
   [lewy20041/DS3_Input_And_Report_Inspector](https://github.com/lewy20041/DS3_Input_And_Report_Inspector).

## Raw sensor bytes in the input report

`DS3_RAW_INPUT_REPORT` (`include/DsHidMini/Ds3Types.h`), offsets counted with the
report ID `0x01` at byte 0 (49-byte USB / BTH HID report):

| Bytes | Field | Notes |
| --- | --- | --- |
| 41-42 | `AccelerometerX` | **big-endian** u16, 10-bit payload (0-1023) |
| 43-44 | `AccelerometerY` | big-endian u16 |
| 45-46 | `AccelerometerZ` | big-endian u16 |
| 47-48 | `Gyroscope` | big-endian u16, **yaw only** (rotation about the axis perpendicular to the face of the pad) |

All four values idle near 512. The pad has no pitch/roll gyro; only one axis.

lewy20041's inspector reads these little-endian, which is why it needs an odd
35583 LSB/g constant; his `0xEF` offsets are also shifted by one byte (see
below). Treat that tool's numbers as unverified.

### Observed idle values

From the PS3 captures (pads being handled on a desk, so X/Y are not perfectly
level) and from the WinUSB probe with each pad lying flat:

| Pad | X | Y | Z | Gyro | Reports |
| --- | --- | --- | --- | --- | --- |
| DS3 on CECHZC2E-A1 | 511 | 491 | 398 | 487 | 1853 |
| DS3 on CECHZC2E-A1 (rumble test, 60 s) | 495 | 532 | 398 | 487 | 5978 |
| DS3 on CECHZC2E-B1 | 498 | 563 | 424 | 494 | 1837 |
| DS3 on CECHZC2U-A2 | 505 | 553 | 408 | 386 | 1129 |
| SIXAXIS (PS3 capture) | 505 | 554 | 469 | 758 (very noisy, 2-997) | 1199 |
| DS3-A1a, flat on desk (WinUSB probe) | 494 | 476 | 396 | 483 | 200 |
| DS3-A1b, flat on desk (WinUSB probe) | 508 | 488 | 397 | 491 | 200 |
| DS3-A2, flat on desk (WinUSB probe) | 504 | 495 | 400 | **361** | 200 |
| DS3-E, flat on desk (WinUSB probe) | 510 | 502 | 379 | 498 | 200 |
| Obigben aftermarket, flat (WinUSB probe) | 513 | 495 | 383 | 500 (frozen) | 200 |
| Fake DS3, flat (WinUSB probe) | 512 | 512 | 384 | 512 (frozen) | 1846 |

The short pad names are defined in the [Pad matrix](#pad-matrix). Flat on the
desk a working pad reads **Z ~ 1 g below its zero-g value** (398 vs 511 on
DS3-A1b, 396 vs 496 on DS3-A1a, 383 vs 512 on the Obigben); X and Y sit at
their zero-g values when level. Two pads are exceptions and both are
instructive: the Fake DS3 has no sensors at all (every axis is a frozen
constant) and DS3-A2 idles its gyro at 361 rather than near its EEPROM zero of
521 - see [Hardware cal byte](#hardware-cal-byte).

### Orientation / sign table (DS3-A1a, probe `--interactive`)

Raw big-endian values averaged over 200 reports per pose, and the same values
after the calibration formula below (this pad's `zero`/`oneG`: X 498/386,
Y 494/383, Z 496/387):

| Pose | raw X | raw Y | raw Z | cal X | cal Y | cal Z | 1 g on |
| --- | --- | --- | --- | --- | --- | --- | --- |
| flat on desk, buttons up | 494 | 475 | **396** | 509 | 493 | **409** | Z = -1 g |
| upside down, buttons on desk | 494 | 469 | **624** | 509 | 487 | **644** | Z = +1 g |
| standing on the left grip | **607** | 511 | 512 | **621** | 529 | 528 | X = +1 g |
| standing on the right grip | **383** | 516 | 515 | **397** | 534 | 531 | X = -1 g |
| front edge down (triggers on desk) | 496 | **385** | 501 | 511 | **402** | 517 | Y = -1 g |
| back edge down (triggers up) | 495 | **603** | 520 | 510 | **622** | 536 | Y = +1 g |

In Sony's convention (512 = 0 g, +113 = +1 g) an axis reads **+1 g when it
points upwards**: X is positive with the left grip down, Y with the trigger edge
up, Z with the pad upside down. That is a right-handed frame of **X towards the
left grip, Y towards the trigger edge, Z down through the buttons**. Raw and
calibrated values share the signs; only `sixaxis.sys`/DsHidMini's SIXAXIS mode
mirror X (`0x3FF - X`). The ~10-count residuals (cal Z 409 instead of 399 when
flat) are this pad's ageing/temperature drift against its factory EEPROM.

## Identification: `GET_REPORT Feature 0x01`

64 bytes requested, layout (offsets into the returned buffer, which starts
with `00 01`):

| Offset | Meaning | Values seen |
| --- | --- | --- |
| 2-4 | firmware/board revision bytes; the same three bytes are echoed at offset 2-4 of every `0xEF` answer | B1 `04 01 03`, and per the [pad matrix](#pad-matrix) `04 00 06`, `04 00 08` (twice), `04 00 0b`, `02 00 03`, `03 00 04`, `03 00 05` (both counterfeits) |
| 8-11 | sensor/pad type, four identical bytes | `18 18 18 18` DualShock 3, `17 17 17 17` SIXAXIS - but **not a reliable class test**: SIXAXIS-2 reports `18 18 18 18`, and both counterfeits do too |
| 0x25 | number of calibration field IDs that follow | 4 / 3 / 4 / 1 / 3 / 2 |
| 0x26.. | calibration field IDs | `00 01 02 07` (DS3-A1b, DS3-A2), `00 01 02` (B1, DS3-A1a, DS3-E), `06` (both SIXAXIS units), `01 02` (both counterfeits) |

These offsets are **verified**: `sixaxis.sys` reads this report with
`GET_REPORT(Feature, 0x01)` into a 49-byte buffer and tests exactly bytes 8-11
for `0x18`, byte `0x25` as the field count and `0x26..` as the field list.

Field ID `0x07` is the important one. `sixaxis.sys` derives two flags from this
report and they select the whole motion code path (bit names ours):

| Flag | Set when | Pads |
| --- | --- | --- |
| `DS3_TYPE` (0x08) | bytes 8-11 all `0x18` | every DS3, both counterfeits, and SIXAXIS-2 |
| `PLAIN_ZERO` (0x10) | the field list starts `01 02` at index 0 **or** index 1 | B1, DS3-A1a, DS3-A1b, DS3-A2, DS3-E (`00 01 02...`), both counterfeits (`01 02`) |
| `HW_CAL` (0x20) | the field list contains `07` anywhere | DS3-A1b, DS3-A2 |

A SIXAXIS (single field `06`) matches neither, so it gets `PLAIN_ZERO` clear and
`HW_CAL` clear. The three resulting gyro paths are described under
[Gyroscope](#gyroscope). Pads without field `0x07` get no cal byte and their
EEPROM gyro cal-byte slot reads `0`; rajkosto's `UsbDs3.cs` encodes the same
decision tree.

When DsHidMini (and the SDK parser) collapse those flags to a single motion
path, **`HW_CAL` wins** if field `0x07` is present. Those pads also match
`PLAIN_ZERO` because the list is `00 01 02 07` (`01 02` starts at index 1):

| Motion path | Set when | Pads |
| --- | --- | --- |
| `HW_CAL` | the field list contains `0x07` | DS3-A1b, DS3-A2 |
| `SIXAXIS` | `PLAIN_ZERO` is clear (typically a single field `06`) | SIXAXIS-1, SIXAXIS-2 |
| `PLAIN_ZERO` | the field list starts `01 02` at index 0 or 1 | B1, DS3-A1a, DS3-E, both counterfeits |

### Clone heuristic

A **heuristic**, not a verdict. Both known counterfeits (Obigben, Fake DS3) and
no genuine pad in the [pad matrix](#pad-matrix) match:

- the calibration field list is exactly `01 02` (count 2, no leading `00`), **and**
- byte `0x29` is `0x64`

This is independent of the OUI / MAC genuine check used elsewhere.

Two observations from the [pad matrix](#pad-matrix) constrain how this may be
implemented:

- **The field list has to be read per pad.** DS3-A1a and DS3-A1b share firmware
  bytes `04 00 08` and the same model label, yet only A1b lists field `0x07`.
  It cannot be inferred from the revision bytes.
- **The type bytes must not be used to pick the gyro path.** SIXAXIS-2 reports
  `18 18 18 18` (DS3-class) with a single field `06`, so it takes the SIXAXIS
  path and wants its cal byte at `[3]/[4]` despite claiming to be a DS3. Only
  the field list decides.

### What DsHidMini publishes

On USB, `DsUsb_PrepareHardware` GETs Feature `0x01` (see
[issue #50](https://github.com/nefarius/DsHidMini/issues/50)) and writes the
raw 64-byte blob plus the decoded fields as read-only device properties on
`{3FECF510-CC94-4FBE-8839-738201F84D59}`:

| PID | Key | Type |
| --- | --- | --- |
| 4 | `DEVPKEY_DsHidMini_RO_IdentificationData` | BINARY 64 |
| 8 | `DEVPKEY_DsHidMini_RO_IdentificationFirmware` | UINT32, packed `b2<<16 \| b3<<8 \| b4` |
| 9 | `DEVPKEY_DsHidMini_RO_IdentificationPadType` | BYTE (first of offsets 8-11; informational) |
| 10 | `DEVPKEY_DsHidMini_RO_IdentificationMotionPath` | BYTE (`Unknown` / `PlainZero` / `HwCal` / `Sixaxis`) |
| 11 | `DEVPKEY_DsHidMini_RO_IdentificationCloneHeuristic` | BOOLEAN |

Bluetooth instances are not queried. A GET success with a parse failure still
keeps the raw blob and leaves the decoded keys unset. Publishing these
properties does **not** change HID mode, output reports, or rumble.

## Calibration EEPROM: `Feature 0xEF`, `0xF8`, `0xF7`

### Page select and read

The PS3 reads two 16-byte EEPROM pages right after `0xF2`/`0xF5` and before the
first output report (step 6 of the startup sequence in
[`PS3_USB_STARTUP.md`](PS3_USB_STARTUP.md#sequence)):

```
SET_REPORT Feature 0xEF, 48 bytes:  00 00 00 00 03 01 A0 00 ... 00     ; select page 0xA0
GET_REPORT Feature 0xEF, 64 bytes:  00 EF vv vv vv 03 01 A0 00 00 00 00 00 00 00 00 00 <16 page bytes> 00 ... 05 00 ...
SET_REPORT Feature 0xEF:            00 00 00 00 03 01 B0 00 ... 00     ; select page 0xB0
GET_REPORT Feature 0xEF:            00 EF vv vv vv 03 01 B0 ... <16 page bytes> ...
GET_REPORT Feature 0xF8, 64 bytes
```

- Request bytes 4-6 are `03 01 <page>`; the reply echoes them at offsets 5-7
  and the page payload starts at **offset 0x11 (17)**, 16 bytes long.
  `vv vv vv` are the revision bytes from `Feature 0x01`. Offset 0x30 is `05` or
  `06` on genuine pads and `04` on both counterfeits (meaning unknown).
- Page addresses step in units of `0x10`; every pad tested answers every page
  from `0x00` to `0xF0`. On a genuine DS3: `0x00`-`0x60` hold a monotonic byte
  curve (`d2 d3 d4 ... ff`, most likely a lookup table for the analogue inputs
  or the battery gauge - and identical on all four DS3s, so firmware data rather
  than calibration), `0x70` and `0x80` trim data
  (`0x80` = `03 ff 00 00 ff 44 44 02 65 02 5a`), `0x90` four u16 pairs of stick
  min/max that sum to 1024 on an un-recalibrated pad (`00 5f 03 a1`,
  `00 63 03 9d`, ...), `0xA0` the sensor calibration below, `0xB0` two u16
  (`02 65 02 5a` = 613 / 602 - the same two values that also sit at `0x80`
  offset 8-11), `0xC0` a lone `06` or `03`, and `0xF0` what looks like a
  manufacturing record
  (`20 07 09 03 00 00 22 2f 00 01 00 00 1f ba 95 0b`, plausibly 2007-09-03 plus
  a serial; all zero on DS3-A2 and on both counterfeits). Each counterfeit's
  image is a fixed template with the same structure but pages `0x00`-`0x60` and
  `0xF0` blank (Obigben `0x90` = `00 5c 03 a4` four times, `0xB0` =
  `02 80 02 80`; Fake DS3 `0xB0` = `02 50 02 50`).
- Without a preceding page select, a counterfeit returns page `0xA0`; the genuine
  DS3 returned a buffer whose header reads `03 00 10` with content
  (`00 00 01 64 19 01 00 64 00 01 90 00 19 fe 00`) unlike page `0x10` read via
  `03 01 10`, so request byte 5 (`01` in the PS3 payload) selects something else
  - a different bank or a read width. Leave it at `01`; the PS3 never does a
  plain read.
- `Feature 0xF8` returns whatever is in the same 64-byte device buffer with
  the header replaced by `00 01 00 00` (A1, B1, SIXAXIS all return the last
  `0xEF` answer). The A2 pad and both counterfeits return all zeros. It
  carries no extra information in any capture.
- The live SIXAXIS answered every page from `0x00` to `0xC0` with the same
  structure as the genuine DS3: `0x00`-`0x60` the monotonic byte curve
  (`c3 c4 c6 ... ff`), `0x70` trim data
  (`01 8b bf cf c3 ca 16 00 bf a9 a6 bc a3 be 10 b0`, unlike any DS3),
  `0x80` = `03 ff 00 00 ff 44 44 02 7d 02 78`, `0x90` four u16 pairs
  (`00 85 03 7d`, `00 8e 03 97`, `00 91 03 84`, `00 65 03 74`), `0xA0` the
  calibration block `0209 019b | 0206 0194 | 01f3 0185 | 0201 0075`, `0xB0` =
  `02 7d 02 78`, `0xC0` all zeros (the DS3 has a lone `06` there). Its
  identification report is `00 01 02 00 03 08 01 02 17 17 17 17 09 0a ...` with
  one calibration field, `06`. `0xF2` =
  `f2 ff ff 00 00 19 c1 63 7e a0 00 03 40 80 18 01 8a`, `0xF7` =
  `01 00 7f 02 d9 01 02 ff 14 23`. An earlier probe summary for that run said factory calibration was absent;
  that was a **false negative**: the pad stopped answering partway through the
  page sweep, so the confirmation re-read of `0xA0` failed. The dump now notes
  the re-select error 31 instead of concluding calibration is missing. The probe
  falls back to the swept page (and checks the echoed page number).
- `Feature 0xF7` (read once after the first output report) varies per pad and
  per plug-in; bytes 2-6 look like live sensor/ADC readings
  (`7f 02 ce 01 f1`, `1e 02 fa 01 01`, `fe 02 f8 01 ef`, DS3-A1a
  `04 02 da 01 ee`) followed by `ff 14 33` or `ff 10 90` on DS3-class pads, all
  zero from byte 8 on the A2 sample and mostly zero on the SIXAXIS
  (`0a 01 ea` at 12-14). The live per-pad values are in the
  [pad matrix](#pad-matrix); byte 1 is `00` on every genuine pad except DS3-E
  (`04`) and `01` on both counterfeits. Purpose unknown; not needed for motion.

### Page `0xA0`: sensor calibration

Eight **big-endian** u16 values, four pairs `(zero, oneG)`:

| Page offset | Buffer offset | Field |
| --- | --- | --- |
| 0-1 / 2-3 | 0x11 / 0x13 | accel X: reading at 0 g / reading at -1 g |
| 4-5 / 6-7 | 0x15 / 0x17 | accel Y: same |
| 8-9 / 10-11 | 0x19 / 0x1B | accel Z: same |
| 12-13 / 14-15 | 0x1D / 0x1F | gyro: raw reading at rest / factory **cal byte** (0-255) |

This layout is **verified** against `sixaxis.sys`, which parses the reply with a
4x2 loop reading big-endian u16 starting at buffer offset `0x11` into eight
`int` slots, then uses slot 6 as the gyro zero and slot 7 as the gyro cal byte
(see [What `sixaxis.sys` actually does](#what-sixaxissys-actually-does)). The
naming is a little unfortunate: `oneG` is the raw reading at **-1 g**, and for
the gyro the second element of the pair is not a reading at all but the cal
byte.

Observed:

| Pad | X | Y | Z | Gyro zero / cal | Page `0xB0` (first 2 u16) |
| --- | --- | --- | --- | --- | --- |
| DS3-A1a | 498 / 386 (112) | 494 / 383 (111) | 496 / 387 (109) | 481 / **0** | 613 / 602 |
| DS3-A1b (= A1 PS3 capture) | 508 / 397 (111) | 503 / 387 (116) | 511 / 396 (115) | 521 / **0x77** | 620 / 610 |
| DS3-A2 (= A2 PS3 capture) | 507 / 395 (112) | 510 / 397 (113) | 511 / 400 (111) | 521 / **0x7F** | 620 / 622 |
| DS3-E | 507 / 397 (110) | 516 / 407 (109) | 491 / 379 (112) | 499 / **0** | 622 / 624 |
| DS3 (B1 capture, pcap only) | 508 / 395 (113) | 507 / 398 (109) | 513 / 402 (111) | 488 / **0** | 624 / 615 |
| SIXAXIS-1 (= PS3 capture) | 521 / 411 (110) | 518 / 404 (114) | 499 / 389 (110) | 513 / **0x75** | 637 / 632 |
| SIXAXIS-2 | 514 / 403 (111) | 514 / 400 (114) | 517 / 403 (114) | 512 / **0x7C** | 626 / 625 |
| Obigben aftermarket | 512 / 384 (128) | 512 / 384 (128) | 512 / 384 (128) | 512 / **0** | 640 / 640 |
| Fake DS3 | 512 / 384 (128) | 512 / 384 (128) | 512 / 384 (128) | 512 / **0x7F** | 592 / 592 |

Numbers in parentheses are `zero - oneG`, i.e. counts per g: **109-116 on all
seven genuine pads**, a suspiciously round 128 on both counterfeits, whose
`0xA0` is the fixed template `0200 0180` repeated three times.

Three of these live dumps are byte-identical to rows extracted from the PS3
captures - DS3-A1b to the A1 pad, DS3-A2 to the A2 pad and SIXAXIS-1 to the
SIXAXIS - down to page `0xB0`. Those are the same physical units that produced
the captures, which makes them a clean end-to-end check of both the `tshark`
extractor and the WinUSB probe. (The pad labelled `ds3-cechzc2e-a1` in the
first dumps is **not** the A1 capture pad; see the matrix.)

The rest of page `0xB0` is zero on every pad. Its two values (~590-640) are
unexplained; candidates are gyro sensitivity or temperature compensation. On
every pad, genuine or not, the same two values are repeated at page `0x80`
offset 8-11, which argues for them being a sensor property rather than
gyro-specific. On all seven genuine pads the two values *differ* slightly
(613/602, 620/610, ...), while both counterfeits report them exactly equal
(640/640 and 592/592) - a template tell rather than a measurement.
**`sixaxis.sys` never reads page `0xB0` at all** (it issues exactly one page
select, for `0xA0`), so whatever it holds is not needed for motion.

Two neighbouring pages turned out to be identifiable and are worth recording so
that nobody else has to wonder about them:

- **Pages `0x00`-`0x60`** hold a monotonic byte curve that is *identical across
  every pad of the same class* and therefore firmware data, not per-unit
  calibration: all four DS3s start `d2 d3 d4 d5 ...`, both SIXAXIS units start
  `c3 c4 c6 c7 ...`, and both counterfeits return all zeros. That makes the
  first byte of page `0x00` a better DS3-vs-SIXAXIS discriminator than the
  `Feature 0x01` type bytes, which SIXAXIS-2 gets "wrong" (see the matrix).
- **Page `0x90`** is four big-endian u16 pairs of analog-stick limits: on a pad
  whose sticks have not been re-calibrated the pairs sum to exactly 1024
  (e.g. `(95, 929)`, `(99, 925)`), i.e. symmetric min/max about 512. The two
  most-used pads (DS3-A2, SIXAXIS-1) deviate from 1024, the Obigben repeats one
  template pair, and the Fake DS3's pairs are asymmetric junk. Nothing to do
  with motion.

Note on lewy20041's parsing: he reads little-endian u16 at buffer offsets 20,
22, ..., 32. Offset 20-21 happens to be the low byte of X.oneG followed by the
high byte of Y.zero, which decodes "correctly" only by accident for the values
seen here, and his "gyro offset" at 32-33 is really the cal byte. Use the
layout above.

## Pad matrix

Ten successful WinUSB probe runs over eight physical pads (2026-09-06, dumps in
[`research/ds3-motion/dumps/`](../research/ds3-motion/dumps/)). "Path" is the gyro path
`sixaxis.sys` would take for that pad, derived from the `Feature 0x01`
calibration field list exactly as in [Gyroscope](#gyroscope).

| Pad | `Feature 0x01` fw / type | Cal fields | Path | `0xA0` gyro zero / cal | Idle gyro raw | Accel spans (X/Y/Z) | `0xB0` |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **DS3-A1a** | `04 00 08` / `18` x4 | `00 01 02` | `PLAIN_ZERO` | 481 / `0x00` | 482.5 | 112 / 111 / 109 | 613 / 602 |
| **DS3-A1b** | `04 00 08` / `18` x4 | `00 01 02 07` | `HW_CAL` | 521 / `0x77` | 491.6 | 111 / 116 / 115 | 620 / 610 |
| **DS3-A2** | `04 00 0b` / `18` x4 | `00 01 02 07` | `HW_CAL` | 521 / `0x7F` | **361.4** | 112 / 113 / 111 | 620 / 622 |
| **DS3-E** | `04 00 06` / `18` x4 | `00 01 02` | `PLAIN_ZERO` | 499 / `0x00` | 498.4 | 110 / 109 / 112 | 622 / 624 |
| **SIXAXIS-1** | `02 00 03` / `17` x4 | `06` | `SIXAXIS` | 513 / `0x75` | n/a | 110 / 114 / 110 | 637 / 632 |
| **SIXAXIS-2** | `03 00 04` / **`18` x4** | `06` | `SIXAXIS` | 512 / `0x7C` | n/a | 111 / 114 / 114 | 626 / 625 |
| **Obigben** | `03 00 05` / `18` x4 | `01 02` | `PLAIN_ZERO` | 512 / `0x00` | 500 (frozen) | 128 / 128 / 128 | 640 / 640 |
| **Fake DS3** | `03 00 05` / `18` x4 | `01 02` | `PLAIN_ZERO` | 512 / `0x7F` | 512 (frozen) | 128 / 128 / 128 | 592 / 592 |

Identities, so the same pad is not counted twice: DS3-A1a is the unit dumped
twice (16:14 and 17:17) and is the "live DS3" of the earlier sections;
DS3-A1b, DS3-A2 and SIXAXIS-1 are the same physical units that produced the A1,
A2 and SIXAXIS PS3 captures; the Obigben is the pad earlier called just
"clone". The `Feature 0xF2` Bluetooth addresses separate all eight.

Supporting bytes:

| Pad | `Feature 0xF2` (BD addr, firmware) | `Feature 0xF7` first 11 bytes | `0xF0` (manufacturing record) | `0x00` curve |
| --- | --- | --- | --- | --- |
| DS3-A1a | `e0:ae:5e:72:8c:62`, `00 03 50 81 d8 01 8a 13` | `01 00 0f 02 d9 01 ee ff 10 90 00` | `20 07 09 03 ... 22 2f 00 01` | `d2 d3 d4 ...` |
| DS3-A1b | `00:07:04:14:49:51`, `00 03 50 81 d8 01 8a 13` | `01 00 06 02 d0 01 ee ff 10 90 00` | `20 07 09 03 ... 22 2f 00 01` | `d2 d3 d4 ...` |
| DS3-A2 | `ac:7a:4d:28:19:ac`, `00 03 55 43 c3 01 8a 00` | `01 00 08 02 dc 01 ee ff 00 00 00` | all zero | `d2 d3 d4 ...` |
| DS3-E | `00:26:43:c3:3e:01`, `00 03 50 81 d8 01 8a 13` | `01 04 20 02 c8 01 ee ff 14 93 01` | `20 07 09 03 ... 22 2f 00 01` | `d2 d3 d4 ...` |
| SIXAXIS-1 | `00:19:c1:63:7e:a0`, `00 03 40 80 18 01 8a 00` | `01 00 7f 02 d9 01 02 ff 14 23 00` | all zero | `c3 c4 c6 ...` |
| SIXAXIS-2 | `00:1b:fb:72:3d:d6`, `00 03 50 81 d8 01 8a 13` | `01 00 1d 02 cb 01 ee ff 10 84 00` | `20 07 03 01 ... 22 0f 00 00` | `c3 c4 c6 ...` |
| Obigben | `00:1b:fb:18:ab:29`, `00 03 50 89 d8 01 8a 09` | `02 01 fe 02 f8 01 ef ff 14 33 00` | all zero | all zero |
| Fake DS3 | `a0:5a:5c:5c:56:06`, `00 03 50 81 d8 01 8a 00` | `02 01 fb 02 00 02 05 ff 14 33 00` | all zero | all zero |

What this set actually settles:

- **The gyro path must be read from the calibration field list, never from the
  type bytes.** SIXAXIS-2 reports DS3-class type bytes (`18 18 18 18`) but a
  single calibration field `06`, so `sixaxis.sys` gives it the *SIXAXIS* gyro
  path and puts its cal byte at output `[3]/[4]`. Its page `0x00` curve and its
  2007-03-01 manufacturing record agree with the SIXAXIS, not the DS3s, so this
  is SIXAXIS-family hardware advertising a DS3 type. It is the first observed
  pad where the two indicators disagree, and it invalidates any
  `type == 0x17 ? sixaxis : ds3` shortcut.
- **Field `0x07` does not follow the revision bytes.** DS3-A1a and DS3-A1b both
  report firmware `04 00 08` and both are labelled CECHZC2E-A1, yet only A1b
  lists field `0x07`. The four genuine DS3s split 2:2 on this flag, so both DS3
  gyro paths occur within a single model label and neither can be treated as
  the default; adding the two SIXAXIS units puts all three paths in this one
  sample.
- **Both `PLAIN_ZERO` pads' EEPROM gyro zero is accurate; both `HW_CAL` pads'
  is not.** DS3-A1a idles at 482.5 against an EEPROM zero of 481 and DS3-E at
  498.4 against 499, so Sony's plain path lands within ~1.5 counts of 512 with
  no tracker at all. DS3-A1b idles 29 counts below its EEPROM zero and DS3-A2 a
  full **160 counts** below - with the factory cal byte applied in both cases.
  That is the concrete reason the `HW_CAL` path exists: on those pads the
  recorded zero is stale, and only the tracker recovers the centre.
- **The counterfeits' EEPROM is a template, so the `zero == oneG` guard does
  *not* fire on them.** Both return `0200 0180` (512/384) per axis, which is a
  valid, non-degenerate pair, so Sony's formula runs and yields 128 counts/g
  instead of 113. The Obigben's accelerometer is real and tracks all six
  orientations, and because its firmware scales to match its own template the
  calibrated output still comes out near +-113 per g. The Fake DS3 has **no
  sensors at all**: X/Y/Z/gyro are frozen at 512/512/384/512 in every
  orientation. A `zero == oneG` fallback therefore protects nobody here - the
  check that actually catches these pads is that the sensors never change.
- **Report rate is a clone tell.** The genuine pads deliver ~101 reports/s; the
  Fake DS3 delivers ~920/s (1846 reports in a 2 s window).

## Accelerometer calibration

This is now **settled** by `sixaxis.sys`, which calibrates all three axes in one
loop with a single gain of `0x71` = 113:

```c
// per axis i = 0,1,2 over the report's three big-endian u16
raw = bswap16(report[41 + 2*i]);
if (zero[i] != oneG[i])                                  // guard: clone/blank EEPROM
{
    t   = ((raw - zero[i]) * 1024 / (zero[i] - oneG[i])) * 113;
    cal = ((t + ((t >> 31) & 0x3FF)) >> 10) + 512;        // /1024 rounding toward zero
}
else
    cal = raw;                                           // pass the raw value through
if (i == 0) cal = 0x3FF - cal;                           // X is mirrored, AFTER calibration
```

So the calibrated scale is **113 counts per g centred on 512** (0 g = 512,
+1 g = 625, -1 g = 399). Lying flat, Z therefore reads ~399 on a genuine pad
and on the clone alike, which the probe confirms (`cal Z 399` for raw 383 with
512/384). Three details worth having:

- The `zero - oneG` divisor is the "unknown `dword_0x0`" RPCS3 was missing, and
  `zero` is its unknown `dword_0xC`.
- **There is no 2x gain.** RPCS3's commented-out `polish_value` calls pass
  `(226, -226)`, `(226, 226)` and `(113, 113)` as (divisor, gain) - every pair
  reduces to a ratio of +-1, so those calls do nothing but mirror X. They carry
  no information about the real gain, which is 113 against a `zero - oneG`
  divisor. ScpToolkit's 113 is correct.
- `sixaxis.sys` mirrors X as `0x3FF - cal` **after** calibrating, and does
  **not** clamp the accelerometer result to 0-1023 (only the gyro is clamped),
  so a hard enough knock can push it outside 10 bits. RPCS3's sketch clamps.
  One dump shows this incidentally: on the SIXAXIS-2 run the pad died before
  streaming, the probe averaged raw 0, and the formula returned `cal X -11` -
  a negative accelerometer value, exactly as the unclamped arithmetic predicts.

The formula was re-checked against **every** pose of **every** pad (42
orientation samples, 126 axis values, script `analysis/Check-Accel.ps1` in the
R&D folder): all of them reproduce the probe's calibrated output exactly, the
only differences being +-1 count where the log prints the raw average to one
decimal. The +-1 g response comes out at 103-119 counts on the six genuine-pad
runs and 109-113 on the Obigben, the spread being how far off level the pad
actually sat rather than any error in the formula. No pad contradicts it.

For a **DS4-style** output (DS4Windows mode) the natural conversion is
`accel_ds4 = (raw - zero) * 8192 / (zero - oneG)` per axis (DS4: 8192 LSB/g).
The DS3 source frame is now known (X towards the left grip, Y towards the
trigger edge, Z down through the buttons - see the orientation table above);
the permutation and signs into the DS4 frame still need a reference DS4
capture to confirm.

## Gyroscope

### Hardware cal byte

The gyro has a hardware zero-rate trim. The host sends a **cal byte** in the
output report and the pad shifts its raw yaw reading by about **26.4 counts
per cal-byte step** (`0x6999` in Q10). That constant is Sony's own - it appears
in `sixaxis.sys` and in `ds3cal.dll` with identical surrounding parameters - and
it is now **confirmed on hardware** (see [Measured cal-byte
sensitivity](#measured-cal-byte-sensitivity) below):

| Pad class | Cal bytes in the 48-byte EP0 output report (no report ID) | With report ID (interrupt OUT) |
| --- | --- | --- |
| DualShock 3 with field `0x07` | `[5] = 0xFF, [6] = calByte` | bytes 6-7 |
| SIXAXIS (`0x17`) | `[3] = 0xFF, [4] = calByte` | bytes 4-5 (overlaps the big-motor slot, which a SIXAXIS does not have) |
| DS3 without field `0x07` (B1, DS3-A1a, DS3-E, both counterfeits) | none | none |

The PS3 captures show exactly this: `ff 77` (A1 pad, `Gyro.oneG = 0x77`),
`ff 7f` (A2 pad, `0x7F`), `ff 75` at bytes 3-4 for the SIXAXIS (`0x75`), and
no cal bytes for the B1 pad whose EEPROM has `0`. In all four captures the
cal byte equals the factory value from page `0xA0` and never changes during the
session (up to 60 s observed), so the initial cal is simply "send the EEPROM
byte back". This refines the note in
[`PS3_USB_STARTUP.md`](PS3_USB_STARTUP.md#output-byte-semantics-from-the-consoles-own-traffic).

`sixaxis.sys` places the byte at exactly those offsets, keyed off the two flags
from `Feature 0x01`: `if (!PLAIN_ZERO) { out[3] = 0xFF; out[4] = cal; }` and
`if (HW_CAL) { out[5] = 0xFF; out[6] = cal; }`, then sends the 48-byte buffer as
`SET_REPORT(Output, report ID 1)` over EP0. Unlike the PS3 captures it *does*
update the byte later: whenever the auto-zero tracker asks for a new one, the
driver stores it under a spin lock for its output-report thread to send. The
captures simply never sat still long enough (or those consoles' pads were
already trimmed) to show a change.

Sending `0x00 0x00` there (what DsHidMini does today) is what the PS3 does for
the B1 class, so it is safe for every pad; it just leaves the gyro on its
untrimmed zero.

#### Measured cal-byte sensitivity

The probe's `--calbyte` mode holds the pad still, steps the cal byte +-2 and
+-4 around the factory value and averages 150 raw reports at each setting. Both
field-`0x07` pads accepted the byte at output `[5]/[6]` and shifted
immediately. DS3-A2 gave a clean run:

| Cal byte | Steps | Raw gyro avg | Shift | Counts / step |
| --- | --- | --- | --- | --- |
| `0x7F` (factory) | 0 | 361.6 | - | - |
| `0x81` | +2 | 415.6 | +53.9 | 26.97 |
| `0x83` | +4 | 468.4 | +106.8 | 26.70 |
| `0x7F` | 0 | 362.2 | +0.5 | - |
| `0x7D` | -2 | 308.7 | -53.0 | 26.48 |
| `0x7B` | -4 | 255.2 | -106.5 | 26.62 |
| `0x7F` | 0 | 362.1 | +0.5 | - |

Mean **26.69 counts/step** (sd 0.21) against Sony's 26.399 - agreement to
1.1 %, and the return-to-`0x7F` rows show a baseline stable to 0.6 counts. The
response is linear and symmetric in both directions.

DS3-A1b's run looks like a contradiction in the raw log (the probe prints
37.16, 30.68, 18.01 and 22.07 counts/step) but is not: that pad's zero was
still drifting upwards when the run started, from 475.3 at the first `0x7F`
sample to 492.2 and 492.5 at the two later ones. Re-deriving the steps against
the settled baseline of 492.35 gives **26.41** (`0x7B`, +4), **26.53** (`0x75`,
-2) and **26.34** (`0x73`, -4); only the +2 point stays contaminated. So both
pads agree with Sony's constant, and the lesson for the probe is that the
baseline has to be re-measured *after* the pad has warmed up.

The related question - whether a pad *without* field `0x07` ignores the bytes -
is still untested, because the probe refuses to run the experiment on those
pads. All four candidates in this set (DS3-A1a, DS3-E and the two
counterfeits) would answer it.

Two consequences fall out of these numbers:

- DS3-A2's factory pair is `(zero 521, cal 0x7F)`, but with `0x7F` applied it
  idles at 361.4 - **160 counts, i.e. 6.0 cal-byte steps, below its recorded
  zero**. Sony's `HW_CAL` path reports `clamp(0x3FF - raw)`, so on a fresh
  connect that pad hands applications `1023 - 361 = 662`: a 150-count yaw bias
  that only disappears once the tracker has stepped the byte up to about
  `0x85`. `Retarget` asks for `(512 - 361) * 1024 / 0x6999` = 5.7 -> 5 steps on
  its first correction, then re-measures, so convergence takes a few 16-sample
  blocks. Any implementation that sends the EEPROM byte and then *stops* will
  leave that bias in place permanently.
- The EEPROM `zero` for the gyro is the reading expected **with the factory cal
  byte applied**, not the untrimmed reading; DS3-A2 is 6 steps of ageing away
  from it and DS3-A1b 1.1 steps, while the two `PLAIN_ZERO` pads (whose cal
  byte is `0`, so nothing is applied) sit within 1.5 counts of theirs.

### Auto-zero algorithm (`sixaxis.sys` / `ds3cal.dll`, clean-room)

`ds3cal.dll` exports `GyroCalCreate/Destroy` (196-byte state on the process
heap), `InitialGyroCal(u16 eepromCal, u16 eepromZero, u8* calByte, state)`,
`RuntimeGyroCal(u16 raw, u16* gyroOut, u8* calByte, state)` (returns 0 **when
the cal byte changed**, -1 otherwise) and `GyroCalStore/Load` (`0xDABE` magic
+ state copy, 0xBC bytes). ScpToolkit calls `InitialGyroCal(Gyro.oneG,
Gyro.zero, ...)`, i.e. `eepromCal` is the cal byte and `eepromZero` the raw
reading it produces at rest.

The same algorithm is in Sony's driver: `sixaxis.sys` builds a 17-field
parameter block that is **identical field-for-field** to the one `ds3cal.dll`'s
`InitialGyroCal` builds (same values, same order, same two `(cal, zero)`
arguments in the same positions), and its per-report code has the same
structure - state copy in, settle counter, `output = clamp(512 - raw + zeroRef)`,
16-sample block tracker, pending-cal jump detector, state copy out. Everything
below is therefore verified against Sony's implementation, not just rajkosto's.

Constants: target 512; step 0x6999 (Q10, 26.4 counts per cal step); settle
32 samples initially and 2 after each later cal change; "moving" if raw > 819
or raw < 204; 16-sample blocks; block is quiet if
`(max-avg)^2 + (avg-min)^2 < 10`; ring of the last 4 quiet block averages,
"at rest" when their range < 4; long-term average over 236 blocks (~38 s at
100 Hz) as a fallback; pending-cal tolerance 100 decaying by 1 per sample.

```c
#define Q10(x) (((x) + (((x) >> 31) & 0x3FF)) >> 10)   // /1024 rounding toward zero

// Decide whether an at-rest average needs a cal-byte change.
bool Retarget(int restAvg, int* zeroRef, int* calByte)
{
    int delta = (512 - restAvg) * 1024;
    if (-0x6999 <= delta && delta <= 0x6999) { *zeroRef = restAvg; return false; }  // < 1 step: software zero only
    int steps = delta / 0x6999;
    *zeroRef = restAvg + Q10(steps * 0x6999);   // predicted raw at rest after the pad applies the new byte
    *calByte += steps;
    return true;
}

InitialGyroCal: calByte = eepromCal; Retarget(eepromZero, &zeroRef, &calByte);
                output = clamp(zeroRef - eepromZero + 512, 0, 1023); settle = 32;

RuntimeGyroCal(raw):
    if (settle--) { lastRaw = raw; return previous output; }
    output = clamp(512 - raw + zeroRef, 0, 1023);        // NOTE the sign flip: 512 = still
    changed = Track(raw);
    if (pending) {                                        // a new cal byte was sent, wait for the pad to apply it
        if (|raw - lastRaw| <= pendingTol) pendingTol -= 1;
        else { apply pending zeroRef/calByte; reset block stats; changed = true; }
    }
    lastRaw = raw;

Track(raw):
    if (raw > 819 || raw < 204) moving = true;
    accumulate 16-sample block (sum/min/max); when full:
        blockAvg = round(sum/16); var = (max-avg)^2 + (avg-min)^2
        every 236 blocks: longAvg = round(sum/236);
            if Retarget(longAvg) needs a step -> store as pending (tolerance 100) else zeroRef = longAvg
        if (moving) { moving = false; return false; }     // discard this block
        if (var < 10) {
            push blockAvg into 4-entry ring; if ring full and range(ring) < 4:
                restAvg = round(ringSum/4); reset long-term;
                changed = Retarget(restAvg, &zeroRef, &calByte);
                if (changed) { reset ring; settle = 2; }
                pending = false; return changed;
            if (pending) { apply pending; reset ring; settle = 2; return true; }
        }
    return false;
```

A full C# re-implementation used to validate this against hardware lives in the
R&D folder (`probe/GyroCal.cs`, see [Tooling](#tooling)). On the Obigben, whose
gyro reads a constant 500, the tracker converged to `zeroRef = 500` after four
quiet blocks and emitted 512 - exactly as designed.

Two traps for a re-implementation, both confirmed in the decompilation:

- The cal byte is tracked as a signed `int` and truncated to `u8` on the way
  out, with **no clamping** at either end - `Retarget` just does
  `calByte += steps`. A pad whose rest value is far from 512 can therefore wrap
  the byte.
- Neither implementation re-zeroes in software while the hardware trim is
  converging; the reported value keeps using the old `zeroRef` until the pad is
  observed to have applied the new byte (the pending/jump detector).

### Units

The gyro's counts-per-degree-per-second are **not** in the EEPROM, not in
`ds3cal.dll` and not in `sixaxis.sys` - none of them scale the gyro, they only
zero and/or mirror it. RPCS3 passes the value through with gain 1. So the scale
had to be measured, and it now has been.

**Method.** Four pads (DS3-A1a, DS3-A1b, DS3-A2, DS3-E) were laid flat and
turned ~90 degrees clockwise as seen from above, paused, then ~90 degrees back,
inside the probe's 5 s yaw window. The CSV streams were segmented offline
(`analysis/Analyze-Yaw4.ps1` in the R&D folder): phase blocks are split on the
>150 ms gaps between probe phases, the gyro zero is taken from the six
*stationary* orientation blocks (~1517 samples, sd 2-13 counts) rather than
from the yaw window itself, and each turn is a maximal run of constant-sign
deviation, integrated as `sum((raw - zero) * dt)`.

**Sign - high confidence.** On all four pads the first turn integrates
**negative**:

| Pad | Turn 1 (clockwise) | Turn 2 (back) |
| --- | --- | --- |
| DS3-A1a | -124.9 counts*s (peak -84) | +82.2 (peak +105) |
| DS3-A1b | -132.9 (peak -130) | +149.5 (peak +145) |
| DS3-A2 | -119.3 (peak -148) | +131.4 (peak +137) |
| DS3-E | -126.2 (peak -183) | +130.6 (peak +143) |

So **the raw yaw value falls below its zero when the pad is turned clockwise
viewed from above**, and rises for counter-clockwise. Because all three
`sixaxis.sys` paths invert the sign (`512 + zeroRef - raw`, or `0x3FF - raw`),
**clockwise-from-above is positive (> 512) in Sony's reported value**. The
16:14 run on DS3-A1a, taken before the direction was prescribed and turned
counter-clockwise first, has the opposite first sign - consistent. The signal
is 80-180 counts against a 2-13 count still-noise, so this is not in doubt.

**Scale - good to about +-15 %.** Dividing each integral by the nominal 90
degrees gives, over the eight turns: 0.914, 1.325, 1.388, 1.402, 1.452, 1.460,
1.477 and 1.662 counts per (deg/s); median **1.43**, mean 1.39 (sd 0.21, 15 %).
The 0.914 outlier is DS3-A1a's return turn, which lasted 1090 ms against 2731
ms for the outbound one and was clearly not brought all the way back; dropping
it gives **1.45 +- 0.11 (7 %)**. Per-pad means are 1.15, 1.39, 1.43 and 1.57,
so the four pads agree within +-15 % and there is no evidence of a per-pad
sensitivity difference in this data.

Take the scale as **~1.4 counts per (deg/s)**, i.e. **~0.7 deg/s per count**,
with the full 10-bit span corresponding to roughly **+-360 deg/s**. That is
consistent with the observed motion: a brisk 90 degree hand turn in 1.7 s peaks
at 130-180 counts, which at 1.4 counts/(deg/s) is 93-130 deg/s - about right
for the wrist, and comfortably clear of saturation.

**Residual uncertainty.** The statistical spread is 7-15 %, but the *systematic*
error is tied entirely to the 90 degrees being eyeballed by hand: if the turns
were really 100 degrees the scale is 1.29, if 80 degrees it is 1.61. The
segmentation also assigns the near-zero tails of each turn to whichever side of
the crossing they fall on, which slightly under-counts long slow turns. Pinning
this below ~5 % needs a turntable or an optical reference, not more hand turns.
Page `0xB0` can now be **ruled out** as the carrier of this scale: its values
span 602-637 across the seven genuine pads with no correlation to the measured
per-pad sensitivity, and `sixaxis.sys` never reads it.

## What `sixaxis.sys` actually does

Decompiled from the signed x64 driver. Offsets are into its device context; the
input report is the 49-byte `GET_REPORT(Input, ID 1)` buffer with the sensors at
byte 41 as documented above.

**Start-up, in order** (all over EP0, all failures abort device start - so a pad
that will not answer `0xEF` never comes up under this driver):

1. optional `SET_REPORT(Feature, 0xF5)` with `01 00 ff ff ff ff ff ff 00...`
   (48 bytes) when the `ClearPairingSetting` registry value under
   `HKLM\SYSTEM\CurrentControlSet\Services\sixaxis\Parameters` is 1.
2. `SET_REPORT(Feature, 0xEF)`, 48 bytes, payload
   `00 00 00 00 03 01 a0 00 00 ... 00` - i.e. select page `0xA0`, byte-for-byte
   the payload seen in the PS3 captures. This is the **only** page select the
   driver ever issues.
3. `GET_REPORT(Feature, 0xEF)`, 64 bytes, then parse eight big-endian u16 from
   buffer offset `0x11` into `(zero, oneG)` x4.
4. `GET_REPORT(Feature, 0x01)`, 49 bytes -> the `DS3_TYPE` / `PLAIN_ZERO` /
   `HW_CAL` flags above. If `!PLAIN_ZERO` or `HW_CAL`, initialise the gyro
   auto-zero state with `InitialGyroCal(cal = slot 7, zero = slot 6)`.
5. `SET_REPORT(Output, ID 1)`, 48 bytes, carrying the gyro cal byte at `[3]/[4]`
   or `[5]/[6]` per the flags (and `0xFF` at `[9]`).
6. `SET_REPORT(Feature, 0xF4)` with `42 0c 00 00` - enable the sensor stream.
   (The probe sends the same four bytes.)

**Per input report:** byte-swap the four big-endian sensor u16, calibrate the
three accelerometer axes with gain 113 and mirror X (see
[Accelerometer calibration](#accelerometer-calibration)), then one of three
gyro paths, selected by the flags:

| Flags | Pads | Reported gyro |
| --- | --- | --- |
| `PLAIN_ZERO` set, `HW_CAL` clear | B1, DS3-A1a, DS3-E, both counterfeits | `clamp(512 + eepromZero - raw)` - plain software zero against the EEPROM value, no tracker |
| `PLAIN_ZERO` clear | SIXAXIS-1, SIXAXIS-2 | `clamp(512 + zeroRef - raw)` with the full auto-zero tracker driving `zeroRef` |
| `HW_CAL` set | DS3-A1b, DS3-A2 | tracker runs (to keep the **hardware** trim converged and re-send cal bytes). `sixaxis.sys` then reports `clamp(0x3FF - raw)`; DsHidMini publishes the tracker's `clamp(512 + zeroRef - raw)` so residuals smaller than one cal-byte step do not leak to consumers |

All three **invert the gyro's sign relative to the raw value**, and the result is
written back into the report as host-order u16.

RPCS3's Windows path (`#ifdef _WIN32`) takes these fields as-is and notes that
Sony's driver already "does the same modification of this value as the PS3" -
which the above confirms for X. Its Linux path (raw hidraw) only mirrors X, as
`512 - (accel_x - 512)` = `1024 - accel_x`; note `sixaxis.sys` uses
`1023 - accel_x` and mirrors the *calibrated* value, so RPCS3's Linux path is
both off by one and uncalibrated. RPCS3 passes the gyro through unchanged on
both platforms, so on Linux it is not zeroed either.

## What DsHidMini does today

- **USB EEPROM read** (`driver/DsMotion.c`, `DsUsb_PrepareHardware`): after
  `0xF2`/`0xF5` and before the first output report, `SET`/`GET Feature 0xEF`
  page `0xA0`. Eight big-endian pairs at offset `0x11` are cached. A failed or
  invalid read does **not** fail device start; the driver keeps nominal
  `zero=512`, `oneG=399` and sets the IPC fallback flag.
- **Canonical processing** on every input report: gain-113 accelerometer
  calibration, X mirrored *after* cal, and one of the three Sony gyro paths
  from the Feature `0x01` field list (`PLAIN_ZERO` / `HW_CAL` / `SIXAXIS`).
  Bluetooth uses the same formulas with the nominal fallback and does **not**
  send hardware cal bytes or attempt `0xEF`.
- **SIXAXIS.SYS-compatible GetFeature** (`DSHM_ProcessHidInputReport`): the
  49-byte feature report now carries the calibrated, host-order Sony values.
  The 12-byte SIXAXIS input report still has no motion fields.
- **Gyro tracker**: `HW_CAL` and `SIXAXIS` USB pads run the Sony auto-zero
  tracker (`research/ds3-motion/probe/GyroCal.cs`). When the cal byte steps,
  the driver re-applies it on the next output report (unified `[5]/[6]` or
  `[3]/[4]`). Sending the factory byte once is not enough. For `HW_CAL`,
  DsHidMini also publishes the tracker's software-zeroed output (same formula
  as `SIXAXIS`) so a rest error below one ~26.4-count cal-byte step — e.g.
  DS3-A1b's 16-count / ~10.7 deg/s leftover — does not appear as yaw bias on
  IPC or SIXAXIS GetFeature.
- **IPC**: the existing 60-byte raw HID slot is unchanged. A third
  allocation-granularity-aligned region publishes `IPC_MOTION_SNAPSHOT_MESSAGE`
  (80 bytes, version 1). `Nefarius.DsHidMini.IPC` maps it when present and
  exposes `GetMotionSnapshot`.
- **ControlApp**: per-device Motion viewer with numeric readout and a
  diagnostic HelixToolkit pose (gravity pitch/roll, integrated yaw).
- **DS4Windows-compatible mode** (`driver/DsHid.c`, `DS3_RAW_TO_DS4WINDOWS_HID_INPUT_REPORT`):
  still leaves the DS4 gyro/accel fields at offsets 13-24 zero. Mapping is
  deferred until the axis permutation is verified.
- `0xF8`/`0xF7` are still not sent. Bluetooth `0xEF` is deferred.

### Discrepancies a driver implementation must resolve

Ordered by how visible they are to an application that expects `sixaxis.sys`:

1. **No calibration at all.** DsHidMini reports raw counts centred on the pad's
   EEPROM `zero` (494/476/396 on DS3-A1a); `sixaxis.sys` reports 113
   counts/g centred on exactly 512. Every consumer scaled against Sony's driver
   (RPCS3 included) is therefore mis-scaled and off-centre per pad. Fix: read
   page `0xA0` at start-up and apply the gain-113 formula, which is verified
   against every pose of all eight pads.
2. **Gyro sign is inverted.** All three `sixaxis.sys` paths report
   `512 + zeroRef - raw` or `0x3FF - raw`; DsHidMini reports `swap16(raw)`. The
   sign is backwards *and* the value is not zeroed, so a still pad reads its
   EEPROM zero (~481 on DS3-A1a, ~361 on DS3-A2) instead of 512. Concretely,
   with the sign now measured: a clockwise turn seen from above must come out
   **above** 512, and DsHidMini currently sends it below.
3. **X is mirrored on the wrong value.** `0x3FF - swap16(X)` mirrors the raw
   reading; Sony mirrors the *calibrated* one (`0x3FF - cal`). These differ once
   calibration exists, so the mirror has to move after the formula, not before.
4. **No `zero == oneG` fallback.** Sony's per-axis guard passes the raw value
   through when the EEPROM pair is degenerate. Any implementation needs the same
   guard, plus a fallback for pads that refuse `0xEF` entirely (Sony's driver
   simply fails device start; DsHidMini must not).
5. **No cal byte and no auto-zero.** Sending `00 00` matches the PS3 only for
   `PLAIN_ZERO` pads. A1/A2-class pads (field `0x07`) expect their EEPROM cal
   byte at output `[5]/[6]`, a SIXAXIS at `[3]/[4]`, and Sony re-sends an updated
   byte whenever the tracker converges on a new one. The 26.4 counts/step
   sensitivity is now measured, so this is directly actionable.
6. **Sending the factory cal byte once is not enough.** On DS3-A2 the EEPROM
   pair is `(521, 0x7F)` but the pad idles at 361 with that byte applied, so
   Sony's `HW_CAL` output `0x3FF - raw` starts 150 counts off centre and only
   the running tracker pulls it in (about 6 cal-byte steps). A driver that reads
   `0xA0`, sends the byte and stops will show a permanent yaw bias on exactly
   the pads that have a hardware trim. `PLAIN_ZERO` pads do not have this
   problem: their EEPROM zero is good to ~1.5 counts.
7. **Per-class behaviour is not just cosmetic.** The three gyro paths are keyed
   off the `Feature 0x01` calibration field list - *not* the type bytes, which
   SIXAXIS-2 gets wrong. Picking one path for all pads will be wrong for the
   others. A field-`0x07` DS3 still needs the hardware cal-byte tracker for
   large trim errors (DS3-A2). `sixaxis.sys` then reports `0x3FF - raw` and
   leaves sub-step residuals in the HID report; DsHidMini publishes
   `512 + zeroRef - raw` so those residuals do not reach consumers.
8. **DS4Windows mode has no motion at all**, and the DS4 frame mapping is still
   only partly verified: the DS3 source frame is known (X to the left grip, Y to
   the trigger edge, Z down through the buttons) and the gyro is now measured at
   ~1.4 counts per deg/s with clockwise-from-above positive, so a DS4 yaw field
   at 16 LSB/deg/s needs about `(512 + zero - raw) * 16 / 1.4`, i.e. a gain near
   11.4. The axis permutation and the remaining signs still need a reference DS4
   capture.
9. **Counterfeits need a behavioural check, not an EEPROM check.** Both pads
   here return a valid-looking `0200 0180` template, so the `zero == oneG`
   guard never fires, yet one has no sensors at all (frozen 512/512/384/512) and
   the other has a dead gyro (frozen 500). Anything that advertises motion to
   applications should notice that the values never change rather than trusting
   page `0xA0`.
10. **Bluetooth is untested.** `sixaxis.sys` does all of this over EP0; the same
    feature reports would have to travel the BthPS3 HID control channel.

## Deferred work

The items below were deliberately left out of the current implementation.

1. Bluetooth `Feature 0xEF` over the BthPS3 HID control channel (untested).
   Wireless pads currently use the documented nominal fallback.
2. DS4Windows motion-field mapping: `accel_ds4 = (raw - zero) * 8192 / (zero - oneG)`
   with the axis permutation checked against a reference DS4; gyro yaw scaled by
   the measured ~1.4 counts per (deg/s) into the DS4 gyro-Y slot (a gain of about
   11.4 for DS4's 16 LSB per deg/s).
3. Counterfeit behavioural detection: treat "this axis has not changed in N
   seconds" as "no sensor". The `zero == oneG` guard remains for a blank EEPROM
   only; both seen counterfeits return a plausible `0200 0180` template.
4. A turntable measurement of absolute gyro scale (currently ~1.4 counts/(deg/s)).

## Status and implementation roadmap

Phase status: **USB path implemented** (driver motion subsystem, IPC snapshot,
ControlApp viewer). Research dumps remain in
[`research/ds3-motion/`](../research/ds3-motion/README.md).

### Hardware verification checklist

On at least one `PLAIN_ZERO` and one `HW_CAL` USB pad:

- Flat rest centres near 512 after calibration; six accel poses match the sign table.
- Clockwise-from-above yaw is positive in the corrected gyro / viewer.
- Tracker-driven cal-byte resend and convergence (`HW_CAL` / `SIXAXIS`).
- IPC snapshot and ControlApp Motion viewer (Recenter, expected yaw drift).
- Disconnect / reconnect, and a failed EEPROM read falling back to 512/399.

### Verified (a driver can rely on these)

- Accel formula: gain `0x71` = 113 over `(zero - oneG)`, X mirrored **after** cal (`0x3FF - cal`), no clamp, `zero == oneG` passthrough. Reproduces all 126 live values. See [Accelerometer calibration](#accelerometer-calibration).
- EEPROM page `0xA0`: eight BE u16 at buffer offset `0x11`; gyro pair = (raw zero, cal byte). Sony reads **only** this page. See [Page 0xA0](#page-0xa0-sensor-calibration).
- Gyro sign: raw **falls** for clockwise-from-above; every `sixaxis.sys` path inverts, so clockwise is **positive (>512)** in Sony's reported value.
- Gyro scale: ~1.4 counts per (deg/s) (~0.7 deg/s per count, ~±360 deg/s FS), ±15% (hand-turned 90°). Page `0xB0` is not the scale.
- Cal byte: 26.4 counts/step (Sony `0x6999` Q10; hardware 26.69 ± 0.21). Placement in the 48-byte EP0 payload (no report ID): `[5]/[6]` if the field list sets `HW_CAL` (contains `0x07`), `[3]/[4]` if it indicates the SIXAXIS path (`PLAIN_ZERO` clear, including SIXAXIS-2). Do not derive that path from type bytes `0x17`. The 49-byte interrupt-OUT report (report ID at `[0]`) is one byte later: bytes 6-7 / 4-5. See [Gyroscope](#gyroscope).
- Three gyro paths from Feature `0x01` **field list**, not type bytes: `PLAIN_ZERO`, `HW_CAL`, `SIXAXIS`. SIXAXIS-2 reports type `18 18 18 18` but takes the SIXAXIS path. Class curve on EEPROM `0x00` (`d2 d3…` DS3, `c3 c4…` SIXAXIS, zeros on fakes) is the better discriminator.
- Counterfeits return a plausible `0200 0180` template so `zero == oneG` never fires. Fake DS3: frozen sensors, ~920 Hz. Obigben: real accel, dead gyro. Detect behaviourally.
- `ds3cal.dll` is a field-for-field port of Sony's tracker (`research/ds3-motion/probe/GyroCal.cs`).

### Implementation checklist (ordered)

Shipped in this pass:

1. USB connect (after `0xF2`/`0xF5`, before first output): `SET Feature 0xEF` page `0xA0` + `GET 0xEF`. Cache the four pairs. Soft-fail to nominal 512/399.
2. Feature `0x01` field list selects `PLAIN_ZERO` / `HW_CAL` / `SIXAXIS`. Cal byte at unified `[5]/[6]` or `[3]/[4]`.
3. Accel gain-113 with post-cal X mirror; `zero == oneG` passthrough. Applied to the SIXAXIS GetFeature report and IPC snapshot, not the 12-byte SIXAXIS input report.
4. Gyro sign inverted; tracker + cal-byte resend on `HW_CAL`/`SIXAXIS` USB pads.
5. IPC third region + `GetMotionSnapshot`; ControlApp Motion viewer.

Still deferred:

6. Counterfeit behavioural detection (frozen sensors). Keep `zero == oneG` as a blank-EEPROM guard only.
7. DS4Windows motion-field mapping. Source frame is known; permutation + remaining signs still need a reference DS4 capture.
8. Bluetooth `0xEF` over the BthPS3 HID control channel (unverified). Absolute gyro scale better than ~1.4 counts/(deg/s).

Headline remaining gaps: Bluetooth EEPROM, DS4 axis mapping, counterfeit freeze detection, and a turntable gyro scale. Historical discrepancies versus pre-#217 DsHidMini are listed under [Discrepancies](#discrepancies-a-driver-implementation-must-resolve).

### Open measurements

| Item | How to get it |
| --- | --- |
| Absolute gyro scale (±15% now) | Turntable / known-rate reference, not more hand turns |
| Cal-byte accepted without field `0x07`? | `--calbyte` on DS3-A1a, DS3-E, Obigben, Fake DS3 (probe currently skips) |
| `[3]/[4]` placement on live SIXAXIS | `--wait` + stream before it dies, or HID path (not WinUSB) |
| SIXAXIS orientation table | Same `--interactive` six poses, needs a live stream |
| DS3-A1b ~17-count warm-up drift | Leave pad still 30 s, log raw G |
| `0xEF` over Bluetooth | Same SET/GET page `0xA0` via BthPS3 |
| Page `0xB0` meaning | Curiosity; unused by Sony |
| DS4 axis permutation | Reference DS4 capture vs known DS3 frame |

### Session log

- 2026-09-06 — pcap mining (`tshark` HID control) of CircumSpector PS3 captures.
- 2026-09-06 — headless Ghidra of `ds3cal.dll`; clean-room `GyroCal.cs`.
- 2026-09-06 — WinUSB probe; genuine DS3 + clone dumps; orientation table.
- 2026-09-06 — Sony `sixaxis.sys` decompiled; formula, `0xA0` layout, three gyro paths verified (`386da4c`).
- 2026-09-06 — eight-pad matrix, measured yaw sign/scale, cal-byte 26.69 (`a8ab908`).
- 2026-09-06 — research tree persisted under `research/ds3-motion/`.

## Open questions

- The **absolute** gyro scale is only good to ~15 %, because the reference
  angle was a hand-turned "about 90 degrees". The sign is settled and the
  magnitude is ~1.4 counts per (deg/s); tightening it needs a turntable or an
  optical reference rather than more hand turns.
- Whether a pad **without** field `0x07` ignores the cal bytes. The probe skips
  its `--calbyte` experiment on those pads, so this is untested; DS3-A1a,
  DS3-E and the two counterfeits would all answer it. The `[3]/[4]` placement
  for the SIXAXIS class is also still unconfirmed on hardware - both SIXAXIS
  units died before streaming - although SIXAXIS-2 makes a DS3-class pad
  available for that test.
- Meaning of page `0xB0` (and of the same pair at `0x80` offset 8-11). Now
  **ruled out** as the gyro deg/s scale, and not used by `sixaxis.sys`, so it is
  curiosity rather than a blocker. The one regularity is that genuine pads have
  the two values slightly unequal and both counterfeits have them equal.
- Orientation/sign table for a SIXAXIS, to confirm it shares the DS3 axis frame.
  Only DS3-class pads have been posed through the six orientations; both
  SIXAXIS units stopped answering before the streaming phase.
- Why DS3-A1b's gyro zero drifted ~17 counts upward during the first ~20 s of
  its session while the other pads were stable to under a count. Warm-up is the
  obvious guess and it is the reason its `--calbyte` numbers needed
  re-deriving, but it was not investigated.
- Does `0xEF` work over Bluetooth (BthPS3 control channel)?
- Do other clone families (Defender BT in DS3 mode, ShanWan) answer `0xEF`
  and with what? Two counterfeits are characterised here; both return the same
  `0200 0180` `0xA0` template but differ in page `0xB0` (592/592 vs 640/640)
  and in the EEPROM gyro cal byte (`0x7F` vs `0x00`), so the template is not
  from a single shared source.
- A SIXAXIS bound to WinUSB stops answering **all** transfers (EP0 and the
  interrupt IN pipe, `ERROR_GEN_FAILURE`) a few seconds after enumeration and
  never recovers - `WinUsb_ResetPipe`, re-selecting the alternate setting and
  disabling autosuspend do not bring it back, and Windows still reports the node
  healthy and in D0. Catching it within that window is the only way to dump it,
  which is what the probe's `--wait` mode does. Whether the pad selectively
  suspends and fails to resume, or something else, was not chased further; it is
  a quirk of the throwaway WinUSB setup, not of DsHidMini.

## Tooling

Reusable pieces are in [`research/ds3-motion/`](../research/ds3-motion/README.md)
(probe, dumps, analysis scripts, pcap extractor, Ghidra export script). How to
bind a pad and run a measurement is in that README. Proprietary binaries
(`ds3cal.dll`, `sixaxis.sys`) and Ghidra project files stay out of the repo.

Private R&D leftovers (decompiled C, one-off patch scripts, the binaries) remain
in `D:\FOSS\DsHidMini-motion-rnd\` and are not required to continue.

- `pcap/Extract-HidControl.ps1` - `tshark`-based extractor that reassembles
  every HID class control transfer (setup + data stage) from the USB
  link-layer pcaps; per-capture `.txt` outputs and `resting_sensor_stats.txt`
  next to it.
- `ghidra/` - `ds3cal.dll` x86/x64 and Sony's `sixaxis.sys`, headless project,
  `ExportDecompiled.java` post-script, decompiler output
  (`ds3cal_*.decompiled.c`, `sixaxis_sys.decompiled.c`) and
  `ds3cal_algorithm.md` with the annotated state layout.
  Run: `analyzeHeadless.bat <abs proj path> <name> -import <abs binary> -scriptPath <abs scripts> -postScript ExportDecompiled.java <abs out.c>`
  (headless Ghidra rejects any path element starting with `.`).
- `probe/` - .NET 10 console driving `winusb.dll` through a thin P/Invoke layer
  (`WinUsbRaw.cs`). `Nefarius.Drivers.WinUSB` was tried first but its
  `USBDevice` constructor eagerly reads string descriptors, which an original
  SIXAXIS answers with a STALL, killing the whole session. For a
  Zadig/WinUSB-bound `054C:0268` it dumps `0x01/0xF2/0xF5/0xF7/0xF8`, every
  `0xEF` page, enables
  streaming (`0xF4 42 0C`), logs raw / SIXAXIS.SYS-style / calibrated values
  side by side to CSV, runs the gyro tracker live, optional `--interactive`
  orientation prompts and `--calbyte` sensitivity experiment, and shuts down
  like the PS3 (`0xF4 42 0B`). `--wait` polls until a pad answers on EP0, which
  is the only way to catch a SIXAXIS (see [Open questions](#open-questions)).
  Two WinUSB gotchas cost a while: the handle is opened `FILE_FLAG_OVERLAPPED`,
  so every transfer needs a real `OVERLAPPED` plus `GetOverlappedResult` (a NULL
  one yields `ERROR_GEN_FAILURE`/`ERROR_NOACCESS`), and WinUSB probes the
  transfer buffer for *write* even on an OUT transfer, so a span backed by a
  read-only static (what a C# collection expression compiles to) fails with
  `ERROR_NOACCESS`. Restore DsHidMini afterwards via Device Manager (uninstall
  the WinUSB device without deleting the driver, rescan).
- `dumps/` - per-controller logs and CSV streams.
- `analysis/` - throwaway PowerShell for the offline passes: `Pad-Matrix.ps1`
  (rebuilds the [pad matrix](#pad-matrix) from the logs so nothing is
  hand-transcribed), `Check-Accel.ps1` (re-derives every calibrated value in
  every log from the raw averages) and `Analyze-Yaw4.ps1` (phase-block split,
  still-zero estimation and per-turn integration). Two traps worth knowing if
  these are ever re-run: the probe's CSV `calByte` column does **not** track
  the `--calbyte` experiment, so the cal-byte plateaus look like 4-second yaw
  turns unless the phase is cut first; and the yaw window is a poor source for
  the still baseline, because on some runs the pad is moving for most of it.
