0x09, 0x6E,        /* Usage (Set Periodic Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0D,        /*   Report ID (13) */ \
0x09, 0x22,        /*   Usage (Effect Block Index) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x0A,        /*   Logical Maximum (10) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x0A,        /*   Physical Maximum (10) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x70,        /*   Usage (Magnitude) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0x10, 0x27,  /*   Logical Maximum (10000) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x6F,        /*   Usage (Offset) */ \
0x16, 0xF0, 0xD8,  /*   Logical Minimum (-10000) */ \
0x26, 0x10, 0x27,  /*   Logical Maximum (10000) */ \
0x36, 0xF0, 0xD8,  /*   Physical Minimum (-10000) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x71,        /*   Usage (Phase) */ \
0x66, 0x14, 0x00,  /*   Unit (System: English Rotation, Length: Centimeter) */ \
0x55, 0xFE,        /*   Unit Exponent */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x27, 0xA0, 0x8C, 0x00, 0x00,  /*   Logical Maximum (35999) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x47, 0xA0, 0x8C, 0x00, 0x00,  /*   Physical Maximum (35999) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x72,        /*   Usage (Period) */ \
0x26, 0xFF, 0x7F,  /*   Logical Maximum (32767) */ \
0x46, 0xFF, 0x7F,  /*   Physical Maximum (32767) */ \
0x66, 0x03, 0x10,  /*   Unit (System: English Linear, Time: Seconds) */ \
0x55, 0xFD,        /*   Unit Exponent */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x66, 0x00, 0x00,  /*   Unit (None) */ \
0x55, 0x00,        /*   Unit Exponent (0) */ \
0xC0,              /* End Collection */ \