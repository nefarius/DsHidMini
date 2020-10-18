0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x89,        /* Usage (PID Block Load Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_BLOCK_LOAD_REPORT_ID,        /*   Report ID () */ \
0x09, 0x22,        /*   Usage (Effect Block Index) */ \
0x25, MAX_EFFECT_BLOCKS,        /*   Logical Maximum () */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, MAX_EFFECT_BLOCKS,        /*   Physical Maximum () */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0xB1, 0x02,        /*   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x8B,        /*   Usage (Block Load Status) */ \
0xA1, 0x02,        /*   Collection (Logical) */ \
0x09, 0x8C,        /*     Usage (Block Load Success) */ \
0x09, 0x8D,        /*     Usage (Block Load Full) */ \
0x09, 0x8E,        /*     Usage (Block Load Error) */ \
0x25, 0x03,        /*     Logical Maximum (3) */ \
0x15, 0x01,        /*     Logical Minimum (1) */ \
0x35, 0x01,        /*     Physical Minimum (1) */ \
0x45, 0x03,        /*     Physical Maximum (3) */ \
0x75, 0x08,        /*     Report Size (8) */ \
0x95, 0x01,        /*     Report Count (1) */ \
0xB1, 0x00,        /*     Feature (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /*   End Collection */ \
0x09, 0xAC,        /*   Usage (RAM Pool Available) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x27, 0xFF, 0xFF, 0x00, 0x00,  /*   Logical Maximum (65534) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x47, 0xFF, 0xFF, 0x00, 0x00,  /*   Physical Maximum (65534) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0xB1, 0x00,        /*   Feature (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \