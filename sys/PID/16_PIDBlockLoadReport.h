0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x89,        /* Usage (0x89) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0D,        /*   Report ID (13) */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x25, 0x0A,        /*   Logical Maximum (10) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, 0x0A,        /*   Physical Maximum (10) */ \
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