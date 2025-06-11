using System.Collections.ObjectModel;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Util.Web;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;
using Nefarius.Utilities.DeviceManagement.PnP;

using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

public partial class DevicesViewModel : ObservableObject, INavigationAware
{
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
    private readonly IContentDialogService _contentDialogService;
    private readonly AddressValidator _addressValidator;
    private readonly DshmConfigManager _dshmConfigManager;

    private readonly DshmDevMan _dshmDevMan;

    /// <summary>
    ///     Are there devices connected.
    /// </summary>
    [ObservableProperty]
    private bool _anyDeviceSelected;


    /// <summary>
    ///     Currently selected device, if any.
    /// </summary>
    [ObservableProperty]
    private DeviceViewModel? _selectedDevice;

    public DevicesViewModel(
        DshmDevMan dshmDevMan,
        DshmConfigManager dshmConfigManager,
        AppSnackbarMessagesService appSnackbarMessagesService, 
        IContentDialogService contentDialogService,
        AddressValidator addressValidator
        )
    {
        _dshmDevMan = dshmDevMan;
        _dshmConfigManager = dshmConfigManager;
        _appSnackbarMessagesService = appSnackbarMessagesService;
        _dshmDevMan.ConnectedDeviceListUpdated += OnConnectedDevicesListUpdated;
        _dshmConfigManager.DshmConfigurationUpdated += OnDshmConfigUpdated;
        _contentDialogService = contentDialogService;
        _addressValidator = addressValidator;
        RefreshDevicesList();
    }


    /// <summary>
    ///     List of detected devices.
    /// </summary>
    public ObservableCollection<DeviceViewModel> Devices { get; set; } = new();

    /// <summary>
    ///     Is a device currently selected.
    /// </summary>
    public bool HasDeviceSelected => SelectedDevice != null;

    public Task OnNavigatedToAsync()
    {
        Log.Logger.Debug(
            "Navigating to Devices page. Refreshing dynamic properties of each connected Device ViewModel.");
        foreach (DeviceViewModel device in Devices)
        {
            device.RefreshDeviceSettings();
        }

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        SelectedDevice = null;
        return Task.CompletedTask;
    }

    partial void OnSelectedDeviceChanged(DeviceViewModel? value)
    {
        AnyDeviceSelected = value != null;
    }

    private void OnConnectedDevicesListUpdated(object? obj, EventArgs? eventArgs)
    {
        RefreshDevicesList();
    }

    private void OnDshmConfigUpdated(object? obj, EventArgs? eventArgs)
    {
        ReconnectDevicesWithMismatchedHidMode();
    }

    private void ReconnectAllDevices()
    {
        bool oneOrMoreFails = false;
        foreach (DeviceViewModel devVM in Devices)
        {
            if (devVM.IsHidModeMismatched)
            {
                if (!DshmDevMan.TryReconnectDevice(devVM.Device))
                {
                    oneOrMoreFails = true;
                }
            }
        }

        if (oneOrMoreFails)
        {
            ShowReconnectionAttemptFailedMessageBox();
        }
    }

    private async void ShowReconnectionAttemptFailedMessageBox()
    {
        MessageBox uiMessageBox = new()
        {
            Title = "Failed to restart/reconnect one or more devices",
            Content =
                "To restart USB controllers the ControlApp needs to be running as administrator.\n\nIf it's still failing then reconnect the controllers manually to update their HID mode."
        };

        _ = await uiMessageBox.ShowDialogAsync();
    }

    private void ReconnectDevicesWithMismatchedHidMode()
    {
        Log.Logger.Debug("Checking for devices in non-expected Hid Mode");
        bool mismatchedRemains = false;
        foreach (DeviceViewModel devVM in Devices)
        {
            if (devVM.IsHidModeMismatched)
            {
                mismatchedRemains = true;
                break;
            }
        }

        if (mismatchedRemains)
        {
            Log.Logger.Information("Detected one or more devices with non-expected Hid Mode.");
            ShowHidMismatchedDevicesDialog();
        }
        else
        {
            Log.Logger.Debug("No mismatches found");
        }
    }

    private async void ShowHidMismatchedDevicesDialog()
    {
        Log.Logger.Debug("Showing Hid Mismatch dialog.");
        ContentDialogResult result = await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "One or more devices pending reconnection",
                Content = """
                          The HID mode setting of one or more connected device has changed.
                          Reconnect the pending devices to make this change effective.
                          """,
                //PrimaryButtonText = "Ok",
                //SecondaryButtonText = "Don't Save",
                PrimaryButtonText = "Reconnect them now",
                CloseButtonText = "OK"
            }
        );
        if (result == ContentDialogResult.Primary)
        {
            ReconnectAllDevices();
        }
    }

    private void RefreshDevicesList()
    {
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            Devices.Clear();
            foreach (PnPDevice device in _dshmDevMan.Devices)
            {
                Devices.Add(new DeviceViewModel(
                    device,
                    _dshmDevMan,
                    _dshmConfigManager, 
                    _appSnackbarMessagesService,
                    _contentDialogService,
                    _addressValidator
                    )
                );
            }
        }));
    }


    /*
    /// <summary>
    ///     Helper to check if run with elevated privileges.
    /// </summary>
    public bool IsElevated => SecurityUtil.IsElevated;

    /// <summary>
    ///     True if run as regular, non-administrative user.
    /// </summary>
    public bool IsMissingPermissions => !IsElevated;

    /// <summary>
    ///     Is it possible to edit the selected device.
    /// </summary>
    public bool IsEditable => IsElevated && HasDeviceSelected && !IsRestarting;

    /// <summary>
    ///     Is the selected device in the process of getting restarted.
    /// </summary>
    public bool IsRestarting { get; set; } = false;

    /// <summary>
    ///     Version to display in window title.
    /// </summary>
    public string Version => $"Version: {Assembly.GetEntryAssembly().GetName().Version}";

    /// <summary>
    ///     True if GitHub version is newer than own version.
    /// </summary>
    public bool IsUpdateAvailable => Updater.IsUpdateAvailable;

    private static string ParametersKey =>
        "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\WUDF\\Services\\dshidmini\\Parameters";

    /// <summary>
    ///     Indicates if verbose logging is on or off.
    /// </summary>
    public bool VerboseOn
    {
        get
        {
            using (var key = RegistryHelpers.GetRegistryKey(ParametersKey))
            {
                if (key == null) return false;
                var value = key.GetValue("VerboseOn");
                return value != null && (int) value > 0;
            }
        }
        set
        {
            using (var key = RegistryHelpers.GetRegistryKey(ParametersKey, true))
            {
                key?.SetValue("VerboseOn", value ? 1 : 0, RegistryValueKind.DWord);
            }
        }
    }

    public bool IsFilterAvailable => IsElevated && BthPS3FilterDriver.IsFilterAvailable;

    public bool IsFilterUnavailable => IsElevated && !BthPS3FilterDriver.IsFilterAvailable;

    public bool IsFilterEnabled
    {
        get => IsElevated && BthPS3FilterDriver.IsFilterEnabled;
        set => BthPS3FilterDriver.IsFilterEnabled = value;
    }

    public bool IsRawPDODisabled => !BthPS3ProfileDriver.RawPDO;

    public bool AreBthPS3SettingsCorrect =>
        IsElevated && !BthPS3ProfileDriver.RawPDO && BthPS3FilterDriver.IsFilterEnabled;

    public bool AreBthPS3SettingsIncorrect =>
        IsElevated && (BthPS3ProfileDriver.RawPDO || !BthPS3FilterDriver.IsFilterEnabled);

    public bool IsPressureMutingSupported => IsEditable && SelectedDevice.IsPressureMutingSupported;

    public event PropertyChangedEventHandler PropertyChanged;

    public void RefreshProperties()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsFilterEnabled"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRawPDODisabled"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AreBthPS3SettingsIncorrect"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AreBthPS3SettingsCorrect"));
    }
    */
}

//[ObservableProperty]
//private int _counter = 0;

//[RelayCommand]
//private void OnCounterIncrement()
//{
//    Counter++;
//}