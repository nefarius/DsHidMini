using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Serilog;
using Serilog.Core;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager
{
    /// <summary>
    ///  Class for managing user's dshidmini settings and applying them to the DsHidMini Configuration File
    /// </summary>
    public class DshmConfigManager
    {

        /// <summary>
        /// Singleton instace of the DshmConfigManager's user data
        /// </summary>
        private static readonly DshmConfigManagerUserData dshmManagerUserData = DshmConfigManagerUserData.Instance;
        /// <summary>
        /// Raised when the DsHidMini Configuraton File on disk is updated
        /// </summary>
        public event EventHandler<DshmUpdatedEventArgs> DshmConfigurationUpdated;
        /// <summary>
        /// Raised when the GlobalProfile is updated
        /// </summary>
        public event EventHandler GlobalProfileUpdated;


        // ----------------------------------------------------------- PROPERTIES
        
        /// <summary>
        /// Profile used for all new controllers and those configured to use Global Settings Mode.
        /// Reverts back to the Default Profile if the profile it's set to does not exist 
        /// </summary>
        public ProfileData GlobalProfile
        {
            get
            {
                ProfileData gp = GetProfile(dshmManagerUserData.GlobalProfileGuid);
                if (gp == null)
                {
                    Log.Logger.Debug("Global profile set to non-existing profile");
                    Log.Logger.Debug("Reverting Global profile to default profile.");
                    dshmManagerUserData.GlobalProfileGuid = ProfileData.DefaultGuid;
                    GlobalProfileUpdated?.Invoke(this, new());
                    gp = ProfileData.DefaultProfile;
                }
                return gp;
            }
            set
            {
                Log.Logger.Debug($"Setting profile {value.ProfileName} as Global Profile");
                dshmManagerUserData.GlobalProfileGuid = value.ProfileGuid;
                GlobalProfileUpdated?.Invoke(this,new());
            }
            
        }


        // ----------------------------------------------------------- CONSTRUCTOR

        public DshmConfigManager()
        {
            FixDevicesWithBlankProfiles();
        }

        /// <summary>
        /// Dshm Config Manager's User Data, containing the profile set as Global and Devices/Profiles datas
        /// </summary>
        private class DshmConfigManagerUserData
        {
            /// <summary>
            ///     Implicitly loads configuration from file.
            /// </summary>
            private static readonly Lazy<DshmConfigManagerUserData> AppConfigLazy =
                new Lazy<DshmConfigManagerUserData>(() => JsonDshmUserData
                    .Load<DshmConfigManagerUserData>(
                        GlobalUserDataFileName,
                        true,
                        GlobalUserDataDirectory));

            /// <summary>
            ///     Singleton instance of app configuration.
            /// </summary>
            public static DshmConfigManagerUserData Instance => AppConfigLazy.Value;

            /// <summary>
            /// Configuration file name
            /// </summary>
            [JsonIgnore]
            public static string GlobalUserDataFileName => "DshmUserData";

            public static string GlobalUserDataFolderName => "ControlApp";

            public static string GlobalUserDataDirectory
            {
                get
                {
                    var commonFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    return Path.Combine(commonFolder, GlobalUserDataFolderName);
                }
            }
            /// <summary>
            /// Guid of the profile set as global
            /// </summary>
            public Guid GlobalProfileGuid { get; set; } = ProfileData.DefaultGuid;
            /// <summary>
            /// List of profiles datas
            /// </summary>
            public List<ProfileData> Profiles { get; set; } = new();
            /// <summary>
            /// List of known devices datas
            /// </summary>
            public List<DeviceData> Devices { get; set; } = new();


            /// <summary>
            ///     Write changes to file.
            /// </summary>
            public void Save()
            {
                //
                // Store (modified) configuration to disk
                // 
                JsonDshmUserData.Save(
                    GlobalUserDataFileName,
                    this,
                    GlobalUserDataDirectory);
            }

        }

        // ----------------------------------------------------------- METHODS

        public void SaveChanges()
        {
            Log.Logger.Information("Saving DsHidMini User Data to disk.");
            dshmManagerUserData.Save();
        }

        /// <summary>
        /// Links Devices Datas back to the default profile if the profile they are set to use doesn't exist anymore,
        /// also reverting them to the Global Settings Mode if in Profile Setting Mode
        /// </summary>
        private void FixDevicesWithBlankProfiles()
        {
            foreach(DeviceData device in dshmManagerUserData.Devices)
            {
                if(GetProfile(device.GuidOfProfileToUse) == null)
                {
                    Log.Logger.Information($"Device {device.DeviceMac} linked to non-existing profile. Reverting link to default profile.");
                    device.GuidOfProfileToUse = ProfileData.DefaultGuid;
                    if (device.SettingsMode == SettingsModes.Profile)
                    {
                        Log.Logger.Information($"Device {device.DeviceMac} was in Profile Settings Mode while using a non-existing profile. Setting device back to Global Settings. ");
                        device.SettingsMode = SettingsModes.Global;
                    }

                }
            }
        }

        /// <summary>
        /// If it exists, returns the Profile Data identified by the given GUID
        /// </summary>
        /// <param name="profileGuid"></param>
        /// <returns>The profile data of the given GUID if it exists, null otherwise</returns>
        public ProfileData? GetProfile(Guid profileGuid)
        {
            ProfileData profile = null;

            foreach(ProfileData p in GetListOfProfilesWithDefault())
            {
                if(p.ProfileGuid == profileGuid)
                {
                    profile = p;
                    break;
                }
            }

            if (profile == null)
            {
                Log.Logger.Debug($"No profile with GUID {profileGuid} found.");
            }
            return profile;
        }

        /// <summary>
        /// Saves the DshmConfigManager configuration to disk and updates DsHidMini configuration file
        /// </summary>
        public void SaveChangesAndUpdateDsHidMiniConfigFile()
        {
            dshmManagerUserData.Save();
            ApplySettings();
        }

        /// <summary>
        /// Updates the DsHidMini configuration file on disk based on the global profile and each device's settings
        /// </summary>
        public void ApplySettings()
        {
            Log.Information("Updating DsHidMini configuration based on DsHidMini User Data");
            Log.Debug("Building DsHidMini configuration object based on DsHidMini User Data");
            var dshmConfiguration = new DshmConfiguration();
            DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(GlobalProfile.Settings, dshmConfiguration.Global);
           
            foreach(DeviceData dev in dshmManagerUserData.Devices)
            {
                var dshmDeviceData = new DshmDeviceData();
                dshmDeviceData.DeviceAddress = dev.DeviceMac;
                
                // Disable BT auto-pairing if in Disabled BT Pairing Mode
                dshmDeviceData.DeviceSettings.DisableAutoPairing =
                    (dev.BluetoothPairingMode == BluetoothPairingMode.Disabled) ? true : false;

                dshmDeviceData.DeviceSettings.DevicePairingMode = DshmManagerToDriverConversion.PairingModeManagerToDriver[dev.BluetoothPairingMode];

                dshmDeviceData.DeviceSettings.PairOnHotReload = dev.PairOnHotReload;

                // If using custom BT Pairing Mode, set the pairing address to the desired one. Otherwise, leave it blank so DsHidMini auto-pairs to current BT host
                dshmDeviceData.DeviceSettings.CustomPairingAddress =
                    (dev.BluetoothPairingMode == BluetoothPairingMode.Custom) ? dev.PairingAddress : null;

                switch (dev.SettingsMode)
                {
                    case SettingsModes.Custom:
                        DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(dev.Settings,dshmDeviceData.DeviceSettings);
                        break;
                    case SettingsModes.Profile:
                        ProfileData devprof = GetProfile(dev.GuidOfProfileToUse);
                        DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(devprof.Settings,dshmDeviceData.DeviceSettings);
                        break;

                    case SettingsModes.Global:
                    default:
                        // Device's in Global settings mode need to have empty settings so global settings are not overwritten
                        break;
                }
                dshmConfiguration.Devices.Add(dshmDeviceData);
            }

            Log.Logger.Debug("Configuration object built. Applying configuration.");
            var updateStatus = dshmConfiguration.ApplyConfiguration();
            DshmConfigurationUpdated?.Invoke(this, new DshmUpdatedEventArgs() { UpdatedSuccessfully = updateStatus});
        }

        public List<ProfileData> GetListOfProfilesWithDefault()
        {
            var userProfilesPlusDefault = new List<ProfileData>(dshmManagerUserData.Profiles);
            userProfilesPlusDefault.Insert(0, ProfileData.DefaultProfile);
            return userProfilesPlusDefault;
        }

        /// <summary>
        /// Adds to the Profile List a new profile with the given name and settings based on the default profile
        /// </summary>
        /// <param name="profileName">Name of the new profile</param>
        /// <returns>The created profile</returns>
        public ProfileData CreateProfile(string profileName)
        {
            ProfileData newProfile = new();
            newProfile.ProfileName = profileName;
            //newProfile.DiskFileName = profileName + ".json";
            dshmManagerUserData.Profiles.Add(newProfile);
            Log.Logger.Information($"Profile '{profileName}' created on DsHidMini User Data.");
            return newProfile;
        }

        /// <summary>
        /// Removes the given profile from the profile list.
        /// Devices set to use it will be updated to use the default profile and will be reverted back to Global settings mode if in Profile settings mode
        /// </summary>
        /// <param name="profile">The profile to be deleted</param>
        public void DeleteProfile(ProfileData profile)
        {
            Log.Logger.Information($"Deleting profile '{profile.ProfileName}'");
            if (profile == ProfileData.DefaultProfile) // Never remove Default profile from the list
            {
                Log.Logger.Information($"Default Profile can't be deleted.");
                return;
            }
            dshmManagerUserData.Profiles.Remove(profile);
            FixDevicesWithBlankProfiles();
        }
        
        public SettingsContext GetDeviceExpectedHidMode(DeviceData dev)
        {
            switch (dev.SettingsMode)
            {
                case SettingsModes.Custom:
                    return dev.Settings.HidMode.SettingsContext;
                    break;
                case SettingsModes.Profile:
                    return GetProfile(dev.GuidOfProfileToUse).Settings.HidMode.SettingsContext;
                    break;
                case SettingsModes.Global:
                default:
                    return GlobalProfile.Settings.HidMode.SettingsContext;
                    break;
            }
        }

        /// <summary>
        /// Gets the DsHidMini config. manager device data of a DsHidMini device.  If it does not exist, a new one will be created for it first before returning
        /// </summary>
        /// <param name="deviceMac">The MAC address of the DsHidMini device</param>
        /// <returns>The device data of the DsHidMini device</returns>
        public DeviceData GetDeviceData(string deviceMac)
        {
            Log.Logger.Information($"Getting data for device {deviceMac}.");
            foreach (DeviceData dev in dshmManagerUserData.Devices)
            {
                if (dev.DeviceMac == deviceMac)
                {
                    return dev;
                }
            }
            Log.Logger.Information($"Data for Device {deviceMac} does not exist. Creating new.");
            var newDevice = new DeviceData(deviceMac);
            newDevice.DeviceMac = deviceMac;
            dshmManagerUserData.Devices.Add(newDevice);
            return newDevice;
        }

        public class DshmUpdatedEventArgs : EventArgs
        {
            public bool UpdatedSuccessfully;
        }

    }
}