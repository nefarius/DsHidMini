0x09, 0x7D,        /* Usage (Device Gain Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_DEVICE_GAIN_REPORT_ID,        /*   Report ID () */ \
0x09, 0x7E,        /*   Usage (Device Gain) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0x10, 0x27,  /*   Logical Maximum (10000) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \