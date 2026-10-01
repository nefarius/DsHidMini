# Steam sees two controllers for one XInput-mode pad

Steam can list one DsHidMini device in XInput HID mode as two controllers and
bind the same input twice (for example a doubled D-pad / POV in desktop mode).
This is a Steam / SDL enumeration bug, not a DsHidMini or BthPS3 defect, and it
is not Bluetooth-specific. See [discussion #580](https://github.com/nefarius/DsHidMini/discussions/580).

## Symptom

- ControlApp shows **one** physical pad.
- Steam Settings > Controller shows **two** "Xbox One Controller" (or similar)
  entries for that pad.
- Desktop-mode bindings fire twice. A two-player game may also treat the pad as
  two players.
- Wireless connections hit the duplicate case more often; USB can do the same.

The driver exposes the same HID child on both transports:
`HID\VID_045E&PID_02FF&IG_00` with inbox `xinputhid.sys` as the upper filter.
Only the container ID differs (BthPS3 nodes often carry the machine-wide
`{00000000-0000-0000-FFFF-FFFFFFFFFFFF}`). Steam / SDL do not use that ID.

## Fingerprint in `controller.txt`

Open `%ProgramFiles(x86)%\Steam\logs\controller.txt` (or
`%ProgramFiles%\Steam\logs\controller.txt`) and look for two `Local Device Found`
blocks of type `045e 02ff` for the same connection. Each block is followed by an
SDL mapping GUID. The last four hex digits are the backend signature:

| GUID suffix | SDL signature | Backend | Typical mapping name |
| --- | --- | --- | --- |
| `...6701` | `0x67` (`'g'`) | GameInput | `Xbox One Controller` |
| `...7801` | `0x78` (`'x'`) | XInput | `*` |

Example (prefixes after `0300` are name-derived and can differ):

```
SDL Mapping for 45e/2ff: 030042d15e040000ff02000000006701,Xbox One Controller,...
SDL Mapping for 45e/2ff: 0300fa675e040000ff02000000007801,*,...
```

If both suffixes appear for one pad, this is the issue.

## Root cause

Steam's controller stack is SDL. With the **Microsoft GameInput** redistributable
installed, recent Steam builds keep both the GameInput and XInput joystick
backends enabled.

SDL only asks *higher-priority* backends whether they already own a device.
GameInput sits above the Windows XInput backend, so GameInput never defers to
XInput. XInput defers to GameInput only when GameInput already claims
`VID_045E` / `PID_02FF`.

Steam still runs the XInput backend, so `SDL_HINT_JOYSTICK_GAMEINPUT` is off.
Until 2026-09-21, SDL's GameInput backend still *added* gamepads in that state
while refusing to claim them against XInput. Arrival order then decides the
count:

1. GameInput first — one Steam controller (XInput backs off).
2. XInput first, GameInput later — two Steam controllers.
3. GameInput never reports — one Steam controller (XInput only).

Bluetooth GameInput notifications often arrive tens of seconds after XInput
(observed 17–45 s), so wireless lands in case 2 more often. USB usually wins
the race, but not always.

The inconsistency is fixed upstream in
[libsdl-org/SDL@c4cfb739](https://github.com/libsdl-org/SDL/commit/c4cfb739)
(2026-09-21, "Prevent GameInput from picking up controllers when
`SDL_HINT_JOYSTICK_GAMEINPUT` is disabled"). Steam client build `1788652215`
(2026-09-05) predates that commit.

On a machine that reproduced #580, `Microsoft GameInput` 3.5.270.0 installed on
2026-09-22 and the first `'g'` GUID appeared at the next Steam restart. Before
that date the same log (back to 2026-06) showed XInput-only entries.

## Workarounds

Until Steam ships an SDL build that includes `c4cfb739`, pick one:

1. **SXS mode** in ControlApp for Steam use. Steam has native DualShock 3
   support in that layout, so the XInput / GameInput overlap does not exist.
   This is the cleanest option.
2. Uninstall **Microsoft GameInput** from Apps & Features. Steam then falls
   back to XInput-only enumeration, as it did before the redistributable
   arrived. A game that depends on GameInput may reinstall it later.
3. In Steam, disable Steam Input for Xbox controllers (Settings > Controller).
   Steam stops binding either entry; XInput games keep talking to the pad
   directly.

Changing DsHidMini serials, container IDs, or HID descriptors will not merge
the two Steam entries. No driver change is planned for this.

## Related

- [XINPUTHID.md](XINPUTHID.md) — XInput-compatible HID report and hardware IDs
- [docs.nefarius.at/projects/DsHidMini](https://docs.nefarius.at/projects/DsHidMini/)
