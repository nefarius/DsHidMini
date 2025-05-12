using System.Windows.Controls;

using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

namespace Nefarius.DsHidMini.ControlApp.Helpers;

internal class SettingsGroupsDataTemplateSelector : DataTemplateSelector
{
    public Dictionary<Type, string> GroupSettingsTemplateIndex = new()
    {
        { typeof(HidModeSettingsViewModel), "Template_Unique_All" },
        { typeof(LedsSettingsViewModel), "Template_LEDsSettings" },
        { typeof(WirelessSettingsViewModel), "Template_WirelessSettings" },
        { typeof(SticksSettingsViewModel), "Template_SticksDeadZone" },
        { typeof(GeneralRumbleSettingsViewModel), "Template_RumbleBasicFunctions" },
        { typeof(OutputReportSettingsViewModel), "Template_OutputRateControl" },
        { typeof(LeftMotorRescalingSettingsViewModel), "Template_RumbleHeavyStrRescale" },
        { typeof(AltRumbleModeSettingsViewModel), "Template_RumbleVariableLightEmuTuning" }
    };

    public override DataTemplate
        SelectTemplate(object item, DependencyObject container)
    {
        if (item != null)
        {
            if (GroupSettingsTemplateIndex.TryGetValue(item.GetType(), out string templateKey))
            {
                return Application.Current.TryFindResource(templateKey) as DataTemplate;
            }
        }

        return null;
    }
}