0x09, 0x68,        /* Usage (Custom Force Data Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_SET_CUSTOM_FORCE_DATA_REPORT_ID,        /*   Report ID () */ \
0x09, 0x22,        /*   Usage (Effect Block Index) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, MAX_EFFECT_BLOCKS,        /*   Logical Maximum () */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, MAX_EFFECT_BLOCKS,        /*   Physical Maximum () */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x6C,        /*   Usage (Custom Force Data Offset) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0x10, 0x27,  /*   Logical Maximum (10000) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x69,        /*   Usage (Custom Force Data) */ \
0x15, 0x81,        /*   Logical Minimum (-127) */ \
0x25, 0x7F,        /*   Logical Maximum (127) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0xFF, 0x00,  /*   Physical Maximum (255) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x0C,        /*   Report Count (12) */ \
0x92, 0x02, 0x01,  /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile,Buffered Bytes) */ \
0xC0,              /* End Collection */ \