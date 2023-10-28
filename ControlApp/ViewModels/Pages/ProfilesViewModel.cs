using System.Diagnostics.Metrics;
using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Services;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Pages
{

    public partial class ProfilesViewModel : ObservableObject, INavigationAware
    {
        // ----------------------------------------------------------- FIELDS

        private readonly DshmDevMan _dshmDevMan;
        private readonly DshmConfigManager _dshmConfigManager;

        [ObservableProperty] public List<ProfileViewModel> _profilesViewModels;
        [ObservableProperty] private ProfileViewModel? _selectedProfileVM = null;



        // ----------------------------------------------------------- PROPERTIES

        public List<ProfileData> ProfilesDatas => _dshmConfigManager.GetListOfProfilesWithDefault();

        private readonly AppSnackbarMessagesService _appSnackbarMessagesService;

        [ObservableProperty] private bool _anyProfileSelected;

        // ----------------------------------------------------------- CONSTRUCTOR

        public ProfilesViewModel(AppSnackbarMessagesService appSnackbarMessagesService, DshmDevMan dshmDevMan, DshmConfigManager dshmConfigManager)
        {
            _dshmDevMan = dshmDevMan;
            _dshmConfigManager = dshmConfigManager;
            _appSnackbarMessagesService = appSnackbarMessagesService;
            UpdateProfileList();
        }
        
        public void UpdateProfileList()
        {
            List<ProfileViewModel> newList = new();
            foreach(ProfileData prof in ProfilesDatas)
            {
                newList.Add(new(prof, _appSnackbarMessagesService, _dshmConfigManager));
            }
            ProfilesViewModels = newList;
            UpdateGlobalProfileCheck();
        }

        public void UpdateGlobalProfileCheck()
        {
            foreach (ProfileViewModel profVM in ProfilesViewModels)
            {
                profVM.IsGlobal = (profVM.ProfileData == _dshmConfigManager.GlobalProfile);
            }
        }


        // ---------------------------------------- Methods
        
        public void OnNavigatedFrom() 
        {
            if(SelectedProfileVM != null && SelectedProfileVM.IsEditEnabled)
            {
                SelectedProfileVM.CancelChanges();
                _appSnackbarMessagesService.ShowProfileChangedCanceledMessage();
            }

            SelectedProfileVM = null;
        }

        public void OnNavigatedTo()
        {
        }

        partial void OnSelectedProfileVMChanged(ProfileViewModel? value)
        {
            AnyProfileSelected = (SelectedProfileVM != null);
        }

        [RelayCommand]
        private void SetprofileAsGlobal(ProfileViewModel? obj)
        {
            if (obj != null)
            {
                _dshmConfigManager.GlobalProfile = obj.ProfileData;
                _appSnackbarMessagesService.ShowGlobalProfileUpdatedMessage();
                _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
            }
            UpdateGlobalProfileCheck();
        }

        [RelayCommand]
        private void CreateProfile()
        {
            _dshmConfigManager.CreateProfile("New profile");
            _dshmConfigManager.SaveChanges();
            UpdateProfileList();
        }

        [RelayCommand]
        private void DeleteProfile(ProfileViewModel? obj)
        {
            if (obj == null) return;
            if (obj.ProfileData == ProfileData.DefaultProfile)
            {
                _appSnackbarMessagesService.ShowDefaultProfileEditingBlockedMessage();
                return;
            }
            _dshmConfigManager.DeleteProfile(obj.ProfileData);
            _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
            _appSnackbarMessagesService.ShowProfileDeletedMessage();
            UpdateProfileList();
        }
            
        }
}