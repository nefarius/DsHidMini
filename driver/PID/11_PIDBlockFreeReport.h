0x09, 0x90,        /* Usage (PID Block Free Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_BLOCK_FREE_REPORT_ID,        /*   Report ID () */ \
0x09, 0x22,        /*   Usage (Effect Block Index) */ \
0x25, MAX_EFFECT_BLOCKS,        /*   Logical Maximum () */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, MAX_EFFECT_BLOCKS,        /*   Physical Maximum () */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \