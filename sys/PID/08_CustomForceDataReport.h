0x09, 0x68,        /* Usage (Custom Force Data Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0D,        /*   Report ID (13) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x0A,        /*   Logical Maximum (10) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x0A,        /*   Physical Maximum (10) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x6C,        /*   Usage (0x6C) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0x10, 0x27,  /*   Logical Maximum (10000) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x69,        /*   Usage (0x69) */ \
0x15, 0x81,        /*   Logical Minimum (-127) */ \
0x25, 0x7F,        /*   Logical Maximum (127) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0xFF, 0x00,  /*   Physical Maximum (255) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x0C,        /*   Report Count (12) */ \
0x92, 0x02, 0x01,  /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile,Buffered Bytes) */ \
0xC0,              /* End Collection */ \