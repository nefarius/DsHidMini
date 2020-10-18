0x09, 0xAB,        /* Usage (Create New Effect Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_NEW_EFFECT_REPORT_ID,        /*   Report ID () */ \
0x09, 0x25,        /*   Usage (Effect Type) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x09, 0x26,        /*     Usage (ET Constant Force ) */ \
0x09, 0x27,        /*     Usage (ET Ramp) */ \
0x09, 0x30,        /*     Usage (ET Square) */ \
0x09, 0x31,        /*     Usage (ET Sine) */ \
0x09, 0x32,        /*     Usage (ET Triangle) */ \
0x09, 0x33,        /*     Usage (ET Sawtooth Up) */ \
0x09, 0x34,        /*     Usage (ET Sawtooth Down) */ \
0x09, 0x40,        /*     Usage (ET Spring) */ \
0x09, 0x41,        /*     Usage (ET Damper) */ \
0x09, 0x42,        /*     Usage (ET Inertia) */ \
0x09, 0x43,        /*     Usage (ET Friction) */ \
0x09, 0x28,        /*     Usage (ET Custom Force Data) */ \
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