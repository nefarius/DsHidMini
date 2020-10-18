0x09, 0x21,        /* Usage (Set Effect Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_SET_EFFECT_REPORT_ID,        /*   Report ID () */ \
0x09, 0x22,        /*   Usage (Effect Block Index) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, MAX_EFFECT_BLOCKS,        /*   Logical Maximum () */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, MAX_EFFECT_BLOCKS,        /*   Physical Maximum () */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x25,        /*   Usage (Effect Type) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x09, 0x26,        /*     Usage (ET Constant Force) */ \
0x09, 0x27,        /*     Usage (ET Ramp ) */ \
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
0x91, 0x00,        /*     Output (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*   End Collection */ \
0x09, 0x50,        /*   Usage (Duration) */ \
0x09, 0x54,        /*   Usage (Trigger Repeat Interval) */ \
0x09, 0x51,        /*   Usage (Sample Period) */ \
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
0x09, 0x52,        /*   Usage (Gain) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x26, 0x10, 0x27,  /*   Logical Maximum (10000) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x46, 0x10, 0x27,  /*   Physical Maximum (10000) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x53,        /*   Usage (Trigger Button) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x26, 0x80, 0x00,  /*   Logical Maximum (128) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x46, 0x80, 0x00,  /*   Physical Maximum (128) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x55,        /*   Usage (Axes Enable) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x05, 0x01,        /*     Usage Page (Generic Desktop Ctrls) */ \
0x09, 0x01,        /*     Usage (Pointer) */ \
0xA1, 0x00,        /*     Collection (Physical) */ \
0x09, 0x30,        /*       Usage (X) */ \
0x09, 0x31,        /*       Usage (Y) */ \
0x15, 0x00,        /*       Logical Minimum (0) */ \
0x25, 0x01,        /*       Logical Maximum (1) */ \
0x35, 0x00,        /*       Physical Minimum (0) */ \
0x45, 0x01,        /*       Physical Maximum (1) */ \
0x75, 0x01,        /*       Report Size (1) */ \
0x95, 0x02,        /*       Report Count (2) */ \
0x91, 0x02,        /*       Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*     End Collection */ \
0xC0,              /*   End Collection */ \
0x05, 0x0F,        /*   Usage Page (PID Page) */ \
0x09, 0x56,        /*   Usage (Direction Enable) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x95, 0x05,        /*   Report Count (5) */ \
0x91, 0x03,        /*   Output (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x57,        /*   Usage (Direction) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x0B, 0x01, 0x00, 0x0A, 0x00,  /*     Usage (0x0A0001) */ \
0x0B, 0x02, 0x00, 0x0A, 0x00,  /*     Usage (0x0A0002) */ \
0x66, 0x14, 0x00,  /*     Unit (System: English Rotation, Length: Centimeter) */ \
0x55, 0xFE,        /*     Unit Exponent */ \
0x15, 0x00,        /*     Logical Minimum (0) */ \
0x27, 0xA0, 0x8C, 0x00, 0x00,  /*     Logical Maximum (35999) */ \
0x35, 0x00,        /*     Physical Minimum (0) */ \
0x47, 0xA0, 0x8C, 0x00, 0x00,  /*     Physical Maximum (35999) */ \
0x66, 0x00, 0x00,  /*     Unit (None) */ \
0x75, 0x10,        /*     Report Size (16) */ \
0x95, 0x02,        /*     Report Count (2) */ \
0x91, 0x02,        /*     Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x55, 0x00,        /*     Unit Exponent (0) */ \
0x66, 0x00, 0x00,  /*     Unit (None) */ \
0xC0,              /*   End Collection */ \
0x05, 0x0F,        /*   Usage Page (PID Page) */ \
0x09, 0xA7,        /*   Usage (Start Delay) */ \
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