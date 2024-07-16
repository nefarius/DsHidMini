using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Services;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;

public partial class ProfileViewModel : ObservableObject
{
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
    private readonly DshmConfigManager _dshmConfigManager;

    [ObservableProperty]
    private bool _isGlobal;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private SettingsEditorViewModel _vmGroupsCont;

    public ProfileViewModel(ProfileData data, AppSnackbarMessagesService appSnackbarMessagesService,
        DshmConfigManager dshmConfigManager)
    {
        _dshmConfigManager = dshmConfigManager;
        _appSnackbarMessagesService = appSnackbarMessagesService;
        ProfileData = data;
        _name = data.ProfileName;
        _vmGroupsCont = new SettingsEditorViewModel(data.Settings);
    }

    public ProfileData ProfileData { get; }

    public bool IsEditEnabled
    {
        get => VmGroupsCont.AllowEditing;
        private set
        {
            Log.Logger.Information(
                $"Edition of profile '{ProfileData.ProfileName}' ({ProfileData.ProfileGuid}) set to {value}");
            VmGroupsCont.AllowEditing = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    public void EnableEditing()
    {
        if (ProfileData == ProfileData.DefaultProfile)
        {
            _appSnackbarMessagesService.ShowDefaultProfileEditingBlockedMessage();
            return;
        }

        IsEditEnabled = true;
    }

    [RelayCommand]
    public void SaveChanges()
    {
        Log.Logger.Information($"Saving changes to profile '{ProfileData.ProfileName}' ({ProfileData.ProfileGuid})");
        if (string.IsNullOrEmpty(Name))
        {
            Log.Logger.Debug("New profile name is null or empty. Setting name to generic one.");
            Name = "User Profile";
        }

        if (ProfileData.ProfileName != Name)
        {
            Log.Logger.Information($"Profile name changed from '{ProfileData.ProfileName}' to '{Name}'");
            ProfileData.ProfileName = Name;
        }

        VmGroupsCont.SaveAllChangesToBackingData(ProfileData.Settings);
        _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
        IsEditEnabled = false;

        _appSnackbarMessagesService.ShowProfileUpdateMessage();
    }

    [RelayCommand]
    public void CancelChanges()
    {
        Log.Logger.Debug($"Canceled changes to profile '{ProfileData.ProfileName}' ({ProfileData.ProfileGuid})");
        VmGroupsCont.LoadDatasToAllGroups(ProfileData.Settings);
        Name = ProfileData.ProfileName;
        IsEditEnabled = false;
    }
}