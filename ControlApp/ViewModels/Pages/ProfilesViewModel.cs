using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Services;

using Serilog;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Pages
{

    public partial class ProfilesViewModel : ObservableObject, INavigationAware
    {
        // ----------------------------------------------------------- FIELDS

        private readonly DshmDevMan _dshmDevMan;
        private readonly DshmConfigManager _dshmConfigManager;

        [ObservableProperty] public List<UserControls.ProfileViewModel> _profilesViewModels;
        [ObservableProperty] private UserControls.ProfileViewModel? _selectedProfileVM = null;



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
            Log.Logger.Debug("Rebuilding profiles' ViewModels.");
            List<UserControls.ProfileViewModel> newList = new();
            foreach(ProfileData prof in ProfilesDatas)
            {
                newList.Add(new(prof, _appSnackbarMessagesService, _dshmConfigManager));
            }
            ProfilesViewModels = newList;
            UpdateGlobalProfileCheck();
        }

        public void UpdateGlobalProfileCheck()
        {
            Log.Logger.Debug("Updating Profiles' ViewModels 'Global' check");
            foreach (UserControls.ProfileViewModel profVM in ProfilesViewModels)
            {
                profVM.IsGlobal = (profVM.ProfileData == _dshmConfigManager.GlobalProfile);
            }
        }


        // ---------------------------------------- Methods
        
        public void OnNavigatedFrom() 
        {
            if(SelectedProfileVM != null && SelectedProfileVM.IsEditEnabled)
            {
                Log.Logger.Information("Navigated away from Profiles page mid-edition. Canceling changes.");
                SelectedProfileVM.CancelChanges();
                _appSnackbarMessagesService.ShowProfileChangedCanceledMessage();
            }

            SelectedProfileVM = null;
        }

        public void OnNavigatedTo()
        {
        }

        partial void OnSelectedProfileVMChanged(UserControls.ProfileViewModel? value)
        {
            AnyProfileSelected = (SelectedProfileVM != null);
        }

        [RelayCommand]
        private void SetprofileAsGlobal(UserControls.ProfileViewModel? obj)
        {
            if (obj != null)
            {
                Log.Logger.Information($"Setting profile '{obj.ProfileData.ProfileName}' ({obj.ProfileData.ProfileGuid}) as Global.");
                _dshmConfigManager.GlobalProfile = obj.ProfileData;
                _appSnackbarMessagesService.ShowGlobalProfileUpdatedMessage();
                _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
                UpdateGlobalProfileCheck();
            }
        }

        [RelayCommand]
        private void CreateProfile()
        {
            Log.Logger.Information("Creating new profile generic name.");
            _dshmConfigManager.CreateProfile("New profile");
            _dshmConfigManager.SaveChanges();
            UpdateProfileList();
        }

        [RelayCommand]
        private void DeleteProfile(UserControls.ProfileViewModel? obj)
        {
            if (obj == null) return;
            Log.Logger.Information($"Deleting profile '{obj.ProfileData.ProfileName}' ({obj.ProfileData.ProfileGuid})");
            if (obj.ProfileData == ProfileData.DefaultProfile)
            {
                Log.Logger.Information("Profile to be deleted is ControlApp's Default Profile. Delete action canceled.");
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