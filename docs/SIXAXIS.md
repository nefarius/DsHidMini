# SIXAXIS

## Buttons

| DX | Button   | Byte | Value |
| -- | -------- | ---- | ----- |
| 1  | Triangle | 00   | 01    |
| 2  | Circle   | 00   | 02    |
| 3  | Cross    | 00   | 04    |
| 4  | Square   | 00   | 08    |
| 5  | L2       | 00   | 10    |
| 6  | R2       | 00   | 20    |
| 7  | L1       | 00   | 40    |
| 8  | R1       | 00   | 80    |
| 9  | Select   | 01   | 02    |
| 10 | Start    | 01   | 01    |
| 11 | L3       | 01   | 04    |
| 12 | R3       | 01   | 08    |
| 13 | PS       | 01   | 10    |

## Axes

Left Thumb = X Axis/Y Axis
Right Thumb = Z Axis/Z Rotation
L2 = X Rotation
R2 = Y Rotation

8 = Circle
9 = Cross

## Sensors/Motion/Gyro/Accel

- https://github.com/RPCS3/rpcs3/blob/master/rpcs3/Input/ds3_pad_handler.cpp#L445
- https://github.com/RPCS3/rpcs3/blob/master/rpcs3/Input/ds4_pad_handler.cpp#L824
- https://github.com/nefarius/ScpToolkit/compare/master...rajkosto:master
- https://github.com/rajkosto/ScpToolkit/blob/master/ScpControl/Usb/Ds3/DS3Cal.cs
- https://www.psdevwiki.com/ps3/DualShock_3
- https://mclab.uunyan.com/lab/sixaxis/sxs004.htm
