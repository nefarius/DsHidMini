/* https://forums.vigem.org/topic/294/pid-1-0-example-descriptor */
#define FFB_DESCRIPTOR_SEGMENT \
0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x58,        /* Usage (0x58) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x0B, 0x01, 0x00, 0x0A, 0x00,  /*   Usage (0x0A0001) */ \
0x0B, 0x02, 0x00, 0x0A, 0x00,  /*   Usage (0x0A0002) */ \
0x26, 0xFD, 0x7F,  /*   Logical Maximum (32765) */ \
0x75, 0x10,        /*   Report Size (16) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0xC0,              /* End Collection */ \
0x09, 0x5A,        /* Usage (0x5A) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x02,        /* Report ID (2) */ \
0x09, 0x23,        /* Usage (0x23) */ \
0x26, 0xFD, 0x7F,  /* Logical Maximum (32765) */ \
0x75, 0x0F,        /* Report Size (15) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x24,        /* Usage (0x24) */ \
0x25, 0x01,        /* Logical Maximum (1) */ \
0x75, 0x01,        /* Report Size (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x5B,        /* Usage (0x5B) */ \
0x09, 0x5D,        /* Usage (0x5D) */ \
0x26, 0xFF, 0x00,  /* Logical Maximum (255) */ \
0x75, 0x08,        /* Report Size (8) */ \
0x95, 0x02,        /* Report Count (2) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x5C,        /* Usage (0x5C) */ \
0x09, 0x5E,        /* Usage (0x5E) */ \
0x26, 0x10, 0x27,  /* Logical Maximum (10000) */ \
0x46, 0x10, 0x27,  /* Physical Maximum (10000) */ \
0x66, 0x03, 0x10,  /* Unit (System: English Linear, Time: Seconds) */ \
0x55, 0x0D,        /* Unit Exponent (-3) */ \
0x75, 0x10,        /* Report Size (16) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x45, 0x00,        /* Physical Maximum (0) */ \
0x65, 0x00,        /* Unit (None) */ \
0x55, 0x00,        /* Unit Exponent (0) */ \
0xC0,              /* End Collection */ \
0x09, 0x5F,        /* Usage (0x5F) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x03,        /* Report ID (3) */ \
0x09, 0x23,        /* Usage (0x23) */ \
0x26, 0xFD, 0x7F,  /* Logical Maximum (32765) */ \
0x75, 0x0F,        /* Report Size (15) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x24,        /* Usage (0x24) */ \
0x25, 0x01,        /* Logical Maximum (1) */ \
0x75, 0x01,        /* Report Size (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x60,        /* Usage (0x60) */ \
0x09, 0x61,        /* Usage (0x61) */ \
0x09, 0x62,        /* Usage (0x62) */ \
0x09, 0x63,        /* Usage (0x63) */ \
0x09, 0x64,        /* Usage (0x64) */ \
0x09, 0x65,        /* Usage (0x65) */ \
0x26, 0xFF, 0x00,  /* Logical Maximum (255) */ \
0x75, 0x08,        /* Report Size (8) */ \
0x95, 0x06,        /* Report Count (6) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x6E,        /* Usage (0x6E) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x04,        /* Report ID (4) */ \
0x09, 0x23,        /* Usage (0x23) */ \
0x26, 0xFD, 0x7F,  /* Logical Maximum (32765) */ \
0x75, 0x0F,        /* Report Size (15) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x24,        /* Usage (0x24) */ \
0x25, 0x01,        /* Logical Maximum (1) */ \
0x75, 0x01,        /* Report Size (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x70,        /* Usage (0x70) */ \
0x09, 0x6F,        /* Usage (0x6F) */ \
0x09, 0x71,        /* Usage (0x71) */ \
0x26, 0xFF, 0x00,  /* Logical Maximum (255) */ \
0x75, 0x08,        /* Report Size (8) */ \
0x95, 0x03,        /* Report Count (3) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x72,        /* Usage (0x72) */ \
0x26, 0x10, 0x27,  /* Logical Maximum (10000) */ \
0x46, 0x10, 0x27,  /* Physical Maximum (10000) */ \
0x66, 0x03, 0x10,  /* Unit (System: English Linear, Time: Seconds) */ \
0x55, 0x0D,        /* Unit Exponent (-3) */ \
0x75, 0x10,        /* Report Size (16) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x45, 0x00,        /* Physical Maximum (0) */ \
0x65, 0x00,        /* Unit (None) */ \
0x55, 0x00,        /* Unit Exponent (0) */ \
0xC0,              /* End Collection */ \
0x09, 0x73,        /* Usage (0x73) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x05,        /* Report ID (5) */ \
0x09, 0x23,        /* Usage (0x23) */ \
0x26, 0xFD, 0x7F,  /* Logical Maximum (32765) */ \
0x75, 0x0F,        /* Report Size (15) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x24,        /* Usage (0x24) */ \
0x25, 0x01,        /* Logical Maximum (1) */ \
0x75, 0x01,        /* Report Size (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x70,        /* Usage (0x70) */ \
0x26, 0xFF, 0x00,  /* Logical Maximum (255) */ \
0x75, 0x08,        /* Report Size (8) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x74,        /* Usage (0x74) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x06,        /* Report ID (6) */ \
0x09, 0x23,        /* Usage (0x23) */ \
0x26, 0xFD, 0x7F,  /* Logical Maximum (32765) */ \
0x75, 0x0F,        /* Report Size (15) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x24,        /* Usage (0x24) */ \
0x25, 0x01,        /* Logical Maximum (1) */ \
0x75, 0x01,        /* Report Size (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x75,        /* Usage (0x75) */ \
0x09, 0x76,        /* Usage (0x76) */ \
0x26, 0xFF, 0x00,  /* Logical Maximum (255) */ \
0x75, 0x08,        /* Report Size (8) */ \
0x95, 0x02,        /* Report Count (2) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x68,        /* Usage (0x68) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x07,        /* Report ID (7) */ \
0x09, 0x23,        /* Usage (0x23) */ \
0x26, 0xFD, 0x7F,  /* Logical Maximum (32765) */ \
0x75, 0x0F,        /* Report Size (15) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x0B, 0x3B, 0x00, 0x01, 0x00,  /* Usage (0x01003B) */ \
0x26, 0x00, 0x01,  /* Logical Maximum (256) */ \
0x75, 0x09,        /* Report Size (9) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x69,        /* Usage (0x69) */ \
0x26, 0xFF, 0x00,  /* Logical Maximum (255) */ \
0x75, 0x08,        /* Report Size (8) */ \
0x96, 0x00, 0x01,  /* Report Count (256) */ \
0x92, 0x02, 0x01,  /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile,Buffered Bytes) */ \
0xC0,              /* End Collection */ \
0x09, 0x66,        /* Usage (0x66) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x08,        /* Report ID (8) */ \
0x05, 0x01,        /* Usage Page (Generic Desktop Ctrls) */ \
0x09, 0x01,        /* Usage (Pointer) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x09, 0x30,        /*   Usage (X) */ \
0x09, 0x31,        /*   Usage (Y) */ \
0x15, 0x81,        /*   Logical Minimum (-127) */ \
0x25, 0x7F,        /*   Logical Maximum (127) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x02,        /*   Report Count (2) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0xC0,              /* End Collection */ \
0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x6B,        /* Usage (0x6B) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x09,        /* Report ID (9) */ \
0x09, 0x23,        /* Usage (0x23) */ \
0x09, 0x6C,        /* Usage (0x6C) */ \
0x09, 0x6D,        /* Usage (0x6D) */ \
0x15, 0x00,        /* Logical Minimum (0) */ \
0x26, 0xFD, 0x7F,  /* Logical Maximum (32765) */ \
0x95, 0x03,        /* Report Count (3) */ \
0x75, 0x10,        /* Report Size (16) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x77,        /* Usage (0x77) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0A,        /* Report ID (10) */ \
0x09, 0x22,        /* Usage (0x22) */ \
0x25, 0x7F,        /* Logical Maximum (127) */ \
0x75, 0x07,        /* Report Size (7) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x24,        /* Usage (0x24) */ \
0x25, 0x01,        /* Logical Maximum (1) */ \
0x75, 0x01,        /* Report Size (1) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x78,        /* Usage (0x78) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x09, 0x79,        /*   Usage (0x79) */ \
0x09, 0x7A,        /*   Usage (0x7A) */ \
0x09, 0x7B,        /*   Usage (0x7B) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x03,        /*   Logical Maximum (3) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x91, 0x00,        /*   Output (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x7C,        /* Usage (0x7C) */ \
0x15, 0x00,        /* Logical Minimum (0) */ \
0x26, 0xFF, 0x00,  /* Logical Maximum (255) */ \
0x91, 0x02,        /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x7F,        /* Usage (0x7F) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x01,        /* Report ID (1) */ \
0x09, 0x80,        /* Usage (0x80) */ \
0x09, 0x81,        /* Usage (0x81) */ \
0x09, 0x82,        /* Usage (0x82) */ \
0x26, 0xFD, 0x7F,  /* Logical Maximum (32765) */ \
0x95, 0x03,        /* Report Count (3) */ \
0x75, 0x10,        /* Report Size (16) */ \
0xB1, 0x02,        /* Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0xA8,        /* Usage (0xA8) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x09, 0x21,        /*   Usage (0x21) */ \
0x09, 0x5A,        /*   Usage (0x5A) */ \
0x09, 0x5F,        /*   Usage (0x5F) */ \
0x09, 0x6E,        /*   Usage (0x6E) */ \
0x09, 0x73,        /*   Usage (0x73) */ \
0x09, 0x74,        /*   Usage (0x74) */ \
0x09, 0x6B,        /*   Usage (0x6B) */ \
0x26, 0xFF, 0x00,  /*   Logical Maximum (255) */ \
0x75, 0x08,        /*   Report Size (8) */ \
0x95, 0x07,        /*   Report Count (7) */ \
0xB1, 0x02,        /*   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x25, 0x01,        /* Logical Maximum (1) */ \
0x75, 0x07,        /* Report Size (7) */ \
0x95, 0x01,        /* Report Count (1) */ \
0xB1, 0x03,        /* Feature (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0x09, 0x67,        /* Usage (0x67) */ \
0x75, 0x01,        /* Report Size (1) */ \
0xB1, 0x02,        /* Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0x09, 0x92,        /* Usage (0x92) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x02,        /* Report ID (2) */ \
0x09, 0x22,        /* Usage (0x22) */ \
0x25, 0x7F,        /* Logical Maximum (127) */ \
0x75, 0x07,        /* Report Size (7) */ \
0x81, 0x02,        /* Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x09, 0x24,        /* Usage (0x24) */ \
0x25, 0x01,        /* Logical Maximum (1) */ \
0x75, 0x01,        /* Report Size (1) */ \
0x95, 0x01,        /* Report Count (1) */ \
0x81, 0x02,        /* Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x09, 0x94,        /* Usage (0x94) */ \
0x09, 0xA0,        /* Usage (0xA0) */ \
0x09, 0xA4,        /* Usage (0xA4) */ \
0x09, 0xA6,        /* Usage (0xA6) */ \
0x75, 0x01,        /* Report Size (1) */ \
0x95, 0x04,        /* Report Count (4) */ \
0x81, 0x02,        /* Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x81, 0x03,        /* Input (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0xC0,              /* End Collection */ \
0x09, 0x95,        /* Usage (0x95) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0B,        /* Report ID (11) */ \
0x09, 0x96,        /* Usage (0x96) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x09, 0x97,        /*   Usage (0x97) */ \
0x09, 0x98,        /*   Usage (0x98) */ \
0x09, 0x99,        /*   Usage (0x99) */ \
0x09, 0x9A,        /*   Usage (0x9A) */ \
0x09, 0x9B,        /*   Usage (0x9B) */ \
0x09, 0x9C,        /*   Usage (0x9C) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, 0x06,        /*   Logical Maximum (6) */ \
0x75, 0x01,        /*   Report Size (1) */ \
0x95, 0x08,        /*   Report Count (8) */ \
0x91, 0x02,        /*   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \
0xC0,              /* End Collection */ \
0x09, 0x85,        /* Usage (0x85) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x0C,        /* Report ID (12) */ \
0x09, 0x86,        /* Usage (0x86) */ \
0x09, 0x87,        /* Usage (0x87) */ \
0x09, 0x88,        /* Usage (0x88) */ \
0x26, 0xFF, 0x7F,  /* Logical Maximum (32767) */ \
0x75, 0x10,        /* Report Size (16) */ \
0x95, 0x03,        /* Report Count (3) */ \
0x92, 0x02, 0x01,  /* Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile,Buffered Bytes) */ \
0xC0,              /* End Collection */ \
0x09, 0x7D,        /* Usage (0x7D) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, 0x02,        /* Report ID (2) */ \
0x09, 0x7E,        /* Usage (0x7E) */ \
0x26, 0xFF, 0x00,  /* Logical Maximum (255) */ \
0x75, 0x08,        /* Report Size (8) */ \
0x95, 0x01,        /* Report Count (1) */ \
0xB1, 0x02,        /* Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile) */ \
0xC0,              /* End Collection */ \