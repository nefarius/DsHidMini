0x09, 0x66,        /* Usage (Download Force Sample) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_DOWNLOAD_SAMPLE_REPORT_ID,        /*   Report ID () */ \
0x05, 0x01,        /*   Usage Page (Generic Desktop Ctrls) */ \
0x09, 0x30,        /*   Usage (X) */ \
0x09, 0x31,        /*   Usage (Y) */ \
0x15, 0x81,        /*   Logical Minimum (-127) */ \
0x25, 0x7F,        /*   Logical Maximum (127) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0xFF, 0x00,  /*   Physical Maximum (255) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \