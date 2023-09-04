// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.Utilities.DeviceManagement.PnP;
using System.Collections.ObjectModel;
using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Services;
using Wpf.Ui.Controls;
using Newtonsoft.Json.Linq;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Pages
{
    public partial class DevicesViewModel : ObservableObject, INavigationAware
    {


        /// <summary>
        ///     List of detected devices.
        /// </summary>
        public ObservableCollection<DeviceViewModel> Devices { get; set; } = new();

        private readonly DshmDevicesManager _dshmDevicesManager;
        private readonly DshmConfigManager _dshmConfigManager;
        private readonly AppSnackbarMessagesService _appSnackbarMessagesService;


        /// <summary>
        ///     Currently selected device, if any.
        /// </summary>
        [ObservableProperty] private DeviceViewModel? _selectedDevice;

        /// <summary>
        ///     Is a device currently selected.
        /// </summary>
        public bool HasDeviceSelected => SelectedDevice != null;

        /// <summary>
        ///     Are there devices connected.
        /// </summary>
        [ObservableProperty] private bool _anyDeviceSelected = false;

        public DevicesViewModel(DshmDevicesManager dshmDevicesManager, DshmConfigManager dshmConfigManager, AppSnackbarMessagesService appSnackbarMessagesService)
        {
            _dshmDevicesManager = dshmDevicesManager;
            _dshmConfigManager = dshmConfigManager;
            _appSnackbarMessagesService = appSnackbarMessagesService;
            _dshmDevicesManager.ConnectedDeviceListUpdated += OnConnectedDevicesListUpdated;
            RefreshDevicesList();
        }


        partial void OnSelectedDeviceChanged(DeviceViewModel? value)
        {
            AnyDeviceSelected = (value != null);
        }

        public void OnNavigatedTo()
        {
            foreach(DeviceViewModel device in Devices)
            {
                device.RefreshDeviceSettings();
            }
        }

        public void OnNavigatedFrom()
        {
            SelectedDevice = null;
        }

        public void OnConnectedDevicesListUpdated(object? obj, EventArgs? eventArgs)
        {
            RefreshDevicesList();
        }

        private void RefreshDevicesList()
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Devices.Clear();
                foreach (PnPDevice device in _dshmDevicesManager.Devices)
                {
                    Devices.Add(new DeviceViewModel(device, _dshmDevicesManager, _dshmConfigManager, _appSnackbarMessagesService));
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
}

//[ObservableProperty]
//private int _counter = 0;



//[RelayCommand]
//private void OnCounterIncrement()
//{
//    Counter++;
//}

