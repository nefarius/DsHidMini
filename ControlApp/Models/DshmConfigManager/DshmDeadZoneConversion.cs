namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>
///     ControlApp stores stick dead zones as 0–142 UI units. The driver expects a polar value (double).
///     The historical scale factor is 181/142.
/// </summary>
public static class DshmDeadZoneConversion
{
    public const double PolarScale = 181.0 / 142.0;
    public const double DriverDefaultPolarValue = 3.0;
    public const int MaxUiDeadZone = 142;

    public static int DefaultUiDeadZone { get; } = FromPolarValue(DriverDefaultPolarValue);

    public static double ToPolarValue(int deadZone) => deadZone * PolarScale;

    public static int FromPolarValue(double polarValue)
    {
        int mapped = (int)Math.Round(polarValue / PolarScale);
        return Math.Clamp(mapped, 0, MaxUiDeadZone);
    }
}
