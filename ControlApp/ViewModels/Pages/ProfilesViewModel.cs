using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;

using Wpf.Ui.Abstractions.Controls;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

public partial class ProfilesViewModel : ObservableObject, INavigationAware
{
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;

    private readonly DshmConfigManager _dshmConfigManager;
    // ----------------------------------------------------------- FIELDS

    private readonly DshmDevMan _dshmDevMan;

    [ObservableProperty]
    private bool _anyProfileSelected;

    [ObservableProperty]
    private List<ProfileViewModel> _profilesViewModels;

    [ObservableProperty]
    private ProfileViewModel? _selectedProfileVM;

    // ----------------------------------------------------------- CONSTRUCTOR

    public ProfilesViewModel(AppSnackbarMessagesService appSnackbarMessagesService, DshmDevMan dshmDevMan,
        DshmConfigManager dshmConfigManager)
    {
        _dshmDevMan = dshmDevMan;
        _dshmConfigManager = dshmConfigManager;
        _appSnackbarMessagesService = appSnackbarMessagesService;
        UpdateProfileList();
    }


    // ----------------------------------------------------------- PROPERTIES

    public List<ProfileData> ProfilesDatas => _dshmConfigManager.GetListOfProfilesWithDefault();


    // ---------------------------------------- Methods

    public Task OnNavigatedFromAsync()
    {
        if (SelectedProfileVM is { IsEditEnabled: true })
        {
            Log.Logger.Information("Navigated away from Profiles page mid-edition. Canceling changes.");
            SelectedProfileVM.CancelChanges();
            _appSnackbarMessagesService.ShowProfileChangedCanceledMessage();
        }

        SelectedProfileVM = null;

        return Task.CompletedTask;
    }

    public Task OnNavigatedToAsync()
    {
        return Task.CompletedTask;
    }

    private void UpdateProfileList()
    {
        Log.Logger.Debug("Rebuilding profiles' ViewModels.");
        List<ProfileViewModel> newList = new();
        foreach (ProfileData prof in ProfilesDatas)
        {
            newList.Add(new ProfileViewModel(prof, _appSnackbarMessagesService, _dshmConfigManager));
        }

        ProfilesViewModels = newList;
        UpdateGlobalProfileCheck();
    }

    private void UpdateGlobalProfileCheck()
    {
        Log.Logger.Debug("Updating Profiles' ViewModels 'Global' check");
        foreach (ProfileViewModel profVM in ProfilesViewModels)
        {
            profVM.IsGlobal = profVM.ProfileData == _dshmConfigManager.GlobalProfile;
        }
    }

    partial void OnSelectedProfileVMChanged(ProfileViewModel? value)
    {
        AnyProfileSelected = SelectedProfileVM != null;
    }

    [RelayCommand]
    private void SetProfileAsGlobal(ProfileViewModel? obj)
    {
        if (obj != null)
        {
            Log.Logger.Information(
                "Setting profile '{ProfileDataProfileName}' ({ProfileDataProfileGuid}) as Global.", obj.ProfileData
                    .ProfileName, obj.ProfileData.ProfileGuid);
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
    private void DeleteProfile(ProfileViewModel? obj)
    {
        if (obj == null)
        {
            return;
        }

        Log.Logger.Information(
            "Deleting profile '{ProfileDataProfileName}' ({ProfileDataProfileGuid})", obj.ProfileData.ProfileName, obj
                .ProfileData.ProfileGuid);
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