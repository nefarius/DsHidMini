/* https://github.com/Syniurge/ShieldControllerWinDriver/blob/b5c2bfa7b3e86d85c7fd7bac24cb6a4ae6ab5226/sys/hid.c#L109 */
#define FFB_DESCRIPTOR_SEGMENT \
0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x92,        /* Usage (0x92) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x20,        /*   Report ID (32) */ \
0x09, 0x9F,        /*   Usage (0x9F) */ \
0x09, 0xA0,        /*   Usage (0xA0) */ \
0x09, 0xA4,        /*   Usage (0xA4) */ \
0x09, 0xA5,        /*   Usage (0xA5) */ \
0x09, 0xA6,        /*   Usage (0xA6) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x25, 0x01,        /*   Logical Maximum (1) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x45, 0x01,        /*   Physical Maximum (1) */ \
0x75, 0x01,        /*   Report Size (1) */ \
0x95, 0x05,        /*   Report Count (5) */ \
0x81, 0x02,        /*   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x95, 0x03,        /*   Report Count (3) */ \
0x75, 0x01,        /*   Report Size (1) */ \
0x81, 0x03,        /*   Input (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x09, 0x94,        /*   Usage (0x94) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x25, 0x01,        /*   Logical Maximum (1) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x45, 0x01,        /*   Physical Maximum (1) */ \
0x75, 0x01,        /*   Report Size (1) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x81, 0x02,        /*   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x07,        /*   Report Size (7) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x81, 0x02,        /*   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0xC0,              /* End Collection */ \
0x09, 0x21,        /* Usage (0x21) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x21,        /*   Report ID (33) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x25,        /*   Usage (0x25) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x09, 0x26,        /*     Usage (0x26) */ \
0x09, 0x27,        /*     Usage (0x27) */ \
0x09, 0x30,        /*     Usage (0x30) */ \
0x09, 0x31,        /*     Usage (0x31) */ \
0x09, 0x32,        /*     Usage (0x32) */ \
0x09, 0x33,        /*     Usage (0x33) */ \
0x09, 0x34,        /*     Usage (0x34) */ \
0x09, 0x40,        /*     Usage (0x40) */ \
0x09, 0x41,        /*     Usage (0x41) */ \
0x09, 0x42,        /*     Usage (0x42) */ \
0x09, 0x43,        /*     Usage (0x43) */ \
0x09, 0x28,        /*     Usage (0x28) */ \
0x25, 0x0C,        /*     Logical Maximum (12) */ \
0x15, 0x01,        /*     Logical Minimum (1) */ \
0x35, 0x01,        /*     Physical Minimum (1) */ \
0x45, 0x0C,        /*     Physical Maximum (12) */ \
0x75, 0x08,        /*     Report Size (8) */ \
0x95, 0x01,        /*     Report Count (1) */ \
0x91, 0x00,        /*     Output (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*   End Collection */ \
0x09, 0x50,        /*   Usage (0x50) */ \
0x09, 0x54,        /*   Usage (0x54) */ \
0x09, 0x51,        /*   Usage (0x51) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x7F,  /*   Logical Maximum (32767) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0xFF, 0x7F,  /*   Physical Maximum (32767) */ \
0x66, 0x03, 0x10,  /*   Unit (System: English Linear, Time: Seconds) */ \
0x55, 0xFD,        /*   Unit Exponent */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x03,        /*   Report Count (3) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x55, 0x00,        /*   Unit Exponent (0) */ \
0x66, 0x00, 0x00,  /*   Unit (None) */ \
0x09, 0x52,        /*   Usage (0x52) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x53,        /*   Usage (0x53) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x08,        /*   Logical Maximum (8) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x08,        /*   Physical Maximum (8) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x55,        /*   Usage (0x55) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x05, 0x01,        /*     Usage Page (Generic Desktop Ctrls) */ \
0x09, 0x30,        /*     Usage (X) */ \
0x09, 0x31,        /*     Usage (Y) */ \
0x15, 0x00,        /*     Logical Minimum (0) */ \
0x25, 0x01,        /*     Logical Maximum (1) */ \
0x75, 0x01,        /*     Report Size (1) */ \
0x95, 0x02,        /*     Report Count (2) */ \
0x91, 0x02,        /*     Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*   End Collection */ \
0x05, 0x0F,        /*   Usage Page (PID Page) */ \
0x09, 0x56,        /*   Usage (0x56) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x95, 0x05,        /*   Report Count (5) */ \
0x91, 0x03,        /*   Output (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x57,        /*   Usage (0x57) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x0B, 0x01, 0x00, 0x0A, 0x00,  /*     Usage (0x0A0001) */ \
0x0B, 0x02, 0x00, 0x0A, 0x00,  /*     Usage (0x0A0002) */ \
0x66, 0x14, 0x00,  /*     Unit (System: English Rotation, Length: Centimeter) */ \
0x55, 0xFE,        /*     Unit Exponent */ \
0x15, 0x00,        /*     Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /*     Logical Maximum (255) */ \
0x35, 0x00,        /*     Physical Minimum (0) */ \
0x47, 0xA0, 0x8C, 0x00, 0x00,  /*     Physical Maximum (35999) */ \
0x66, 0x00, 0x00,  /*     Unit (None) */ \
0x75, 0x08,        /*     Report Size (8) */ \
0x95, 0x02,        /*     Report Count (2) */ \
0x91, 0x02,        /*     Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x55, 0x00,        /*     Unit Exponent (0) */ \
0x66, 0x00, 0x00,  /*     Unit (None) */ \
0xC0,              /*   End Collection */ \
0x05, 0x0F,        /*   Usage Page (PID Page) */ \
0x09, 0xA7,        /*   Usage (0xA7) */ \
0x66, 0x03, 0x10,  /*   Unit (System: English Linear, Time: Seconds) */ \
0x55, 0xFD,        /*   Unit Exponent */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x7F,  /*   Logical Maximum (32767) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0xFF, 0x7F,  /*   Physical Maximum (32767) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x66, 0x00, 0x00,  /*   Unit (None) */ \
0x55, 0x00,        /*   Unit Exponent (0) */ \
0xC0,              /* End Collection */ \
0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x5A,        /* Usage (0x5A) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x20,        /*   Report ID (32) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x5B,        /*   Usage (0x5B) */ \
0x09, 0x5D,        /*   Usage (0x5D) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x5C,        /*   Usage (0x5C) */ \
0x09, 0x5E,        /*   Usage (0x5E) */ \
0x66, 0x03, 0x10,  /*   Unit (System: English Linear, Time: Seconds) */ \
0x55, 0xFD,        /*   Unit Exponent */ \
0x26, 0xFF, 0x7F,  /*   Logical Maximum (32767) */ \
0x46, 0xFF, 0x7F,  /*   Physical Maximum (32767) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x45, 0x00,        /*   Physical Maximum (0) */ \
0x66, 0x00, 0x00,  /*   Unit (None) */ \
0x55, 0x00,        /*   Unit Exponent (0) */ \
0xC0,              /* End Collection */ \
0x09, 0x5F,        /* Usage (0x5F) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x03,        /*   Report ID (3) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x23,        /*   Usage (0x23) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x25, 0x01,        /*   Logical Maximum (1) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x45, 0x01,        /*   Physical Maximum (1) */ \
0x75, 0x04,        /*   Report Size (4) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x58,        /*   Usage (0x58) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x0B, 0x01, 0x00, 0x0A, 0x00,  /*     Usage (0x0A0001) */ \
0x0B, 0x02, 0x00, 0x0A, 0x00,  /*     Usage (0x0A0002) */ \
0x75, 0x02,        /*     Report Size (2) */ \
0x95, 0x02,        /*     Report Count (2) */ \
0x91, 0x02,        /*     Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*   End Collection */ \
0x15, 0x80,        /*   Logical Minimum (-128) */ \
0x25, 0x7F,        /*   Logical Maximum (127) */ \
0x36, 0xF0, 0xD8,  /*   Physical Minimum (-10000) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x09, 0x60,        /*   Usage (0x60) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x36, 0xF0, 0xD8,  /*   Physical Minimum (-10000) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x09, 0x61,        /*   Usage (0x61) */ \
0x09, 0x62,        /*   Usage (0x62) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x09, 0x63,        /*   Usage (0x63) */ \
0x09, 0x64,        /*   Usage (0x64) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x65,        /*   Usage (0x65) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x6E,        /* Usage (0x6E) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x04,        /*   Report ID (4) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x70,        /*   Usage (0x70) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x6F,        /*   Usage (0x6F) */ \
0x15, 0x80,        /*   Logical Minimum (-128) */ \
0x25, 0x7F,        /*   Logical Maximum (127) */ \
0x36, 0xF0, 0xD8,  /*   Physical Minimum (-10000) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x71,        /*   Usage (0x71) */ \
0x66, 0x14, 0x00,  /*   Unit (System: English Rotation, Length: Centimeter) */ \
0x55, 0xFE,        /*   Unit Exponent */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x47, 0xA0, 0x8C, 0x00, 0x00,  /*   Physical Maximum (35999) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x72,        /*   Usage (0x72) */ \
0x26, 0xFF, 0x7F,  /*   Logical Maximum (32767) */ \
0x46, 0xFF, 0x7F,  /*   Physical Maximum (32767) */ \
0x66, 0x03, 0x10,  /*   Unit (System: English Linear, Time: Seconds) */ \
0x55, 0xFD,        /*   Unit Exponent */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x66, 0x00, 0x00,  /*   Unit (None) */ \
0x55, 0x00,        /*   Unit Exponent (0) */ \
0xC0,              /* End Collection */ \
0x09, 0x73,        /* Usage (0x73) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x05,        /*   Report ID (5) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x70,        /*   Usage (0x70) */ \
0x16, 0x01, 0xFF,  /*   Logical Minimum (-255) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x36, 0xF0, 0xD8,  /*   Physical Minimum (-10000) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x74,        /* Usage (0x74) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x06,        /*   Report ID (6) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x75,        /*   Usage (0x75) */ \
0x09, 0x76,        /*   Usage (0x76) */ \
0x15, 0x80,        /*   Logical Minimum (-128) */ \
0x25, 0x7F,        /*   Logical Maximum (127) */ \
0x36, 0xF0, 0xD8,  /*   Physical Minimum (-10000) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x68,        /* Usage (0x68) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x07,        /*   Report ID (7) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
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
0x09, 0x66,        /* Usage (0x66) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x08,        /*   Report ID (8) */ \
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
0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x77,        /* Usage (0x77) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0A,        /*   Report ID (10) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x78,        /*   Usage (0x78) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x09, 0x79,        /*     Usage (0x79) */ \
0x09, 0x7A,        /*     Usage (0x7A) */ \
0x09, 0x7B,        /*     Usage (0x7B) */ \
0x15, 0x01,        /*     Logical Minimum (1) */ \
0x25, 0x03,        /*     Logical Maximum (3) */ \
0x75, 0x08,        /*     Report Size (8) */ \
0x95, 0x01,        /*     Report Count (1) */ \
0x91, 0x00,        /*     Output (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*   End Collection */ \
0x09, 0x7C,        /*   Usage (0x7C) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0xFF, 0x00,  /*   Physical Maximum (255) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x90,        /* Usage (0x90) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0B,        /*   Report ID (11) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x96,        /* Usage (0x96) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0C,        /*   Report ID (12) */ \
0x09, 0x97,        /*   Usage (0x97) */ \
0x09, 0x98,        /*   Usage (0x98) */ \
0x09, 0x99,        /*   Usage (0x99) */ \
0x09, 0x9A,        /*   Usage (0x9A) */ \
0x09, 0x9B,        /*   Usage (0x9B) */ \
0x09, 0x9C,        /*   Usage (0x9C) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x06,        /*   Logical Maximum (6) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x00,        /*   Output (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x7D,        /* Usage (0x7D) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0D,        /*   Report ID (13) */ \
0x09, 0x7E,        /*   Usage (0x7E) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x6B,        /* Usage (0x6B) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0E,        /*   Report ID (14) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
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
0x09, 0xAB,        /* Usage (0xAB) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x09,        /*   Report ID (9) */ \
0x09, 0x25,        /*   Usage (0x25) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x09, 0x26,        /*     Usage (0x26) */ \
0x09, 0x27,        /*     Usage (0x27) */ \
0x09, 0x30,        /*     Usage (0x30) */ \
0x09, 0x31,        /*     Usage (0x31) */ \
0x09, 0x32,        /*     Usage (0x32) */ \
0x09, 0x33,        /*     Usage (0x33) */ \
0x09, 0x34,        /*     Usage (0x34) */ \
0x09, 0x40,        /*     Usage (0x40) */ \
0x09, 0x41,        /*     Usage (0x41) */ \
0x09, 0x42,        /*     Usage (0x42) */ \
0x09, 0x43,        /*     Usage (0x43) */ \
0x09, 0x28,        /*     Usage (0x28) */ \
0x25, 0x0C,        /*     Logical Maximum (12) */ \
0x15, 0x01,        /*     Logical Minimum (1) */ \
0x35, 0x01,        /*     Physical Minimum (1) */ \
0x45, 0x0C,        /*     Physical Maximum (12) */ \
0x75, 0x08,        /*     Report Size (8) */ \
0x95, 0x01,        /*     Report Count (1) */ \
0xB1, 0x00,        /*     Feature (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*   End Collection */ \
0x05, 0x01,        /*   Usage Page (Generic Desktop Ctrls) */ \
0x09, 0x3B,        /*   Usage (Byte Count) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0x01,  /*   Logical Maximum (511) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0xFF, 0x01,  /*   Physical Maximum (511) */ \
0x75, 0x0A,        /*   Report Size (10) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0xB1, 0x02,        /*   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x75, 0x06,        /*   Report Size (6) */ \
0xB1, 0x01,        /*   Feature (Const,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x89,        /* Usage (0x89) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x20,        /*   Report ID (32) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x25, 0x28,        /*   Logical Maximum (40) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x28,        /*   Physical Maximum (40) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0xB1, 0x02,        /*   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x8B,        /*   Usage (0x8B) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x09, 0x8C,        /*     Usage (0x8C) */ \
0x09, 0x8D,        /*     Usage (0x8D) */ \
0x09, 0x8E,        /*     Usage (0x8E) */ \
0x25, 0x03,        /*     Logical Maximum (3) */ \
0x15, 0x01,        /*     Logical Minimum (1) */ \
0x35, 0x01,        /*     Physical Minimum (1) */ \
0x45, 0x03,        /*     Physical Maximum (3) */ \
0x75, 0x08,        /*     Report Size (8) */ \
0x95, 0x01,        /*     Report Count (1) */ \
0xB1, 0x00,        /*     Feature (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*   End Collection */ \
0x09, 0xAC,        /*   Usage (0xAC) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x27, 0xFF, 0xFF, 0x00, 0x00,  /*   Logical Maximum (65534) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x47, 0xFF, 0xFF, 0x00, 0x00,  /*   Physical Maximum (65534) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0xB1, 0x00,        /*   Feature (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x7F,        /* Usage (0x7F) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x03,        /*   Report ID (3) */ \
0x09, 0x80,        /*   Usage (0x80) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x27, 0xFF, 0xFF, 0x00, 0x00,  /*   Logical Maximum (65534) */ \
0x47, 0xFF, 0xFF, 0x00, 0x00,  /*   Physical Maximum (65534) */ \
0xB1, 0x02,        /*   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x83,        /*   Usage (0x83) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x46, 0xFF, 0x00,  /*   Physical Maximum (255) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0xB1, 0x02,        /*   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0xA9,        /*   Usage (0xA9) */ \
0x09, 0xAA,        /*   Usage (0xAA) */ \
0x75, 0x01,        /*   Report Size (1) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x25, 0x01,        /*   Logical Maximum (1) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x45, 0x01,        /*   Physical Maximum (1) */ \
0xB1, 0x02,        /*   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x75, 0x06,        /*   Report Size (6) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0xB1, 0x03,        /*   Feature (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0xC0,              /* End Collection */ \
0x05, 0x0C,        /* Usage Page (Consumer) */ \
0x09, 0x01,        /* Usage (Consumer Control) */ \
0xA1, 0x01,        /* Collection (Application) */ \
0x85, 0x1E,        /* Report ID (30) */ \
0x15, 0x00,        /* Logical Minimum (0) */ \
0x25, 0x01,        /* Logical Maximum (1) */ \
0x75, 0x03,        /* Report Size (3) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x81, 0x03,        /* Input (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x75, 0x01,        /* Report Size (1) */ \
0x95, 0x02,        /* Report Count (2) */ \
0x09, 0xE9,        /* Usage (Volume Increment) */ \
0x09, 0xEA,        /* Usage (Volume Decrement) */ \
0x81, 0x02,        /* Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x75, 0x03,        /* Report Size (3) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x81, 0x03,        /* Input (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0xA1, 0x01,        /* Collection (Application) */ \
0x19, 0x01,        /*   Usage Minimum (Consumer Control) */ \
0x29, 0x03,        /*   Usage Maximum (Programmable Buttons) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0xFF, 0xFF,  /*   Logical Maximum (-1) */ \
0x95, 0x03,        /*   Report Count (3) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0xC0,              /* End Collection */ \
0x05, 0x01,        /* Usage Page (Generic Desktop Ctrls) */ \
0x09, 0x02,        /* Usage (Mouse) */ \
0xA1, 0x01,        /* Collection (Application) */ \
0x85, 0x02,        /* Report ID (2) */ \
0x09, 0x01,        /* Usage (Pointer) */ \
0xA1, 0x00,        /* Collection (Physical) */ \
0x05, 0x09,        /*   Usage Page (Button) */ \
0x19, 0x01,        /*   Usage Minimum (0x01) */ \
0x29, 0x03,        /*   Usage Maximum (0x03) */ \
0x25, 0x01,        /*   Logical Maximum (1) */ \
0x75, 0x01,        /*   Report Size (1) */ \
0x95, 0x03,        /*   Report Count (3) */ \
0x81, 0x02,        /*   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x05, 0x09,        /*   Usage Page (Button) */ \
0x09, 0x05,        /*   Usage (0x05) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x81, 0x02,        /*   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x75, 0x04,        /*   Report Size (4) */ \
0x81, 0x01,        /*   Input (Const,Array,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x05, 0x01,        /*   Usage Page (Generic Desktop Ctrls) */ \
0x09, 0x30,        /*   Usage (X) */ \
0x09, 0x31,        /*   Usage (Y) */ \
0x15, 0x81,        /*   Logical Minimum (-127) */ \
0x25, 0x7F,        /*   Logical Maximum (127) */ \
0x35, 0x81,        /*   Physical Minimum (-127) */ \
0x45, 0x7F,        /*   Physical Maximum (127) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x81, 0x06,        /*   Input (Data,Var,Rel,No Wrap,Linear,Preferred State,No Null Position) */ \
0xC0,              /* End Collection */ \
0xC0,              /* End Collection */ \
0x06, 0xDE, 0xFF,  /* Usage Page (Vendor Defined 0xFFDE) */ \
0x09, 0x01,        /* Usage (0x01) */ \
0xA1, 0x01,        /* Collection (Application) */ \
0x05, 0xFF,        /* Usage Page (Reserved 0xFF) */ \
0x19, 0x01,        /* Usage Minimum (0x01) */ \
0x29, 0x40,        /* Usage Maximum (0x40) */ \
0x85, 0xFD,        /* Report ID (-3) */ \
0x15, 0x00,        /* Logical Minimum (0) */ \
0x25, 0xFF,        /* Logical Maximum (-1) */ \
0x95, 0x40,        /* Report Count (64) */ \
0x75, 0x08,        /* Report Size (8) */ \
0x81, 0x02,        /* Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0xC0,              /* End Collection */ \
0x06, 0xDE, 0xFF,  /* Usage Page (Vendor Defined 0xFFDE) */ \
0x09, 0x03,        /* Usage (0x03) */ \
0xA1, 0x01,        /* Collection (Application) */ \
0x19, 0x01,        /* Usage Minimum (0x01) */ \
0x29, 0x40,        /* Usage Maximum (0x40) */ \
0x85, 0xFC,        /* Report ID (-4) */ \
0x95, 0x40,        /* Report Count (64) */ \
0x75, 0x08,        /* Report Size (8) */ \
0xB1, 0x02,        /* Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \