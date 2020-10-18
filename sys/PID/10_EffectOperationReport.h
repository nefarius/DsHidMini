0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x77,        /* Usage (Effect Operation Report) */ \
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