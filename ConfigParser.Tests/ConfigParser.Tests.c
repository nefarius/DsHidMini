#include <Windows.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

#include "DsCommon.h"
#include "DsMotionHid.h"
#include "Configuration.Json.h"

static int g_passed;
static int g_failed;

#define TEST(name) static int name(void)
#define RUN(name) do { \
    if ((name)() == 0) { g_passed++; printf("  PASS %s\n", #name); } \
    else { g_failed++; printf("  FAIL %s\n", #name); } \
} while (0)

#define EXPECT(cond) do { \
    if (!(cond)) { \
        printf("    assertion failed: %s (%s:%d)\n", #cond, __FILE__, __LINE__); \
        return 1; \
    } \
} while (0)

#define EXPECT_STATUS(actual, expected) do { \
    NTSTATUS _actual = (actual); \
    if (_actual != (expected)) { \
        printf("    status 0x%08lX != 0x%08lX (%s:%d)\n", \
            (unsigned long)_actual, (unsigned long)(expected), __FILE__, __LINE__); \
        return 1; \
    } \
} while (0)

static NTSTATUS
ParseText(
    _In_z_ const CHAR* Json,
    _In_ BOOLEAN IsHotReload,
    _In_opt_ const DS_DRIVER_CONFIGURATION* Current,
    _In_opt_z_ const CHAR* DeviceAddress,
    _Out_ PDS_DRIVER_CONFIGURATION Parsed,
    _Out_opt_ PDS_CONFIG_RUMBLE_DERIVED Rumble
)
{
    DS_CONFIG_RUMBLE_DERIVED rumble;
    SIZE_T offset = 0;

    return ConfigParseJsonDocument(
        Json,
        strlen(Json),
        DeviceAddress,
        IsHotReload,
        Current,
        Parsed,
        Rumble ? Rumble : &rumble,
        &offset
    );
}

static CHAR*
ReadSiblingFile(
    _In_z_ const CHAR* FileName
)
{
    CHAR modulePath[MAX_PATH];
    CHAR* slash;
    DWORD length;
    FILE* file = NULL;
    long size;
    CHAR* buffer;

    length = GetModuleFileNameA(NULL, modulePath, ARRAYSIZE(modulePath));
    if (length == 0 || length >= ARRAYSIZE(modulePath))
    {
        return NULL;
    }

    slash = strrchr(modulePath, '\\');
    if (slash == NULL)
    {
        return NULL;
    }

    slash[1] = '\0';
    if (strcat_s(modulePath, ARRAYSIZE(modulePath), FileName) != 0)
    {
        return NULL;
    }

    if (fopen_s(&file, modulePath, "rb") != 0 || file == NULL)
    {
        return NULL;
    }

    if (fseek(file, 0, SEEK_END) != 0)
    {
        fclose(file);
        return NULL;
    }

    size = ftell(file);
    if (size <= 0)
    {
        fclose(file);
        return NULL;
    }

    if (fseek(file, 0, SEEK_SET) != 0)
    {
        fclose(file);
        return NULL;
    }

    buffer = (CHAR*)calloc((size_t)size + 1, 1);
    if (buffer == NULL)
    {
        fclose(file);
        return NULL;
    }

    if (fread(buffer, 1, (size_t)size, file) != (size_t)size)
    {
        free(buffer);
        fclose(file);
        return NULL;
    }

    fclose(file);
    return buffer;
}

TEST(Parse_CanonicalSample_LoadsGlobalAndDeviceOverlay)
{
    CHAR* json = ReadSiblingFile("DsHidMini.json");
    DS_DRIVER_CONFIGURATION parsed;
    DS_CONFIG_RUMBLE_DERIVED rumble;
    BOOLEAN ipcEnabled = FALSE;

    EXPECT(json != NULL);
    EXPECT_STATUS(
        ConfigParseJsonDocument(json, strlen(json), "44D832809FCD", FALSE, NULL, &parsed, &rumble, NULL),
        STATUS_SUCCESS
    );
    EXPECT_STATUS(ConfigParseIpcEnabled(json, strlen(json), &ipcEnabled, NULL), STATUS_SUCCESS);
    EXPECT(ipcEnabled == TRUE);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeSDF);
    EXPECT(parsed.DevicePairingMode == DsDevicePairingModeAuto);
    EXPECT(parsed.BluetoothOutputReportTransport == DsBluetoothOutputReportTransportControl);
    EXPECT(parsed.AutoRestartOnHidModeMismatch == TRUE);
    EXPECT(parsed.SDF.DPadExposureMode == DsDPadExposureModeIndividualButtons);
    EXPECT(parsed.ThumbSettings.DeadZoneLeft.PolarValue == 10.0);
    EXPECT(rumble.HeavyRescaleIsAllowed == TRUE);
    free(json);
    return 0;
}

TEST(Parse_Defaults_WhenDocumentIsEmptyObject)
{
    DS_DRIVER_CONFIGURATION parsed;
    DS_DRIVER_CONFIGURATION defaults;

    ConfigSetDefaults(&defaults);
    EXPECT_STATUS(ParseText("{}", FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(memcmp(&parsed, &defaults, sizeof(parsed)) == 0);
    return 0;
}

TEST(Parse_DeviceOverlay_OverridesOnlyPresentFields)
{
    const CHAR* json =
        "{"
        "\"Global\":{\"HidDeviceMode\":\"XInput\",\"OutputRateControlPeriodMs\":150,\"XInput\":{\"DeadZoneLeft\":{\"Apply\":true,\"PolarValue\":3.0}}},"
        "\"Devices\":{\"AABBCCDDEEFF\":{\"XInput\":{\"DeadZoneLeft\":{\"Apply\":true,\"PolarValue\":12.0}}}}"
        "}";
    DS_DRIVER_CONFIGURATION parsed;

    EXPECT_STATUS(ParseText(json, FALSE, NULL, "AABBCCDDEEFF", &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible);
    EXPECT(parsed.OutputRateControlPeriodMs == 150);
    EXPECT(parsed.ThumbSettings.DeadZoneLeft.Apply == TRUE);
    EXPECT(parsed.ThumbSettings.DeadZoneLeft.PolarValue == 12.0);
    return 0;
}

TEST(Parse_HotReload_PreservesImmutableFields)
{
    DS_DRIVER_CONFIGURATION current;
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json =
        "{"
        "\"Global\":{"
        "\"HidDeviceMode\":\"SDF\","
        "\"AutoRestartOnHidModeMismatch\":false,"
        "\"UsbOutputReportTransport\":\"ControlEndpoint\","
        "\"BluetoothOutputReportTransport\":\"Interrupt\","
        "\"OutputRateControlPeriodMs\":40"
        "}"
        "}";

    ConfigSetDefaults(&current);
    current.HidDeviceMode = DsHidMiniDeviceModeGPJ;
    current.AutoRestartOnHidModeMismatch = TRUE;
    current.UsbOutputReportTransport = DsUsbOutputReportTransportAuto;

    EXPECT_STATUS(ParseText(json, TRUE, &current, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeGPJ);
    EXPECT(parsed.AutoRestartOnHidModeMismatch == TRUE);
    EXPECT(parsed.UsbOutputReportTransport == DsUsbOutputReportTransportAuto);
    EXPECT(parsed.BluetoothOutputReportTransport == DsBluetoothOutputReportTransportInterrupt);
    EXPECT(parsed.OutputRateControlPeriodMs == 40);
    return 0;
}

TEST(Parse_HotReload_RemovedKeysRevertToDefaults)
{
    DS_DRIVER_CONFIGURATION current;
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json = "{\"Global\":{\"HidDeviceMode\":\"XInput\"}}";

    ConfigSetDefaults(&current);
    current.DisableWirelessIdleTimeout = TRUE;
    current.OutputRateControlPeriodMs = 10;
    current.DevicePairingMode = DsDevicePairingModeDisabled;

    EXPECT_STATUS(ParseText(json, TRUE, &current, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.DisableWirelessIdleTimeout == FALSE);
    EXPECT(parsed.OutputRateControlPeriodMs == 150);
    EXPECT(parsed.DevicePairingMode == DsDevicePairingModeAuto);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible);
    return 0;
}

TEST(Parse_UnknownHidMode_KeepsDefault)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json = "{\"Global\":{\"HidDeviceMode\":\"Nope\",\"OutputRateControlPeriodMs\":80}}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible);
    EXPECT(parsed.OutputRateControlPeriodMs == 80);
    return 0;
}

TEST(Parse_WrongTypes_LeaveNeighborsIntact)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json =
        "{"
        "\"Global\":{"
        "\"HidDeviceMode\":1,"
        "\"DevicePairingMode\":true,"
        "\"CustomPairingAddress\":123,"
        "\"IsOutputRateControlEnabled\":\"yes\","
        "\"OutputRateControlPeriodMs\":\"nope\","
        "\"WirelessIdleTimeoutPeriodMs\":null,"
        "\"QuickDisconnectCombo\":1,"
        "\"XInput\":\"not-an-object\","
        "\"DisableWirelessIdleTimeout\":true"
        "}"
        "}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible);
    EXPECT(parsed.DevicePairingMode == DsDevicePairingModeAuto);
    EXPECT(parsed.CustomHostAddress[0] == 0);
    EXPECT(parsed.IsOutputRateControlEnabled == TRUE);
    EXPECT(parsed.OutputRateControlPeriodMs == 150);
    EXPECT(parsed.WirelessIdleTimeoutPeriodMs == 300000);
    EXPECT(parsed.WirelessDisconnectButtonCombo.IsEnabled == TRUE);
    EXPECT(parsed.ThumbSettings.DeadZoneLeft.PolarValue == 3.0);
    EXPECT(parsed.DisableWirelessIdleTimeout == TRUE);
    return 0;
}

TEST(Parse_OutOfRangeAndFractionalNumbers_AreIgnored)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json =
        "{"
        "\"Global\":{"
        "\"OutputRateControlPeriodMs\":256,"
        "\"WirelessIdleTimeoutPeriodMs\":-1,"
        "\"QuickDisconnectCombo\":{\"HoldTime\":1.5,\"Button1\":17,\"Button2\":10},"
        "\"XInput\":{"
        "\"DeadZoneLeft\":{\"PolarValue\":361},"
        "\"RumbleSettings\":{\"HeavyRescale\":{\"RescaleMinRange\":-1}},"
        "\"LEDSettings\":{\"Mode\":\"CustomPattern\",\"CustomPatterns\":{\"Player1\":{\"BasePortionDuration\":70000}}}"
        "}"
        "}"
        "}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.OutputRateControlPeriodMs == 150);
    EXPECT(parsed.WirelessIdleTimeoutPeriodMs == 300000);
    EXPECT(parsed.WirelessDisconnectButtonCombo.HoldTime == 1000);
    EXPECT(parsed.WirelessDisconnectButtonCombo.Buttons[0] == 10);
    EXPECT(parsed.WirelessDisconnectButtonCombo.Buttons[1] == 10);
    EXPECT(parsed.ThumbSettings.DeadZoneLeft.PolarValue == 3.0);
    EXPECT(parsed.RumbleSettings.HeavyRescaling.MinRange == 64);
    EXPECT(parsed.LEDSettings.CustomPatterns.Player1.BasePortionDuration == 0x01);
    return 0;
}

TEST(Parse_ValidScalarsAndEnums)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json =
        "{"
        "\"Global\":{"
        "\"HidDeviceMode\":\"SXS\","
        "\"DevicePairingMode\":\"Custom\","
        "\"CustomPairingAddress\":\"112233445566\","
        "\"UsbOutputReportTransport\":\"InterruptOut\","
        "\"BluetoothOutputReportTransport\":\"Interrupt\","
        "\"IsOutputRateControlEnabled\":false,"
        "\"OutputRateControlPeriodMs\":40,"
        "\"WirelessIdleTimeoutPeriodMs\":5000,"
        "\"DisableWirelessIdleTimeout\":true,"
        "\"QuickDisconnectCombo\":{\"IsEnabled\":false,\"HoldTime\":250,\"Button1\":0,\"Button2\":3,\"Button3\":16},"
        "\"SXS\":{"
        "\"DeadZoneLeft\":{\"Apply\":false,\"PolarValue\":12.5},"
        "\"DeadZoneRight\":{\"Apply\":true,\"PolarValue\":0},"
        "\"RumbleSettings\":{"
        "\"DisableLeft\":true,"
        "\"HeavyRescale\":{\"IsEnabled\":false,\"RescaleMinRange\":10,\"RescaleMaxRange\":20}"
        "},"
        "\"LEDSettings\":{\"Mode\":\"BatteryIndicatorBarGraph\",\"Authority\":\"Driver\"},"
        "\"FlipAxis\":{\"LeftX\":true,\"RightY\":true}"
        "}"
        "}"
        "}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeSixaxisCompatible);
    EXPECT(parsed.DevicePairingMode == DsDevicePairingModeCustom);
    EXPECT(parsed.CustomHostAddress[0] == 0x11);
    EXPECT(parsed.CustomHostAddress[5] == 0x66);
    EXPECT(parsed.UsbOutputReportTransport == DsUsbOutputReportTransportInterruptOut);
    EXPECT(parsed.BluetoothOutputReportTransport == DsBluetoothOutputReportTransportInterrupt);
    EXPECT(parsed.IsOutputRateControlEnabled == FALSE);
    EXPECT(parsed.OutputRateControlPeriodMs == 40);
    EXPECT(parsed.WirelessIdleTimeoutPeriodMs == 5000);
    EXPECT(parsed.DisableWirelessIdleTimeout == TRUE);
    EXPECT(parsed.WirelessDisconnectButtonCombo.IsEnabled == FALSE);
    EXPECT(parsed.WirelessDisconnectButtonCombo.HoldTime == 250);
    EXPECT(parsed.WirelessDisconnectButtonCombo.Buttons[0] == 0);
    EXPECT(parsed.WirelessDisconnectButtonCombo.Buttons[2] == 16);
    EXPECT(parsed.ThumbSettings.DeadZoneLeft.Apply == FALSE);
    EXPECT(parsed.ThumbSettings.DeadZoneLeft.PolarValue == 12.5);
    EXPECT(parsed.ThumbSettings.DeadZoneRight.PolarValue == 0.0);
    EXPECT(parsed.RumbleSettings.DisableLeft == TRUE);
    EXPECT(parsed.RumbleSettings.HeavyRescaling.IsEnabled == FALSE);
    EXPECT(parsed.RumbleSettings.HeavyRescaling.MinRange == 10);
    EXPECT(parsed.LEDSettings.Mode == DsLEDModeBatteryIndicatorBarGraph);
    EXPECT(parsed.LEDSettings.Authority == DsLEDAuthorityDriver);
    EXPECT(parsed.FlipAxis.LeftX == TRUE);
    EXPECT(parsed.FlipAxis.RightY == TRUE);
    return 0;
}

TEST(Parse_InvalidPairingAddress_KeepsZeros)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json = "{\"Global\":{\"CustomPairingAddress\":\"11223344556G\"}}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.CustomHostAddress[0] == 0);
    EXPECT(parsed.CustomHostAddress[5] == 0);
    return 0;
}

TEST(Parse_ShortPairingAddress_IsIgnored)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json = "{\"Global\":{\"CustomPairingAddress\":\"AABBCCDDEE\"}}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.CustomHostAddress[0] == 0);
    return 0;
}

TEST(Parse_DuplicateKnownKeys_AreIgnored)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json =
        "{\"Global\":{\"HidDeviceMode\":\"XInput\",\"HidDeviceMode\":\"SDF\",\"OutputRateControlPeriodMs\":40}}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible);
    EXPECT(parsed.OutputRateControlPeriodMs == 40);
    return 0;
}

TEST(Parse_UnknownKeys_AreIgnored)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json =
        "{\"Global\":{\"HidDeviceMode\":\"XInput\",\"FutureDriverFlag\":true,\"IsOutputDeduplicatorEnabled\":true},\"Extra\":1}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible);
    return 0;
}

TEST(Parse_SdfAndGpjModeSpecificSettings)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json =
        "{\"Global\":{\"HidDeviceMode\":\"SDF\",\"SDF\":{\"PressureExposureMode\":\"Digital\",\"DPadExposureMode\":\"HAT\"}}}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.SDF.PressureExposureMode == DsPressureExposureModeDigital);
    EXPECT(parsed.SDF.DPadExposureMode == DsDPadExposureModeHAT);
    EXPECT(parsed.GPJ.PressureExposureMode == DsPressureExposureModeDefault);
    return 0;
}

TEST(Parse_CgpHidDeviceMode_Recognized)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json = "{\"Global\":{\"HidDeviceMode\":\"CGP\"}}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeCGP);
    return 0;
}

TEST(Parse_CgsHidDeviceMode_Recognized)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json = "{\"Global\":{\"HidDeviceMode\":\"CGS\"}}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeCGS);
    return 0;
}

static void
MotionFrame(
    _In_ INT32 ax,
    _In_ INT32 ay,
    _In_ INT32 az,
    _In_ INT32 gyro,
    _Out_ DS_MOTION_HID_FRAME* frame
)
{
    DsMotionHid_ToDeviceFrame(ax, ay, az, gyro, frame);
}

TEST(Motion_Rest_FlatFaceUp_ReportsPlusOneGOnDeviceY)
{
    DS_MOTION_HID_FRAME frame;
    UCHAR ds4[64] = { 0 };

    // DS3 flat, buttons up: Z reads -1 g, X/Y 0, gyro 0
    MotionFrame(0, 0, -1000, 0, &frame);
    EXPECT(frame.AccelMilliG[0] == 0);
    EXPECT(frame.AccelMilliG[1] == 1000);
    EXPECT(frame.AccelMilliG[2] == 0);
    EXPECT(frame.GyroMilliDps[1] == 0);

    DsMotionHid_WriteDs4Report(&frame, ds4);
    // accel Y at bytes 21-22 = +8192 (0x2000) little endian
    EXPECT(ds4[21] == 0x00 && ds4[22] == 0x20);
    EXPECT(ds4[19] == 0 && ds4[20] == 0 && ds4[23] == 0 && ds4[24] == 0);
    EXPECT(ds4[13] == 0 && ds4[14] == 0 && ds4[15] == 0 && ds4[16] == 0 && ds4[17] == 0 && ds4[18] == 0);
    return 0;
}

TEST(Motion_SixPoses_MapToDeviceFrame)
{
    DS_MOTION_HID_FRAME frame;

    // Left grip down: Sony X (towards left grip) points down -> -1 g; device X (right) points up -> +1 g
    MotionFrame(-1000, 0, 0, 0, &frame);
    EXPECT(frame.AccelMilliG[0] == 1000 && frame.AccelMilliG[1] == 0 && frame.AccelMilliG[2] == 0);

    // Right grip down
    MotionFrame(1000, 0, 0, 0, &frame);
    EXPECT(frame.AccelMilliG[0] == -1000);

    // Upside down (buttons on desk): Sony Z +1 g -> device Y -1 g
    MotionFrame(0, 0, 1000, 0, &frame);
    EXPECT(frame.AccelMilliG[1] == -1000);

    // Back edge down (triggers up): Sony Y +1 g -> device Z (towards player) -1 g
    MotionFrame(0, 1000, 0, 0, &frame);
    EXPECT(frame.AccelMilliG[2] == -1000);

    // Front edge down (triggers on desk): Sony Y -1 g -> device Z +1 g
    MotionFrame(0, -1000, 0, 0, &frame);
    EXPECT(frame.AccelMilliG[2] == 1000);
    return 0;
}

TEST(Motion_Yaw_ClockwiseFromAbove_IsNegativeOnDeviceY)
{
    DS_MOTION_HID_FRAME frame;
    UCHAR ds4[64] = { 0 };

    // 90 deg/s clockwise seen from above (positive in the source)
    MotionFrame(0, 0, 0, 90000, &frame);
    EXPECT(frame.GyroMilliDps[0] == 0 && frame.GyroMilliDps[2] == 0);
    EXPECT(frame.GyroMilliDps[1] == -90000);

    DsMotionHid_WriteDs4Report(&frame, ds4);
    // gyro Y at bytes 15-16: -90 * 16 = -1440 = 0xFA60
    EXPECT(ds4[15] == 0x60 && ds4[16] == 0xFA);

    // counter-clockwise
    MotionFrame(0, 0, 0, -90000, &frame);
    EXPECT(frame.GyroMilliDps[1] == 90000);
    return 0;
}

TEST(Motion_Saturation_ClampsToSymmetricInt16)
{
    DS_MOTION_HID_FRAME frame;
    UCHAR ds4[64] = { 0 };
    UCHAR sensor[DS_MOTION_HID_SENSOR_INPUT_REPORT_SIZE];
    INT32 vector[3] = { 100000, -100000, 0 };

    MotionFrame(0, 0, -100000, 3000000, &frame);
    DsMotionHid_WriteDs4Report(&frame, ds4);
    EXPECT(ds4[21] == 0xFF && ds4[22] == 0x7F);
    EXPECT(ds4[15] == 0x01 && ds4[16] == 0x80);

    DsMotionHid_WriteSensorInputReport(DS_MOTION_HID_REPORT_ID_ACCEL, DS_MOTION_HID_STATUS_READY, vector, sensor);
    EXPECT(sensor[3] == 0xFF && sensor[4] == 0x7F);
    EXPECT(sensor[5] == 0x01 && sensor[6] == 0x80);
    return 0;
}

TEST(Motion_SensorInputReport_ExactBytes)
{
    UCHAR sensor[DS_MOTION_HID_SENSOR_INPUT_REPORT_SIZE];
    INT32 vector[3] = { 1000, -2, 258 };

    DsMotionHid_WriteSensorInputReport(DS_MOTION_HID_REPORT_ID_GYRO, DS_MOTION_HID_STATUS_READY, vector, sensor);
    EXPECT(sensor[0] == 0x31);
    EXPECT(sensor[1] == DS_MOTION_HID_STATUS_READY);
    EXPECT(sensor[2] == DS_MOTION_HID_EVENT_DATA_UPDATED);
    EXPECT(sensor[3] == 0xE8 && sensor[4] == 0x03);
    EXPECT(sensor[5] == 0xFE && sensor[6] == 0xFF);
    EXPECT(sensor[7] == 0x02 && sensor[8] == 0x01);
    return 0;
}

TEST(Motion_SensorFeatureReport_RoundTrips)
{
    DS_MOTION_HID_SENSOR_PROPS props;
    DS_MOTION_HID_SENSOR_PROPS parsed;
    UCHAR report[DS_MOTION_HID_SENSOR_FEATURE_REPORT_SIZE];

    DsMotionHid_SensorPropsInit(&props);
    EXPECT(props.ReportingState == DS_MOTION_HID_REPORTING_STATE_ALL_EVENTS);
    EXPECT(props.PowerState == DS_MOTION_HID_POWER_STATE_D0);
    EXPECT(props.SensorState == DS_MOTION_HID_STATUS_NO_DATA);
    EXPECT(props.IntervalMs == DS_MOTION_HID_DEFAULT_INTERVAL_MS);

    props.Sensitivity = 0x1234;
    props.IntervalMs = 0x01020304;
    DsMotionHid_WriteSensorFeatureReport(DS_MOTION_HID_REPORT_ID_ACCEL, DS_MOTION_HID_STATUS_NO_DATA, &props, report);
    EXPECT(report[0] == 0x30 && report[2] == DS_MOTION_HID_POWER_STATE_D0);
    EXPECT(report[3] == DS_MOTION_HID_STATUS_NO_DATA);
    EXPECT(report[4] == 0x34 && report[5] == 0x12);
    EXPECT(report[6] == 0x04 && report[9] == 0x01);

    DsMotionHid_ReadSensorFeatureReport(report, &parsed);
    EXPECT(parsed.ReportingState == props.ReportingState);
    EXPECT(parsed.PowerState == props.PowerState);
    EXPECT(parsed.SensorState == DS_MOTION_HID_STATUS_NO_DATA);
    EXPECT(parsed.Sensitivity == 0x1234);
    EXPECT(parsed.IntervalMs == 0x01020304);

    report[1] = 0xFF;
    DsMotionHid_ReadSensorFeatureReport(report, &parsed);
    EXPECT(parsed.ReportingState == DS_MOTION_HID_REPORTING_STATE_ALL_EVENTS);
    return 0;
}

TEST(Motion_Ds4Timestamp_UsesFiveThirdMicrosecondTicksAndWraps)
{
    // 1 s at 10 MHz = 187500 ticks -> 187500 & 0xFFFF
    EXPECT(DsMotionHid_Ds4Timestamp(10000000, 10000000) == (USHORT)(187500 & 0xFFFF));
    EXPECT(DsMotionHid_Ds4Timestamp(0, 10000000) == 0);
    EXPECT(DsMotionHid_Ds4Timestamp(5, 0) == 0);
    // Very large QPC values must not overflow
    EXPECT(DsMotionHid_Ds4Timestamp(0x7000000000000000LL, 10000000) ==
        (USHORT)((((0x7000000000000000LL / 10000000) * 187500) + (((0x7000000000000000LL % 10000000) * 187500) / 10000000)) & 0xFFFF));
    return 0;
}

TEST(Motion_Descriptors_HaveExpectedLengthsAndIds)
{
    // Guards the contract between the descriptor and the report writers
    EXPECT(DS_MOTION_HID_SENSOR_INPUT_REPORT_SIZE == 1 + 1 + 1 + 6);
    EXPECT(DS_MOTION_HID_SENSOR_FEATURE_REPORT_SIZE == 1 + 1 + 1 + 1 + 2 + 4);
    EXPECT(DS_MOTION_HID_REPORT_ID_ACCEL != DS_MOTION_HID_REPORT_ID_GYRO);
    EXPECT(DS_MOTION_HID_REPORTING_STATE_NO_EVENTS == 1);
    EXPECT(DS_MOTION_HID_REPORTING_STATE_ALL_EVENTS == 2);
    EXPECT(DS_MOTION_HID_REPORTING_STATE_NO_EVENTS_WAKE == 4);
    EXPECT(DS_MOTION_HID_POWER_STATE_D0 == 2);
    EXPECT(DS_MOTION_HID_STATUS_READY == 2);
    EXPECT(DS_MOTION_HID_STATUS_NO_DATA == 4);
    EXPECT(DS_MOTION_HID_EVENT_DATA_UPDATED == 4);
    EXPECT(DsMotionHid_MilliGToMilliMetersPerSecondSquared(1000) == 9806);
    EXPECT(DsMotionHid_MilliGToMilliMetersPerSecondSquared(-1000) == -9806);
    return 0;
}

TEST(Parse_MalformedJson_Fails)
{
    DS_DRIVER_CONFIGURATION parsed;
    DS_CONFIG_RUMBLE_DERIVED rumble;

    RtlZeroMemory(&parsed, sizeof(parsed));
    parsed.OutputRateControlPeriodMs = 7;
    EXPECT_STATUS(ParseText("{\"Global\":", FALSE, NULL, NULL, &parsed, &rumble), STATUS_DATA_ERROR);
    EXPECT(parsed.OutputRateControlPeriodMs == 7);
    return 0;
}

TEST(Parse_TrailingGarbage_Fails)
{
    DS_DRIVER_CONFIGURATION parsed;

    EXPECT_STATUS(ParseText("{\"Global\":{}} trailing", FALSE, NULL, NULL, &parsed, NULL), STATUS_DATA_ERROR);
    return 0;
}

TEST(Parse_RootArray_Fails)
{
    DS_DRIVER_CONFIGURATION parsed;

    EXPECT_STATUS(ParseText("[]", FALSE, NULL, NULL, &parsed, NULL), STATUS_DATA_ERROR);
    return 0;
}

TEST(Parse_EmbeddedNul_Fails)
{
    CHAR buffer[32];
    DS_DRIVER_CONFIGURATION parsed;
    DS_CONFIG_RUMBLE_DERIVED rumble;
    SIZE_T offset = 0;

    memcpy(buffer, "{\"Global\":{}}", 13);
    buffer[7] = '\0';
    buffer[13] = '\0';

    EXPECT_STATUS(
        ConfigParseJsonDocument(buffer, 13, NULL, FALSE, NULL, &parsed, &rumble, &offset),
        STATUS_DATA_ERROR
    );
    EXPECT(offset == 7);
    return 0;
}

TEST(Parse_EmptyPayload_Fails)
{
    DS_DRIVER_CONFIGURATION parsed;
    DS_CONFIG_RUMBLE_DERIVED rumble;
    CHAR empty[1] = { '\0' };

    EXPECT_STATUS(
        ConfigParseJsonDocument(empty, 0, NULL, FALSE, NULL, &parsed, &rumble, NULL),
        STATUS_DATA_ERROR
    );
    return 0;
}

TEST(Parse_ExcessiveNesting_Fails)
{
    CHAR json[256];
    SIZE_T used = 0;
    DS_DRIVER_CONFIGURATION parsed;
    const int depth = CONFIG_JSON_MAX_NESTING + 2;

    json[0] = '\0';
    for (int i = 0; i < depth; i++)
    {
        EXPECT(used + 6 < sizeof(json));
        memcpy(json + used, "{\"a\":", 5);
        used += 5;
    }

    memcpy(json + used, "1", 1);
    used += 1;
    for (int i = 0; i < depth; i++)
    {
        EXPECT(used + 2 < sizeof(json));
        json[used++] = '}';
    }
    json[used] = '\0';

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_DATA_ERROR);
    return 0;
}

TEST(Parse_HugeNumber_IsIgnored)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json = "{\"Global\":{\"OutputRateControlPeriodMs\":1e999,\"WirelessIdleTimeoutPeriodMs\":1}}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, NULL, &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.OutputRateControlPeriodMs == 150);
    EXPECT(parsed.WirelessIdleTimeoutPeriodMs == 1);
    return 0;
}

TEST(Parse_DevicesArray_DoesNotCrash)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json = "{\"Global\":{\"HidDeviceMode\":\"XInput\"},\"Devices\":[]}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, "AABBCCDDEEFF", &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible);
    return 0;
}

TEST(Parse_NullDeviceNode_IsIgnored)
{
    DS_DRIVER_CONFIGURATION parsed;
    const CHAR* json = "{\"Global\":{\"HidDeviceMode\":\"XInput\"},\"Devices\":{\"AABBCCDDEEFF\":null}}";

    EXPECT_STATUS(ParseText(json, FALSE, NULL, "AABBCCDDEEFF", &parsed, NULL), STATUS_SUCCESS);
    EXPECT(parsed.HidDeviceMode == DsHidMiniDeviceModeXInputHIDCompatible);
    return 0;
}

TEST(Parse_IpcEnabled_MissingDefaultsTrue)
{
    BOOLEAN enabled = FALSE;

    EXPECT_STATUS(ConfigParseIpcEnabled("{}", 2, &enabled, NULL), STATUS_SUCCESS);
    EXPECT(enabled == TRUE);
    return 0;
}

TEST(Parse_IpcEnabled_True)
{
    BOOLEAN enabled = FALSE;
    const CHAR* json = "{\"IPCEnabled\":true,\"Global\":{}}";

    EXPECT_STATUS(ConfigParseIpcEnabled(json, strlen(json), &enabled, NULL), STATUS_SUCCESS);
    EXPECT(enabled == TRUE);
    return 0;
}

TEST(Parse_IpcEnabled_False)
{
    BOOLEAN enabled = TRUE;
    const CHAR* json = "{\"IPCEnabled\":false,\"Global\":{}}";

    EXPECT_STATUS(ConfigParseIpcEnabled(json, strlen(json), &enabled, NULL), STATUS_SUCCESS);
    EXPECT(enabled == FALSE);
    return 0;
}

TEST(Parse_IpcEnabled_InvalidType_LeavesOutputUntouched)
{
    BOOLEAN enabled = FALSE;
    const CHAR* json = "{\"IPCEnabled\":\"yes\",\"Global\":{}}";

    EXPECT_STATUS(ConfigParseIpcEnabled(json, strlen(json), &enabled, NULL), STATUS_DATA_ERROR);
    EXPECT(enabled == FALSE);
    return 0;
}

TEST(Parse_IpcEnabled_InvalidDocument_LeavesOutputUntouched)
{
    BOOLEAN enabled = TRUE;

    EXPECT_STATUS(ConfigParseIpcEnabled("{ not-json", 10, &enabled, NULL), STATUS_DATA_ERROR);
    EXPECT(enabled == TRUE);
    return 0;
}

TEST(Parse_IpcEnabled_Duplicate_LeavesOutputUntouched)
{
    BOOLEAN enabled = FALSE;
    const CHAR* json = "{\"IPCEnabled\":true,\"IPCEnabled\":false}";

    EXPECT_STATUS(ConfigParseIpcEnabled(json, strlen(json), &enabled, NULL), STATUS_DATA_ERROR);
    EXPECT(enabled == FALSE);
    return 0;
}

TEST(DeriveRumble_InvalidRange_DisallowsRescale)
{
    DS_DRIVER_CONFIGURATION config;
    DS_CONFIG_RUMBLE_DERIVED rumble;

    ConfigSetDefaults(&config);
    config.RumbleSettings.HeavyRescaling.MinRange = 200;
    config.RumbleSettings.HeavyRescaling.MaxRange = 10;
    ConfigDeriveRumbleState(&config, &rumble);
    EXPECT(rumble.HeavyRescaleIsAllowed == FALSE);
    return 0;
}

int main(void)
{
    printf("ConfigParser.Tests\n");

    RUN(Parse_CanonicalSample_LoadsGlobalAndDeviceOverlay);
    RUN(Parse_Defaults_WhenDocumentIsEmptyObject);
    RUN(Parse_DeviceOverlay_OverridesOnlyPresentFields);
    RUN(Parse_HotReload_PreservesImmutableFields);
    RUN(Parse_HotReload_RemovedKeysRevertToDefaults);
    RUN(Parse_UnknownHidMode_KeepsDefault);
    RUN(Parse_WrongTypes_LeaveNeighborsIntact);
    RUN(Parse_OutOfRangeAndFractionalNumbers_AreIgnored);
    RUN(Parse_ValidScalarsAndEnums);
    RUN(Parse_InvalidPairingAddress_KeepsZeros);
    RUN(Parse_ShortPairingAddress_IsIgnored);
    RUN(Parse_DuplicateKnownKeys_AreIgnored);
    RUN(Parse_UnknownKeys_AreIgnored);
    RUN(Parse_SdfAndGpjModeSpecificSettings);
    RUN(Parse_CgpHidDeviceMode_Recognized);
    RUN(Parse_CgsHidDeviceMode_Recognized);
    RUN(Motion_Rest_FlatFaceUp_ReportsPlusOneGOnDeviceY);
    RUN(Motion_SixPoses_MapToDeviceFrame);
    RUN(Motion_Yaw_ClockwiseFromAbove_IsNegativeOnDeviceY);
    RUN(Motion_Saturation_ClampsToSymmetricInt16);
    RUN(Motion_SensorInputReport_ExactBytes);
    RUN(Motion_SensorFeatureReport_RoundTrips);
    RUN(Motion_Ds4Timestamp_UsesFiveThirdMicrosecondTicksAndWraps);
    RUN(Motion_Descriptors_HaveExpectedLengthsAndIds);
    RUN(Parse_MalformedJson_Fails);
    RUN(Parse_TrailingGarbage_Fails);
    RUN(Parse_RootArray_Fails);
    RUN(Parse_EmbeddedNul_Fails);
    RUN(Parse_EmptyPayload_Fails);
    RUN(Parse_ExcessiveNesting_Fails);
    RUN(Parse_HugeNumber_IsIgnored);
    RUN(Parse_DevicesArray_DoesNotCrash);
    RUN(Parse_NullDeviceNode_IsIgnored);
    RUN(Parse_IpcEnabled_MissingDefaultsTrue);
    RUN(Parse_IpcEnabled_True);
    RUN(Parse_IpcEnabled_False);
    RUN(Parse_IpcEnabled_InvalidType_LeavesOutputUntouched);
    RUN(Parse_IpcEnabled_InvalidDocument_LeavesOutputUntouched);
    RUN(Parse_IpcEnabled_Duplicate_LeavesOutputUntouched);
    RUN(DeriveRumble_InvalidRange_DisallowsRescale);

    printf("%d passed, %d failed\n", g_passed, g_failed);
    return g_failed == 0 ? 0 : 1;
}
