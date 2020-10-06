# FFB

## Sources

- [raphnet/gc_n64_usb](https://github.com/raphnet/gc_n64_usb/blob/b52ec2dbaf649edc15727646ad62f1884dd9ad5c/reportdesc.c#L21)
- [tloimu/adapt-ffb-joy](https://github.com/tloimu/adapt-ffb-joy)
- [YukMingLaw/ArduinoJoystickWithFFBLibrary](https://github.com/YukMingLaw/ArduinoJoystickWithFFBLibrary/blob/master/src/FFBDescriptor.h#L6)
- [shauleiz/vJoy](https://github.com/shauleiz/vJoy/blob/2c9a6f14967083d29f5a294b8f5ac65d3d42ac87/driver/sys/hidReportDescSingle.h#L66)
- [Syniurge/ShieldControllerWinDriver](https://github.com/Syniurge/ShieldControllerWinDriver/blob/b5c2bfa7b3e86d85c7fd7bac24cb6a4ae6ab5226/sys/hid.c#L1099)
- [Force Feedback Tutorial: PIC18F4550](https://www.microchip.com/forums/m958105.aspx)

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