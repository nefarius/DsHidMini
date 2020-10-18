0x09, 0x7F,        /* Usage (PID Pool Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0D,        /*   Report ID (13) */ \
0x09, 0x80,        /*   Usage (0x80) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x27, 0xFF, 0xFF, 0x00, 0x00,  /*   Logical Maximum (65534) */ \
0x47, 0xFF, 0xFF, 0x00, 0x00,  /*   Physical Maximum (65534) */ \
0xB1, 0x02,        /*   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x83,        /*   Usage (0x83) */ \
0x25, 0x7F,        /*   Logical Maximum (127) */ \
0x45, 0x7F,        /*   Physical Maximum (127) */ \
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