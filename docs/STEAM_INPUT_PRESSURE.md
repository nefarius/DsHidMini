# Steam Input and DualShock 3 pressure buttons

Steam can recognise a DualShock 3 in SXS mode, but Steam Input does not
expose the pad's analog (pressure-sensitive) face, shoulder, or D-pad
buttons as bindable analog sources. That is a Steam / SDL limitation, not
a missing DsHidMini remapping feature.

This note answers three related questions:

1. Which Steam games officially use pressure-sensitive buttons?
2. Why SXS DirectInput sliders are Circle and Cross, not L1 and R1.
3. Whether a "sliders and buttons" HID mode would make Steam Input see
   those pressures.

## Steam games and analog buttons

There is no authoritative Steam catalog of titles that consume
pressure-sensitive face or shoulder buttons.

Standard PC controller APIs used by Steam games — Steam Input, XInput,
GameInput, `SDL_Gamepad`, and Windows.Gaming.Input — model a modern
gamepad: two sticks, two analog triggers, digital buttons, and a hat.
They do not model twelve analog buttons.

Known ways those analog values are actually used on PC:

- Emulators that read the DualShock 3 report directly (RPCS3 via SXS,
  PCSX2 via SDF or its dedicated pad path).
- DirectInput games or launchers that let the player bind any axis,
  including extra sliders.
- Game-specific mods (for example the Metal Gear collection PC ports).

A Steam Input binding cannot invent analog face-button support in a game
that only reads digital buttons or XInput triggers.

## What Steam does with SXS mode

SXS is the mode Steam already treats as a DualShock 3. ControlApp labels
it `SXS (Steam, RPCS3, PCSX2 Qt-Edition)`. The 12-byte DirectInput
joystick report is only half of that compatibility: Steam's SDL HIDAPI
path (`HIDAPI_DriverPS3SonySixaxis` in
[`SDL_hidapi_ps3.c`](https://github.com/libsdl-org/SDL/blob/main/src/joystick/hidapi/SDL_hidapi_ps3.c))
polls the 49-byte feature report that
[`driver/HID.FeatureReport.c`](../driver/HID.FeatureReport.c) serves for
`sixaxis.sys`.

That SDL driver:

- Opens the pad as an 11-button, 1-hat PlayStation 3 gamepad with six
  standard axes (two sticks and two triggers).
- Adds ten extra joystick axes for analog buttons when
  `has_analog_buttons` is true.
- Maps those extra axes from the raw DualShock 3 pressure bytes
  (Cross, Circle, Square, Triangle, L1, R1, and the four D-pad
  directions).

Steam Input's configurator still only offers the standard gamepad
layout. The extra analog-button axes are not surfaced as analog sources
you can bind to a trigger, slider, or mouse axis. Public write-ups of
Steam's DualShock 3 support match that: native recognition, no analog
face buttons, no gyro in the binding UI.

Valve's published Steam Input documentation is aimed at game developers
(action sets, glyphs, official controller types). It does not describe a
HID report that would make Steam Input bind twelve analog buttons. A new
DsHidMini descriptor cannot add a source type Steam Input does not have.

SXS remains the cleanest mode when you want Steam to treat the pad as a
PlayStation controller and you do **not** need pressure in Steam Input.
It also avoids the XInput-mode duplicate-device issue documented in
[STEAM_GAMEINPUT_DUPLICATES.md](STEAM_GAMEINPUT_DUPLICATES.md).

## Pressure axes by HID mode

Values below are the analog HID usages, not the digital button bits.
GPJ and SDF report pressure as `0` released through `255` fully pressed.
SXS inverts the four analog bytes it exposes (`0xFF - raw`) to match
`sixaxis.sys`, so those axes sit near `255` at rest.

DirectInput names such as `S0` / `S1` come from Windows putting HID
`Slider` in `rglSlider[0]` and HID `Dial` in `rglSlider[1]`. They are
not a separate DsHidMini control.

### SXS (one joystick)

Descriptor order in [`driver/HID/03_SXS_Col1_Joystick.h`](../driver/HID/03_SXS_Col1_Joystick.h):
`X`, `Y`, `Z`, `Rz`, `Slider`, `Slider`, `Rx`, `Ry`.

Filled by `DS3_RAW_TO_SIXAXIS_HID_INPUT_REPORT` in
[`driver/DsHid.c`](../driver/DsHid.c):

| HID usage | DualShock 3 source | Notes |
| --- | --- | --- |
| `X` / `Y` | Left stick | |
| `Z` / `Rz` | Right stick | |
| First `Slider` | Circle pressure | Inverted |
| Second `Slider` | Cross pressure | Inverted |
| `Rx` | L2 pressure | Inverted |
| `Ry` | R2 pressure | Inverted |

L1, R1, Square, Triangle, and D-pad pressure are **not** on the
DirectInput joystick. They exist only in the feature report that Steam
and RPCS3 read. The slider pairing is intentional `sixaxis.sys`
compatibility; there is no SXS setting that moves L1/R1 onto those
sliders. The digital button table is in [SIXAXIS.md](SIXAXIS.md).

### GPJ (gamepad + joystick)

Two HID collections. This is the mode that already exposes L1/R1 as
sliders on the same device as the sticks and digital buttons.

**Collection 1 (gamepad)** —
[`driver/HID/02_GPJ_Col1_Gamepad.h`](../driver/HID/02_GPJ_Col1_Gamepad.h),
`DS3_RAW_TO_GPJ_HID_INPUT_REPORT_01`:

| HID usage | DualShock 3 source | Typical DirectInput name |
| --- | --- | --- |
| `X` / `Y` | Left stick | `X` / `Y` |
| `Z` / `Rz` | Right stick | `Z` / `Rz` |
| `Rx` | L2 pressure | `Rx` |
| `Ry` | R2 pressure | `Ry` |
| `Slider` | L1 pressure | `S0` |
| `Dial` | R1 pressure | `S1` |

**Collection 2 (joystick)** —
[`driver/HID/02_GPJ_Col2_Joystick.h`](../driver/HID/02_GPJ_Col2_Joystick.h),
`DS3_RAW_TO_GPJ_HID_INPUT_REPORT_02`:

| HID usage | DualShock 3 source |
| --- | --- |
| `X` | D-pad up pressure |
| `Y` | D-pad right pressure |
| `Z` | D-pad down pressure |
| `Rx` | D-pad left pressure |
| `Ry` | Triangle pressure |
| `Rz` | Circle pressure |
| `Slider` | Cross pressure |
| `Dial` | Square pressure |

GPJ writes these analog bytes regardless of
`PressureExposureMode`. That setting only changes whether digital
buttons and the hat are also reported.

### SDF (single gamepad)

One report. Sticks and L2/R2 match GPJ collection 1. The remaining
pressures are ten `Slider` usages, filled only when
`PressureExposureMode` includes analogue
(`DS3_RAW_TO_SDF_HID_INPUT_REPORT` in [`driver/DsHid.c`](../driver/DsHid.c)):

| Order | DualShock 3 source |
| --- | --- |
| `Rx` / `Ry` | L2 / R2 |
| Slider 1–4 | D-pad up, right, down, left |
| Slider 5–6 | L1, R1 |
| Slider 7–10 | Triangle, Circle, Cross, Square |

### XInput, DS4Windows, and CGP

XInput and DS4Windows expose L2/R2 as the two analog triggers and treat
every other DualShock 3 button as digital. CGP is a DirectInput-friendly
gamepad **without** pressure sliders (see issue
[#68](https://github.com/nefarius/DsHidMini/issues/68)).

None of those modes can feed face-button or L1/R1 pressure into Steam
Input.

## Feeding pressure into Steam Input

Steam Input's analog ceiling for a gamepad is four stick axes plus two
trigger axes. The only way to get DualShock 3 pressure into a Steam
Input binding today is to copy a pressure axis onto one of those six
axes — almost always the triggers — on a virtual Xbox controller.

GPJ already publishes L1/R1 as `Slider`/`Dial` on the gamepad
collection, so it is the usual source. Joystick Gremlin or x360ce can
read those axes and write them to a virtual XInput device (typically
through ViGEm). Steam then sees a normal Xbox pad whose triggers follow
the chosen DualShock 3 buttons.

High-level recipe (menu names change between tool versions; confirm
against the current Joystick Gremlin / x360ce / ViGEm Bus documentation):

1. Set the pad to **GPJ** in ControlApp and apply the profile so the
   HID mode actually restarts.
2. In a DirectInput tester, confirm that L1 moves `Slider`/`S0` and R1
   moves `Dial`/`S1` on the **gamepad** device, not the second
   joystick. Face-button pressure is on that second device.
3. Create a virtual Xbox 360 / Xbox One controller.
4. Map the GPJ slider you care about to the virtual pad's left or right
   trigger. Leave the physical DualShock 3 L2/R2 mapped only if the
   game should still use them.
5. In Steam, enable Steam Input for that **virtual Xbox** controller.
   Hide, ignore, or disable Steam Input on the physical GPJ device so
   the same press is not bound twice.
6. Bind the virtual triggers in the game's Steam Input layout.

This does not make Steam Input "see sliders and buttons." It makes
Steam Input see an Xbox pad whose triggers happen to be driven by
pressure. For a DirectInput-only game (Ace Combat Zero's `.ini` mapper
is a typical example), skip Steam Input and bind GPJ's `S0`/`S1` or
`Rx`/`Ry` in the game itself.

SXS plus a remapper is a poorer fit for this job: the DirectInput
sliders are Circle/Cross, and Steam's native PS3 path still will not
expose the extra analog axes.

## Why a "sliders and buttons" mode would not help Steam

In GPJ or SDF, Steam's generic-controller path maps a standard layout
(two sticks, two triggers, buttons, hat) and drops extra sliders. A
purpose-built HID device that is "only sliders and buttons" would still
be a generic DirectInput gamepad to Steam. Steam Input would not grow
twelve analog-button sources.

Do not remap SXS feature-report pressure bytes to put L1/R1 into the
L2/R2 slots. RPCS3, PCSX2, and Steam's PS3 HIDAPI path all depend on
the `sixaxis.sys` layout staying byte-identical.

## Related

- [SIXAXIS.md](SIXAXIS.md) — SXS digital button table and axis names
- [STEAM_GAMEINPUT_DUPLICATES.md](STEAM_GAMEINPUT_DUPLICATES.md) — one
  XInput-mode pad listed twice in Steam
- [XINPUTHID.md](XINPUTHID.md) — XInput-compatible HID report
- [docs.nefarius.at/projects/DsHidMini](https://docs.nefarius.at/projects/DsHidMini/)
