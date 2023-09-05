// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;
using Nefarius.Utilities.DeviceManagement.PnP;

using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        private readonly DshmDevicesManager _dshmDevicesManager;
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            DshmDevicesManager dshmDevicesManager, //
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            ISnackbarService snackbarService,
            IContentDialogService contentDialogService
        )
        {


            ViewModel = viewModel;
            DataContext = this;

            _dshmDevicesManager = dshmDevicesManager;

            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);

            InitializeComponent();

            navigationService.SetNavigationControl(RootNavigation);
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            contentDialogService.SetContentPresenter(RootContentDialog);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            InitializeComponent();
            _dshmDevicesManager.StartListeningForDshmDevices();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _dshmDevicesManager.StopListeningForDshmDevices();
        }


        #region INavigationWindow methods

        public INavigationView GetNavigation()
        {
            return RootNavigation;
        }

        public bool Navigate(Type pageType)
        {
            return RootNavigation.Navigate(pageType);
        }

        public void SetPageService(IPageService pageService)
        {
            RootNavigation.SetPageService(pageService);
        }

        public void ShowWindow()
        {
            Show();
        }

        public void CloseWindow()
        {
            Close();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        #endregion INavigationWindow methods
    }
}