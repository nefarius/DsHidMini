0x09, 0x74,        /* Usage (Set Ramp Force Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_SET_RAMP_FORCE_REPORT_ID,        /*   Report ID () */ \
0x09, 0x22,        /*   Usage (0x22) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, MAX_EFFECT_BLOCKS,        /*   Logical Maximum () */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, MAX_EFFECT_BLOCKS,        /*   Physical Maximum () */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x75,        /*   Usage (0x75) */ \
0x09, 0x76,        /*   Usage (0x76) */ \
0x16, 0xF0, 0xD8,  /*   Logical Minimum (-10000) */ \
0x26, 0x10, 0x27,  /*   Logical Maximum (10000) */ \
0x36, 0xF0, 0xD8,  /*   Physical Minimum (-10000) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \