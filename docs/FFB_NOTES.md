# FFB

## Sources

- [raphnet/gc_n64_usb](https://github.com/raphnet/gc_n64_usb/blob/b52ec2dbaf649edc15727646ad62f1884dd9ad5c/reportdesc.c#L21)
- [tloimu/adapt-ffb-joy](https://github.com/tloimu/adapt-ffb-joy)
- [YukMingLaw/ArduinoJoystickWithFFBLibrary](https://github.com/YukMingLaw/ArduinoJoystickWithFFBLibrary/blob/master/src/FFBDescriptor.h#L6)
- [shauleiz/vJoy](https://github.com/shauleiz/vJoy/blob/2c9a6f14967083d29f5a294b8f5ac65d3d42ac87/driver/sys/hidReportDescSingle.h#L66)
- [Syniurge/ShieldControllerWinDriver](https://github.com/Syniurge/ShieldControllerWinDriver/blob/b5c2bfa7b3e86d85c7fd7bac24cb6a4ae6ab5226/sys/hid.c#L1099)
- [Force Feedback Tutorial: PIC18F4550](https://www.microchip.com/forums/m958105.aspx)
- [USB HID Joystick rumble (ffb)](https://www.microchip.com/forums/m545778.aspx)
- [FirstPlatoLV/EmuController](https://github.com/FirstPlatoLV/EmuController/blob/628d077637eec92a91a4ac845dd8b9c2d3851034/inc/Public.h)

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

```c
 public enum DeviceControlEnum
        {
            EnableActuactors = 1,
            DisableActuactors = 2,
            StopAllEffects = 3,
            Reset = 4,
            Pause = 5,
            Continue = 6
        }
```

## Example traces

```text
--- ON
DsHidMini_GetFeature	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	DsHidMini_GetFeature Entry
DsHidMini_GetFeature	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	!! Packet->reportId: 19, Packet->reportBufferLen: 5, id: 1
DsHidMini_GetFeature	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	!! FFB 3
DsHidMini_GetFeature	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	DsHidMini_GetFeature Exit (STATUS_NO_SUCH_DEVICE (0xC000000E))
DsHidMini_WriteReport	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	DsHidMini_WriteReport Entry
DsHidMini_WriteReport	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	!! Packet->reportId: 28, Packet->reportBufferLen: 2
DsHidMini_WriteReport	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	DsHidMini_WriteReport Exit
DsHidMini_WriteReport	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	DsHidMini_WriteReport Entry
DsHidMini_WriteReport	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	!! Packet->reportId: 29, Packet->reportBufferLen: 2
DsHidMini_WriteReport	2020/10/16-23:43:09.020	TRACE_LEVEL_INFORMATION	DsHidMini_WriteReport Exit
DsHidMini_SetFeature	2020/10/16-23:43:09.026	TRACE_LEVEL_INFORMATION	DsHidMini_SetFeature Entry
DsHidMini_SetFeature	2020/10/16-23:43:09.026	TRACE_LEVEL_INFORMATION	!! Packet->reportId: 17, Packet->reportBufferLen: 4
DsHidMini_SetFeature	2020/10/16-23:43:09.026	TRACE_LEVEL_INFORMATION	DsHidMini_SetFeature Exit
DsHidMini_GetFeature	2020/10/16-23:43:09.026	TRACE_LEVEL_INFORMATION	DsHidMini_GetFeature Entry
DsHidMini_GetFeature	2020/10/16-23:43:09.026	TRACE_LEVEL_INFORMATION	!! Packet->reportId: 18, Packet->reportBufferLen: 5, id: 1
DsHidMini_GetFeature	2020/10/16-23:43:09.026	TRACE_LEVEL_INFORMATION	!! FFB 2
DsHidMini_GetFeature	2020/10/16-23:43:09.026	TRACE_LEVEL_INFORMATION	DsHidMini_GetFeature Exit (STATUS_SUCCESS (0x00000000))
--- OFF
DsHidMini_WriteReport	2020/10/16-23:43:11.204	TRACE_LEVEL_INFORMATION	DsHidMini_WriteReport Entry
DsHidMini_WriteReport	2020/10/16-23:43:11.204	TRACE_LEVEL_INFORMATION	!! Packet->reportId: 28, Packet->reportBufferLen: 2
DsHidMini_WriteReport	2020/10/16-23:43:11.204	TRACE_LEVEL_INFORMATION	DsHidMini_WriteReport Exit
```

Example Constant Force test

```text
!! PID_POOL_REPORT_ID
!! DC Reset
!! PID_DEVICE_GAIN_REPORT, DeviceGain: 10000
!! PID_CREATE_NEW_EFFECT_REPORT
!! ET Constant Force
!! PID_BLOCK_LOAD_REPORT_ID
!! PID_SET_CONSTANT_FORCE_REPORT, Magnitude: 0
!! PPID_SET_EFFECT_REPORT, EffectBlockIndex: 1
!! PID_SET_CONSTANT_FORCE_REPORT, Magnitude: 10000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1
!! PID_BLOCK_FREE_REPORT, EffectBlockIndex: 1
!! DC Reset
```

Rumble Racing example

```text
!! PID_POOL_REPORT_ID
!! DC Reset
!! PID_DEVICE_GAIN_REPORT, DeviceGain: 10000
!! PID_CREATE_NEW_EFFECT_REPORT
!! ET Square
!! PID_BLOCK_LOAD_REPORT_ID (EffectBlockIndex: 1)
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 0
!! SET_EFFECT_REPORT, EffectBlockIndex: 1, EffectType: 3, Duration: 1000, TriggerRepeatInterval: 0, SamplePeriod: 0, Gain: 10000, TriggerButton: 255, AxesEnableX: 0, AxesEnableY: 0, DirectionEnable: 1, DirectionInstance1: 9000, DirectionInstance2: 0, StartDelay: 0
!! PID_CREATE_NEW_EFFECT_REPORT
!! ET Sine
!! PID_BLOCK_LOAD_REPORT_ID (EffectBlockIndex: 2)
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 0, Phase: 0, Period: 0
!! SET_EFFECT_REPORT, EffectBlockIndex: 2, EffectType: 4, Duration: 1000, TriggerRepeatInterval: 0, SamplePeriod: 0, Gain: 10000, TriggerButton: 255, AxesEnableX: 0, AxesEnableY: 0, DirectionEnable: 1, DirectionInstance1: 17999, DirectionInstance2: 0, StartDelay: 0
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 7254, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 7254, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1411, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1411, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2274, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2274, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3137, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3137, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1450, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1450, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3137, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3137, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1176, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1176, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3607, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3607, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1529, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1529, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3647, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3647, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4156, Phase: 0, Period: 2000
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4156, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4156, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4039, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4039, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2823, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2823, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2823, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2823, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1254, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1254, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3372, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3372, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3686, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3686, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5686, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5686, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5137, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5137, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 8235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 8235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5843, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5843, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3647, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3647, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3176, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3176, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3764, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3764, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3529, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3529, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1960, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1960, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 392, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 392, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1372, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1372, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4588, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4588, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3254, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3254, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6313, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6313, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4274, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4274, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 666, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 666, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2823, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2823, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3450, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3450, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 156, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 156, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5372, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4352, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4352, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4039, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4039, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4156, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4156, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2588, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2588, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 9215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 9215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 9215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 9215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 9215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 9215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2509, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2509, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4352, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4352, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 8705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 8705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5960, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5960, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4313, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4313, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3333, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3333, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 8784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 8784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 8784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4862, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4862, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3882, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3882, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3372, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3372, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4352, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4352, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2509, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2509, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3686, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3686, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3686, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3686, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3647, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3647, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2862, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2862, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2862, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4823, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4274, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4274, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4039, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4039, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4117, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4117, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4627, Phase: 0, Period: 2000
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4392, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4392, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3803, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4705, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4862, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4862, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4588, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4588, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4117, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4117, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5372, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5372, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4392, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4392, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4078, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2509, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2509, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3254, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3254, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3568, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3568, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2666, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2666, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3764, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3764, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1450, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1450, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1215, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 7019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 7019, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2980, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1411, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1411, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 10000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2862, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 2, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 2, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2862, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1294, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 627, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2784, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 666, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 666, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4117, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4117, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2549, Phase: 0, Period: 2000
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2549, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2235, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1529, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1529, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3725, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2745, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 6000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 7058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 7058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3960, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3960, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2941, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 5058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3647, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3647, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3450, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3450, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 4196, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3411, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3411, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1843, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1843, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3607, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3607, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2039, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2039, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 470, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3058, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 1490, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 3098, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2901, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2000, Phase: 0, Period: 2000
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 2000, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 431, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
!! PID_SET_PERIODIC_REPORT, EffectBlockIndex: 1, Magnitude: 0, Offset: 0, Phase: 0, Period: 2000
!! PID_EFFECT_OPERATION_REPORT, EffectBlockIndex: 1, EffectOperation: 1, LoopCount: 1
```
