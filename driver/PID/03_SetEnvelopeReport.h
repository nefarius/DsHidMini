0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x5A,        /* Usage (Set Envelope Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_SET_ENVELOPE_REPORT_ID,        /*   Report ID () */ \
0x09, 0x22,        /*   Usage (Effect Block Index) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, MAX_EFFECT_BLOCKS,        /*   Logical Maximum () */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, MAX_EFFECT_BLOCKS,        /*   Physical Maximum () */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x5B,        /*   Usage (Attack Level) */ \
0x09, 0x5D,        /*   Usage (Fade Level) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0x10, 0x27,  /*   Logical Maximum (10000) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x5C,        /*   Usage (Attack Time) */ \
0x09, 0x5E,        /*   Usage (Fade Time) */ \
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