0x09, 0x6B,        /* Usage (0x6B) */ \
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
0x09, 0x6D,        /*   Usage (0x6D) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0xFF, 0x00,  /*   Physical Maximum (255) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x51,        /*   Usage (0x51) */ \
0x66, 0x03, 0x10,  /*   Unit (System: English Linear, Time: Seconds) */ \
0x55, 0xFD,        /*   Unit Exponent */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x7F,  /*   Logical Maximum (32767) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0xFF, 0x7F,  /*   Physical Maximum (32767) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x55, 0x00,        /*   Unit Exponent (0) */ \
0x66, 0x00, 0x00,  /*   Unit (None) */ \
0xC0,              /* End Collection */ \