0x05, 0x0F,        /* Usage Page (PID Page) */ \
0x09, 0x92,        /* Usage (PID State Report) */ \
0xA1, 0x02,        /* Collection (Logical) */ \
0x85, PID_INPUT_REPORT_ID,        /*   Report ID () */ \
0x09, 0x9F,        /*   Usage (Device Paused) */ \
0x09, 0xA0,        /*   Usage (Actuators Enabled) */ \
0x09, 0xA4,        /*   Usage (Safety Switch) */ \
0x09, 0xA5,        /*   Usage (Actuator Override Switch) */ \
0x09, 0xA6,        /*   Usage (Actuator Power) */ \
0x15, 0x00,        /*   Logical Minimum (0) */ \
0x25, 0x01,        /*   Logical Maximum (1) */ \
0x35, 0x00,        /*   Physical Minimum (0) */ \
0x45, 0x01,        /*   Physical Maximum (1) */ \
0x75, 0x01,        /*   Report Size (1) */ \
0x95, 0x05,        /*   Report Count (5) */ \
0x81, 0x02,        /*   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x95, 0x03,        /*   Report Count (3) */ \
0x81, 0x03,        /*   Input (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x09, 0x94,        /*   Usage (Effect Playing) */ \
0x75, 0x01,        /*   Report Size (1) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x81, 0x02,        /*   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0x09, 0x22,        /*   Usage (Effect Block Index ) */ \
0x15, 0x01,        /*   Logical Minimum (1) */ \
0x25, MAX_EFFECT_BLOCKS,        /*   Logical Maximum () */ \
0x35, 0x01,        /*   Physical Minimum (1) */ \
0x45, MAX_EFFECT_BLOCKS,        /*   Physical Maximum () */ \
0x75, 0x07,        /*   Report Size (7) */ \
0x95, 0x01,        /*   Report Count (1) */ \
0x81, 0x02,        /*   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position) */ \
0xC0,              /* End Collection */ \