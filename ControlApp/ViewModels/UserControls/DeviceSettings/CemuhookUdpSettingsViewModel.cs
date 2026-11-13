using System.Collections.ObjectModel;
using System.Net.NetworkInformation;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Motion;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public class CemuhookUdpSettingsViewModel : DeviceSettingsViewModel
{
    private readonly CemuhookUdpSettings _tempBackingData = new();
    private readonly Func<IReadOnlyList<CemuhookListenAddress>> _discover;
    private readonly ObservableCollection<CemuhookListenAddress> _listenAddresses = [];
    private IReadOnlyList<CemuhookListenAddress> _availableAddresses = CemuhookListenAddresses.CreateOptions([]);
    private string? _serverError;
    private string? _discoveryError;
    private string? _loadedSharedAddress;
    private int _loadedSharedPort;
    private int? _dsuSlot;
    public string SlotDisplay => _dsuSlot is { } slot ? $"DSU slot {slot}" : "No DSU slot assigned";

    public CemuhookUdpSettingsViewModel() : this(CemuhookListenAddresses.Discover)
    {
    }

    internal CemuhookUdpSettingsViewModel(Func<IReadOnlyList<CemuhookListenAddress>> discover)
    {
        _discover = discover;
        ListenAddresses = new(_listenAddresses);
        RefreshListenAddresses();
    }

    protected override DeviceSubSettings _mySubSetting => _tempBackingData;
    public override SettingsModeGroups Group { get; } = SettingsModeGroups.CemuhookUdpBridge;

    public ReadOnlyObservableCollection<CemuhookListenAddress> ListenAddresses { get; }

    public CemuhookListenAddress? SelectedListenAddress
    {
        get => ListenAddresses.FirstOrDefault(option =>
            option.Address == CemuhookListenAddresses.AddressKey(IpAddress));
        set
        {
            // Refreshing the choices can briefly clear selection; never overwrite saved settings with that null.
            if (value != null)
                IpAddress = value.Address;
        }
    }

    public bool Enabled
    {
        get => _tempBackingData.Enabled;
        set
        {
            if (_tempBackingData.Enabled == value)
                return;
            _tempBackingData.Enabled = value;
            _serverError = null;
            OnPropertyChanged();
            NotifyValidation();
        }
    }

    public string IpAddress
    {
        get => _tempBackingData.IpAddress;
        set
        {
            if (_tempBackingData.IpAddress == value)
                return;
            _tempBackingData.IpAddress = value;
            _serverError = null;
            RebuildOptions();
            OnPropertyChanged();
        }
    }

    public int Port
    {
        get => _tempBackingData.Port;
        set
        {
            if (_tempBackingData.Port == value)
                return;
            _tempBackingData.Port = value;
            _serverError = null;
            OnPropertyChanged();
            NotifyValidation();
        }
    }

    public string? ValidationError => !Enabled ? null
        : CemuhookListenAddresses.ValidateEndpoint(IpAddress, Port)
          ?? (SelectedListenAddress?.IsAvailable == true ? null
              : "The saved listen address is unavailable on this PC. Choose another address.");

    public bool CanApply => ValidationError == null;
    public string? ErrorMessage => !Enabled ? null : ValidationError ?? _serverError ?? _discoveryError;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public void RefreshListenAddresses()
    {
        try
        {
            _availableAddresses = _discover();
            _discoveryError = null;
        }
        catch (NetworkInformationException)
        {
            // Retain the last known choices if Windows cannot enumerate adapters temporarily.
            _discoveryError = "Could not refresh network addresses. Open the listen-address list to try again.";
        }

        RebuildOptions();
    }

    public override void NotifyAllPropertiesHaveChanged()
    {
        _serverError = null;
        RefreshListenAddresses();
        base.NotifyAllPropertiesHaveChanged();
    }

    public void LoadSharedEndpoint(CemuhookUdpSettings endpoint)
    {
        IpAddress = endpoint.IpAddress;
        Port = endpoint.Port;
        _loadedSharedAddress = endpoint.IpAddress;
        _loadedSharedPort = endpoint.Port;
    }

    public void RefreshSharedEndpoint(CemuhookUdpSettings endpoint)
    {
        if (_loadedSharedAddress == null || (IpAddress == _loadedSharedAddress && Port == _loadedSharedPort))
            LoadSharedEndpoint(endpoint);
    }

    public void SaveSharedEndpoint(CemuhookUdpSettings endpoint)
    {
        endpoint.IpAddress = IpAddress;
        endpoint.Port = Port;
    }

    public void ReportServerError(string? message, string address, int port, int? slot = null)
    {
        _dsuSlot = slot;
        OnPropertyChanged(nameof(SlotDisplay));
        // Do not overlay an old endpoint's error while the user is editing a new endpoint.
        _serverError = Port == port && CemuhookListenAddresses.AddressKey(IpAddress) == CemuhookListenAddresses.AddressKey(address)
            ? message
            : null;
        NotifyValidation();
    }

    private void RebuildOptions()
    {
        string savedAddress = CemuhookListenAddresses.AddressKey(IpAddress);
        var options = _availableAddresses.ToList();
        if (!options.Any(option => option.Address == savedAddress))
        {
            string label = string.IsNullOrWhiteSpace(savedAddress) ? "No address saved" : savedAddress;
            options.Add(new(savedAddress, $"{label} — unavailable", IsAvailable: false));
        }

        // Keep ItemsSource stable. Replacing it can clear the target's SelectedItem
        // after the SelectedListenAddress binding has already refreshed.
        if (!_listenAddresses.SequenceEqual(options))
        {
            _listenAddresses.Clear();
            foreach (var option in options)
                _listenAddresses.Add(option);
        }
        OnPropertyChanged(nameof(SelectedListenAddress));
        NotifyValidation();
    }

    private void NotifyValidation()
    {
        OnPropertyChanged(nameof(ValidationError));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasError));
    }
}
