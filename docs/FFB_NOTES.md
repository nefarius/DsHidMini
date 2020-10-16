# FFB

## Sources

- [raphnet/gc_n64_usb](https://github.com/raphnet/gc_n64_usb/blob/b52ec2dbaf649edc15727646ad62f1884dd9ad5c/reportdesc.c#L21)
- [tloimu/adapt-ffb-joy](https://github.com/tloimu/adapt-ffb-joy)
- [YukMingLaw/ArduinoJoystickWithFFBLibrary](https://github.com/YukMingLaw/ArduinoJoystickWithFFBLibrary/blob/master/src/FFBDescriptor.h#L6)
- [shauleiz/vJoy](https://github.com/shauleiz/vJoy/blob/2c9a6f14967083d29f5a294b8f5ac65d3d42ac87/driver/sys/hidReportDescSingle.h#L66)
- [Syniurge/ShieldControllerWinDriver](https://github.com/Syniurge/ShieldControllerWinDriver/blob/b5c2bfa7b3e86d85c7fd7bac24cb6a4ae6ab5226/sys/hid.c#L1099)
- [Force Feedback Tutorial: PIC18F4550](https://www.microchip.com/forums/m958105.aspx)
- [USB HID Joystick rumble (ffb)](https://www.microchip.com/forums/m545778.aspx)

## Sniffs/Logs

<pre>
DsHidMini_GetFeature		DsHidMini_GetFeature Entry
DsHidMini_GetFeature		!! Packet->reportId: 7, Packet->reportBufferLen: 5, ReportSize: 32760
DsHidMini_GetFeature		DsHidMini_GetFeature Exit (STATUS_SUCCESS (0x00000000))
DsHidMini_WriteReport		DsHidMini_WriteReport Entry
DsHidMini_WriteReport		!! Packet->reportId: 12, Packet->reportBufferLen: 2, ReportSize: 32760
DsHidMini_WriteReport		DsHidMini_WriteReport Exit
DsHidMini_WriteReport		DsHidMini_WriteReport Entry
DsHidMini_WriteReport		!! Packet->reportId: 13, Packet->reportBufferLen: 2, ReportSize: 32760
DsHidMini_WriteReport		DsHidMini_WriteReport Exit
DsHidMini_SetFeature		DsHidMini_SetFeature Entry
DsHidMini_SetFeature		!! Packet->reportId: 5, Packet->reportBufferLen: 4, ReportSize: 314
DsHidMini_SetFeature		DsHidMini_SetFeature Exit
DsHidMini_GetFeature		DsHidMini_GetFeature Entry
DsHidMini_GetFeature		!! Packet->reportId: 6, Packet->reportBufferLen: 5, ReportSize: 32760
DsHidMini_GetFeature		DsHidMini_GetFeature Exit (STATUS_SUCCESS (0x00000000))
DsHidMini_WriteReport		DsHidMini_WriteReport Entry
DsHidMini_WriteReport		!! Packet->reportId: 12, Packet->reportBufferLen: 2, ReportSize: 32760
DsHidMini_WriteReport		DsHidMini_WriteReport Exit
</pre>

## Documentation

`System\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\VID_054C&PID_0268\OEMForceFeedback\Effects\<GUID>\Attributes` 👉 [is this format](https://docs.microsoft.com/en-us/windows/win32/api/dinputd/ns-dinputd-dieffectattributes)

```cpp
#define DIEFT_ALL                   0x00000000

#define DIEFT_CONSTANTFORCE         0x00000001
#define DIEFT_RAMPFORCE             0x00000002
#define DIEFT_PERIODIC              0x00000003
#define DIEFT_CONDITION             0x00000004
#define DIEFT_CUSTOMFORCE           0x00000005
#define DIEFT_HARDWARE              0x000000FF
#define DIEFT_FFATTACK              0x00000200
#define DIEFT_FFFADE                0x00000400
#define DIEFT_SATURATION            0x00000800
#define DIEFT_POSNEGCOEFFICIENTS    0x00001000
#define DIEFT_POSNEGSATURATION      0x00002000
#define DIEFT_DEADBAND              0x00004000
#define DIEFT_STARTDELAY            0x00008000
#define DIEFT_GETTYPE(n)            LOBYTE(n)
```

```cpp
#define DIEP_DURATION               0x00000001
#define DIEP_SAMPLEPERIOD           0x00000002
#define DIEP_GAIN                   0x00000004
#define DIEP_TRIGGERBUTTON          0x00000008
#define DIEP_TRIGGERREPEATINTERVAL  0x00000010
#define DIEP_AXES                   0x00000020
#define DIEP_DIRECTION              0x00000040
#define DIEP_ENVELOPE               0x00000080
#define DIEP_TYPESPECIFICPARAMS     0x00000100
#if(DIRECTINPUT_VERSION >= 0x0600)
#define DIEP_STARTDELAY             0x00000200
#define DIEP_ALLPARAMS_DX5          0x000001FF
#define DIEP_ALLPARAMS              0x000003FF
#else /* DIRECTINPUT_VERSION < 0x0600 */
#define DIEP_ALLPARAMS              0x000001FF
#endif /* DIRECTINPUT_VERSION < 0x0600 */
#define DIEP_START                  0x20000000
#define DIEP_NORESTART              0x40000000
#define DIEP_NODOWNLOAD             0x80000000
```

As of the example of effect setting, refer to this INF file `C:\Windows\inf\swnt.inf` (pre-installed on WinXP)  or MS SideWinder FFB2. Here is an excerpt from this INF.
<pre>
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C20-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,65,00,02,00,01,00,00,00,ed,01,00,00,cd,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C20-8E33-11D0-9AD0-00A0C9A06E35},,,"Constant Force"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C21-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,6a,00,02,00,02,00,00,00,ef,01,00,00,cf,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C21-8E33-11D0-9AD0-00A0C9A06E35},,,"Ramp"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C22-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,69,00,02,00,03,00,00,00,ef,01,00,00,cf,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C22-8E33-11D0-9AD0-00A0C9A06E35},,,"Square"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C23-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,66,00,02,00,03,00,00,00,ef,01,00,00,cf,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C23-8E33-11D0-9AD0-00A0C9A06E35},,,"Sine"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C24-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,6c,00,02,00,03,00,00,00,ef,01,00,00,cf,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C24-8E33-11D0-9AD0-00A0C9A06E35},,,"Triangle"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C25-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,6e,00,02,00,03,00,00,00,ef,01,00,00,cf,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C25-8E33-11D0-9AD0-00A0C9A06E35},,,"Sawtooth Up"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C26-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,6f,00,02,00,03,00,00,00,ef,01,00,00,cf,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C26-8E33-11D0-9AD0-00A0C9A06E35},,,"Sawtooth Down"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C27-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,01,00,01,00,04,00,00,00,29,01,00,00,09,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C27-8E33-11D0-9AD0-00A0C9A06E35},,,"Spring"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C28-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,03,00,01,00,04,00,00,00,29,01,00,00,09,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C28-8E33-11D0-9AD0-00A0C9A06E35},,,"Damper"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C29-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,05,00,01,00,04,00,00,00,29,01,00,00,09,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C29-8E33-11D0-9AD0-00A0C9A06E35},,,"Inertia"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C2A-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,07,00,01,00,04,00,00,00,29,01,00,00,09,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C2A-8E33-11D0-9AD0-00A0C9A06E35},,,"Friction"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C2B-8E33-11D0-9AD0-
00A0C9A06E35},"Attributes",0x00000001,c9,00,03,00,05,00,00,00,6f,01,00,00,4f,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{13541C2B-8E33-11D0-9AD0-00A0C9A06E35},,,"Custom"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a1-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,09,00,01,00,ff,00,00,00,29,01,00,00,09,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a1-81fa-11d0-94ab-0080c74c7e95},,,"Wall"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a3-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,2d,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a3-81fa-11d0-94ab-0080c74c7e95},,,"RandomNoise"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a4-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,2e,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a4-81fa-11d0-94ab-
0080c74c7e95},,,"AircraftCarrierTakeOff"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a5-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,2f,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a5-81fa-11d0-94ab-0080c74c7e95},,,"BasketballDribble"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a6-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,30,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a6-81fa-11d0-94ab-0080c74c7e95},,,"CarEngineIdle"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a7-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,31,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a7-81fa-11d0-94ab-0080c74c7e95},,,"ChainsawIdle"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a8-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,32,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a8-81fa-11d0-94ab-0080c74c7e95},,,"ChainsawInAction"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a9-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,33,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1a9-81fa-11d0-94ab-0080c74c7e95},,,"DieselEngineIdle"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1aa-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,34,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1aa-81fa-11d0-94ab-0080c74c7e95},,,"Jump"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ab-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,35,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ab-81fa-11d0-94ab-0080c74c7e95},,,"Land"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ac-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,36,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ac-81fa-11d0-94ab-0080c74c7e95},,,"MachineGun"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ad-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,37,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ad-81fa-11d0-94ab-0080c74c7e95},,,"Punched"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ae-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,38,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ae-81fa-11d0-94ab-0080c74c7e95},,,"RocketLaunch"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1af-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,39,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1af-81fa-11d0-94ab-0080c74c7e95},,,"SecretDoor"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b0-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,3a,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b0-81fa-11d0-94ab-0080c74c7e95},,,"SwitchClick"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b1-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,3b,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b1-81fa-11d0-94ab-0080c74c7e95},,,"WindGust"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b2-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,3c,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b2-81fa-11d0-94ab-0080c74c7e95},,,"WindShear"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b3-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,3d,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b3-81fa-11d0-94ab-0080c74c7e95},,,"Pistol"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b4-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,3e,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b4-81fa-11d0-94ab-0080c74c7e95},,,"Shotgun"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b5-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,3f,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b5-81fa-11d0-94ab-0080c74c7e95},,,"Laser1"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b6-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,40,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b6-81fa-11d0-94ab-0080c74c7e95},,,"Laser2"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b7-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,41,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b7-81fa-11d0-94ab-0080c74c7e95},,,"Laser3"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b8-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,42,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b8-81fa-11d0-94ab-0080c74c7e95},,,"Laser4"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b9-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,43,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1b9-81fa-11d0-94ab-0080c74c7e95},,,"Laser5"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ba-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,44,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1ba-81fa-11d0-94ab-0080c74c7e95},,,"Laser6"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1bb-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,45,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1bb-81fa-11d0-94ab-0080c74c7e95},,,"OutOfAmmo"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1bc-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,46,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1bc-81fa-11d0-94ab-0080c74c7e95},,,"LightningGun"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1bd-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,47,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1bd-81fa-11d0-94ab-0080c74c7e95},,,"Missile"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1be-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,48,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1be-81fa-11d0-94ab-0080c74c7e95},,,"GatlingGun"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1bf-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,49,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1bf-81fa-11d0-94ab-0080c74c7e95},,,"ShortPlasma"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c0-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,4a,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c0-81fa-11d0-94ab-0080c74c7e95},,,"PlasmaCannon1"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c1-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,4b,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c1-81fa-11d0-94ab-0080c74c7e95},,,"PlasmaCannon2"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c2-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,4c,01,04,00,ff,00,00,00,ef,00,00,00,cf,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c2-81fa-11d0-94ab-0080c74c7e95},,,"Cannon"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c6-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,00,00,06,00,01,00,00,00,60,01,00,00,40,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c6-81fa-11d0-94ab-0080c74c7e95},,,"Raw Force"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c7-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,00,00,05,00,ff,00,00,00,65,01,00,00,4d,00,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c7-81fa-11d0-94ab-0080c74c7e95},,,"VFX Effect"
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c8-81fa-11d0-94ab-
0080c74c7e95},"Attributes",0x00000001,00,00,07,00,ff,00,00,00,20,01,00,00,00,01,00,00,30,00,00,00
HKLM,%szSWFFPro%\OEMForceFeedback\Effects\{e84cd1c8-81fa-11d0-94ab-0080c74c7e95},,,"RTC Spring"
</pre>

**Delete this entire key** to remove outdated values 👉 `Computer\HKEY_CURRENT_USER\System\CurrentControlSet\Control\MediaProperties`
