using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;

using Serilog;

namespace Nefarius.DsHidMini.ControlApp.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
    private readonly DshmConfigManager _dshmConfigManager;

    public ProfileData ProfileData { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private SettingsEditorViewModel _vmGroupsCont;
    [ObservableProperty] private bool _isGlobal = false;

    public bool IsEditEnabled
    {
        get => _vmGroupsCont.AllowEditing;
        private set
        {
            _vmGroupsCont.AllowEditing = value;
            this.OnPropertyChanged(nameof(IsEditEnabled));
        }
    }


    public ProfileViewModel(ProfileData data, AppSnackbarMessagesService appSnackbarMessagesService, DshmConfigManager dshmConfigManager)
    {
        _dshmConfigManager = dshmConfigManager;
        _appSnackbarMessagesService = appSnackbarMessagesService;
        ProfileData = data;
        _name = data.ProfileName;
        _vmGroupsCont = new(data.Settings);
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
        if(string.IsNullOrEmpty(_name))
        {
            Log.Logger.Debug("New profile name is null or empty. Setting name to generic one.");
            Name = "User Profile";
        }

        if (ProfileData.ProfileName != _name)
        {
            Log.Logger.Information($"Profile name changed from '{ProfileData.ProfileName}' to '{_name}'");
            ProfileData.ProfileName = _name;
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