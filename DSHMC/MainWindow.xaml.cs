using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AdonisUI.Controls;
using Nefarius.DsHidMini.Drivers;
using Nefarius.DsHidMini.MVVM;
using Nefarius.DsHidMini.Util;
using Nefarius.DsHidMini.Util.App;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace Nefarius.DsHidMini
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        private readonly DeviceNotificationListener _listener = new DeviceNotificationListener();

        private readonly MainViewModel _vm = new MainViewModel();

        public MainWindow()
        {
            ToolTipService.InitialShowDelayProperty.OverrideMetadata(
                typeof(FrameworkElement), new FrameworkPropertyMetadata(250));
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(FrameworkElement), new FrameworkPropertyMetadata(int.MaxValue));

            InitializeComponent();

            DataContext = _vm;

            if (ApplicationConfiguration.Instance.HasAcknowledgedDonationDialog) return;

            var messageBox = new MessageBoxModel
            {
                Text =
                    "Hello, Gamer! \r\n\r\nDid you know this project was only possible with years of dedication and enthusiasm? " +
                    "You're receiving this work for absolutely free. If it brings you joy please consider giving back to the "
                    + "author's efforts and show your appreciation through a donation. \r\n\r\nThanks for your attention ❤️",
                Caption = "May I have your attention",
                Icon = MessageBoxImage.Question,
                Buttons = new[]
                {
                   MessageBoxButtons.Cancel("Acknowledged"),
                   MessageBoxButtons.Yes("Sure, show me how!")
                },
                CheckBoxes = new[]
                {
                    new MessageBoxCheckBoxModel("I've already donated or will consider it")
                    {
                        IsChecked = false,
                        Placement = MessageBoxCheckBoxPlacement.BelowText
                    }
                },
                IsSoundEnabled = false
            };

            MessageBox.Show(messageBox);

            switch (messageBox.Result)
            {
                case MessageBoxResult.Yes:
                    Process.Start("https://vigem.org/Donations/");
                    break;
            }

            ApplicationConfiguration.Instance.HasAcknowledgedDonationDialog =
                messageBox.CheckBoxes.First().IsChecked;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var instance = 0;
            while (Devcon.Find(DsHidMiniDriver.DeviceInterfaceGuid, out var path, out var instanceId, instance++))
                _vm.Devices.Add(new DeviceViewModel(Device.GetDeviceByInstanceId(instanceId)));

            _listener.DeviceArrived += ListenerOnDeviceArrived;
            _listener.DeviceRemoved += ListenerOnDeviceRemoved;

            _listener.StartListen(this);
        }

        /// <summary>
        ///     DsHidMini device disconnected.
        /// </summary>
        /// <param name="obj">The device path.</param>
        private void ListenerOnDeviceRemoved(string obj)
        {
            _vm.Devices.Clear();

            var instance = 0;
            while (Devcon.Find(DsHidMiniDriver.DeviceInterfaceGuid, out var path, out var instanceId, instance++))
                _vm.Devices.Add(new DeviceViewModel(Device.GetDeviceByInstanceId(instanceId)));
        }

        /// <summary>
        ///     DsHidMini device connected.
        /// </summary>
        /// <param name="obj">The device path.</param>
        private void ListenerOnDeviceArrived(string obj)
        {
            _vm.Devices.Clear();

            var instance = 0;
            while (Devcon.Find(DsHidMiniDriver.DeviceInterfaceGuid, out var path, out var instanceId, instance++))
                _vm.Devices.Add(new DeviceViewModel(Device.GetDeviceByInstanceId(instanceId)));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _listener.EndListen();
        }
    }
}