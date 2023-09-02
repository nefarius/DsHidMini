using System.Text.Json;
using System.Text.Json.Serialization;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

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
                    dshmManagerUserData.GlobalProfileGuid = ProfileData.DefaultGuid;
                    GlobalProfileUpdated?.Invoke(this, new());
                    gp = ProfileData.DefaultProfile;
                }
                return gp;
            }
            set
            {
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
                new Lazy<DshmConfigManagerUserData>(() => JsonApplicationConfiguration
                    .Load<DshmConfigManagerUserData>(
                        GlobalUserDataFileName,
                        true,
                        true));

            /// <summary>
            ///     Singleton instance of app configuration.
            /// </summary>
            public static DshmConfigManagerUserData Instance => AppConfigLazy.Value;

            /// <summary>
            /// Configuration file name
            /// </summary>
            [JsonIgnore]
            public static string GlobalUserDataFileName => "DshmUserData";
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
                JsonApplicationConfiguration.Save(
                    GlobalUserDataFileName,
                    this,
                    true);
            }

        }

        // ----------------------------------------------------------- METHODS

        public void SaveChanges()
        {
            dshmManagerUserData.Save();
        }

        /// <summary>
        /// Links Devices Datas back to the default profile if the profile they are set to use doesn't exist more,
        /// also reverting them to the Global Settings Mode if in Profile Setting Mode
        /// </summary>
        private void FixDevicesWithBlankProfiles()
        {
            foreach(DeviceData device in dshmManagerUserData.Devices)
            {
                if(GetProfile(device.GuidOfProfileToUse) == null)
                {
                    device.GuidOfProfileToUse = ProfileData.DefaultGuid;
                    if(device.SettingsMode == SettingsModes.Profile)
                        device.SettingsMode = SettingsModes.Global;
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
            var dshmConfiguration = new DshmConfiguration();
            GlobalProfile.Settings.ConvertAllToDSHM(dshmConfiguration.Global);
           
            foreach(DeviceData dev in dshmManagerUserData.Devices)
            {
                var dshmDeviceData = new DshmDeviceData();
                dshmDeviceData.DeviceAddress = dev.DeviceMac;
                
                // Disable BT auto-pairing if in Disabled BT Pairing Mode
                dshmDeviceData.DeviceSettings.DisableAutoPairing =
                    (dev.BluetoothPairingMode == BluetoothPairingMode.Disabled) ? true : false;
                // If using custom BT Pairing Mode, set the pairing address to the desired one. Otherwise, leave it blank so DsHidMini auto-pairs to current BT host
                dshmDeviceData.DeviceSettings.PairingAddress =
                    (dev.BluetoothPairingMode == BluetoothPairingMode.Custom) ? dev.PairingAddress : null;

                switch (dev.SettingsMode)
                {
                    case SettingsModes.Custom:
                        dev.Settings.ConvertAllToDSHM(dshmDeviceData.DeviceSettings);
                        break;
                    case SettingsModes.Profile:
                        ProfileData devprof = GetProfile(dev.GuidOfProfileToUse);
                        if(devprof == null)
                        {
                            // Maybe throw error here instead?
                            break; ; // If profile set for the controller does not exist anymore then leave settings blank so controller loads Global Profile
                        }
                        else
                        {
                            devprof.Settings.ConvertAllToDSHM(dshmDeviceData.DeviceSettings);
                        }
                        break;

                    case SettingsModes.Global:
                    default:
                        // Device's in Global settings mode need to have empty settings so global settings are not overwritten
                        break;
                }
                dshmConfiguration.Devices.Add(dshmDeviceData);
            }

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
            return newProfile;
        }

        /// <summary>
        /// Removes the given profile from the profile list.
        /// Devices set to use it will be updated to use the default profile and will be reverted back to Global settings mode if in Profile settings mode
        /// </summary>
        /// <param name="profile">The profile to be deleted</param>
        public void DeleteProfile(ProfileData profile)
        {
            if (profile == ProfileData.DefaultProfile) // Never remove Default profile from the list
            {
                return;
            }
            dshmManagerUserData.Profiles.Remove(profile);
            FixDevicesWithBlankProfiles();
        }

        /// <summary>
        /// Gets the DsHidMini config. manager device data of a DsHidMini device.  If it does not exist, a new one will be created for it first before returning
        /// </summary>
        /// <param name="deviceMac">The MAC address of the DsHidMini device</param>
        /// <returns>The device data of the DsHidMini device</returns>
        public DeviceData GetDeviceData(string deviceMac)
        {
            foreach (DeviceData dev in dshmManagerUserData.Devices)
            {
                if (dev.DeviceMac == deviceMac)
                {
                    return dev;
                }
            }
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