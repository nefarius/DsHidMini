0x09, 0x5F,        /* Usage (Set Condition Report) */ \
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
0x09, 0x23,        /*   Usage (Parameter Block Offset) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x25, 0x01,        /*   Logical Maximum (1) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x45, 0x01,        /*   Physical Maximum (1) */ \
0x75, 0x04,        /*   Report Size (4) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x58,        /*   Usage (Type Specific Block Offset) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x0B, 0x01, 0x00, 0x0A, 0x00,  /*     Usage (0x0A0001) */ \
0x0B, 0x02, 0x00, 0x0A, 0x00,  /*     Usage (0x0A0002) */ \
0x75, 0x02,        /*     Report Size (2) */ \
0x95, 0x02,        /*     Report Count (2) */ \
0x91, 0x02,        /*     Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*   End Collection */ \
0x16, 0xF0, 0xD8,  /*   Logical Minimum (-10000) */ \
0x26, 0x10, 0x27,  /*   Logical Maximum (10000) */ \
0x36, 0xF0, 0xD8,  /*   Physical Minimum (-10000) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x09, 0x60,        /*   Usage (CP Offset ) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x61,        /*   Usage (Positive Coefficient) */ \
0x09, 0x62,        /*   Usage (Negative Coefficient) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x09, 0x63,        /*   Usage (Positive Saturation ) */ \
0x09, 0x64,        /*   Usage (Negative Saturation) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x65,        /*   Usage (Dead Band) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \