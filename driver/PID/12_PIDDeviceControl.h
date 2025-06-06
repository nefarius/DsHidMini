0x09, 0x96,        /* Usage (PID Device Control) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_DEVICE_CONTROL_REPORT_ID,        /*   Report ID () */ \
0x09, 0x97,        /*   Usage (DC Enable Actuators) */ \
0x09, 0x98,        /*   Usage (DC Disable Actuators) */ \
0x09, 0x99,        /*   Usage (DC Stop All Effects) */ \
0x09, 0x9A,        /*   Usage (DC Device Reset) */ \
0x09, 0x9B,        /*   Usage (DC Device Pause) */ \
0x09, 0x9C,        /*   Usage (DC Device Continue) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x06,        /*   Logical Maximum (6) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x00,        /*   Output (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \