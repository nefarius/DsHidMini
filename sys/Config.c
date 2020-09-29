#include "Driver.h"
#include "Config.tmh"
#include <libconfig.h>


VOID DsConfig_LoadOrCreate(PDEVICE_CONTEXT Context)
{
	config_setting_t* root, * setting, * dev;
	config_t cfg;
	size_t characters;

	config_init(&cfg);

	if (!config_read_file(&cfg, DS_DRIVER_CFG_FILE_PATH))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_CONFIG,
			"Failed to load configuration: %s:%d - %s",
			config_error_file(&cfg),
			config_error_line(&cfg),
			config_error_text(&cfg)
		);

		root = config_root_setting(&cfg);
		setting = config_setting_get_member(root, "Devices");
		if (!setting)
			setting = config_setting_add(root, "Devices", CONFIG_TYPE_LIST);

		dev = config_setting_add(setting, NULL, CONFIG_TYPE_GROUP);

		PWSTR wideBuf = WdfMemoryGetBuffer(Context->InstanceId, NULL);
		PSTR buf = malloc(MAX_INSTANCE_ID_LENGTH);
		wcstombs_s(&characters, buf, MAX_INSTANCE_ID_LENGTH, wideBuf, MAX_INSTANCE_ID_LENGTH);

		setting = config_setting_add(dev, "InstanceId", CONFIG_TYPE_STRING);
		config_setting_set_string(setting, buf);
		free(buf);

		config_write_file(&cfg, DS_DRIVER_CFG_FILE_PATH);
	}
	else
	{
		setting = config_lookup(&cfg, "Devices");
		if (setting != NULL)
		{
			unsigned int count = config_setting_length(setting);
			unsigned int i;

			PWSTR wideBuf = WdfMemoryGetBuffer(Context->InstanceId, NULL);
			PSTR buf = malloc(MAX_INSTANCE_ID_LENGTH);
			wcstombs_s(&characters, buf, MAX_INSTANCE_ID_LENGTH, wideBuf, MAX_INSTANCE_ID_LENGTH);

			TraceEvents(TRACE_LEVEL_INFORMATION,
				TRACE_CONFIG,
				"!! %d %s",
				count, buf
			);

			for (i = 0; i < count; ++i)
			{
				dev = config_setting_get_elem(setting, i);

				const char* instId;

				if (config_setting_lookup_string(dev, "InstanceId", &instId)
					&& strcmp(instId, buf) == 0)
				{
					TraceEvents(TRACE_LEVEL_INFORMATION,
						TRACE_CONFIG,
						"!! Found config for instanceId: %s",
						buf
					);

					config_setting_lookup_int(
						dev,
						"HidDeviceMode",
						(int*)&Context->Configuration.HidDeviceMode);
					config_setting_lookup_bool(
						dev,
						"MuteDigitalPressureButtons",
						(int*)&Context->Configuration.MuteDigitalPressureButtons);
					config_setting_lookup_int(
						dev,
						"VendorId",
						(int*)&Context->Configuration.VendorId);
					config_setting_lookup_int(
						dev,
						"ProductId",
						(int*)&Context->Configuration.ProductId);
					config_setting_lookup_int(
						dev,
						"VersionNumber",
						(int*)&Context->Configuration.VersionNumber);

					break;
				}
			}

			free(buf);
		}
	}

	config_destroy(&cfg);
}

VOID DsConfig_Store(PDEVICE_CONTEXT Context)
{
	config_t cfg;
	config_setting_t* dev = NULL, * setting;
	size_t characters;
	
	PDS_DRIVER_CONFIGURATION pCfg = &Context->Configuration;

	config_init(&cfg);

	if (!config_read_file(&cfg, DS_DRIVER_CFG_FILE_PATH))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_CONFIG,
			"Failed to load configuration: %s:%d - %s",
			config_error_file(&cfg),
			config_error_line(&cfg),
			config_error_text(&cfg)
		);
	}
	else
	{
		setting = config_lookup(&cfg, "Devices");
		if (setting != NULL)
		{
			unsigned int count = config_setting_length(setting);
			unsigned int i;

			PWSTR wideBuf = WdfMemoryGetBuffer(Context->InstanceId, NULL);
			PSTR buf = malloc(MAX_INSTANCE_ID_LENGTH);
			wcstombs_s(&characters, buf,MAX_INSTANCE_ID_LENGTH, wideBuf, MAX_INSTANCE_ID_LENGTH);

			TraceEvents(TRACE_LEVEL_INFORMATION,
				TRACE_CONFIG,
				"!! %d %s",
				count, buf
			);

			//
			// Search for existing configuration to update
			// 
			for (i = 0; i < count; ++i)
			{
				dev = config_setting_get_elem(setting, i);

				const char* instId;

				if (config_setting_lookup_string(dev, "InstanceId", &instId)
					&& strcmp(instId, buf) == 0)
				{
					TraceEvents(TRACE_LEVEL_INFORMATION,
						TRACE_CONFIG,
						"!! Found config for instanceId: %s",
						buf
					);

					break;
				}

				dev = NULL;
			}

			//
			// Not found, create new group
			// 
			if (!dev)
			{
				dev = config_setting_add(setting, NULL, CONFIG_TYPE_GROUP);
				setting = config_setting_add(dev, "InstanceId", CONFIG_TYPE_STRING);
				config_setting_set_string(setting, buf);
			}

			//
			// HidDeviceMode
			// 
			setting = config_setting_get_member(dev, "HidDeviceMode");
			if (!setting)
				setting = config_setting_add(dev, "HidDeviceMode", CONFIG_TYPE_INT);
			config_setting_set_int(setting, pCfg->HidDeviceMode);

			//
			// MuteDigitalPressureButtons
			// 
			setting = config_setting_get_member(dev, "MuteDigitalPressureButtons");
			if (!setting)
				setting = config_setting_add(dev, "MuteDigitalPressureButtons", CONFIG_TYPE_BOOL);
			config_setting_set_int(setting, pCfg->MuteDigitalPressureButtons);

			//
			// VendorId
			// 
			setting = config_setting_get_member(dev, "VendorId");
			if (!setting)
				setting = config_setting_add(dev, "VendorId", CONFIG_TYPE_INT);
			config_setting_set_int(setting, pCfg->VendorId);

			//
			// ProductId
			// 
			setting = config_setting_get_member(dev, "ProductId");
			if (!setting)
				setting = config_setting_add(dev, "ProductId", CONFIG_TYPE_INT);
			config_setting_set_int(setting, pCfg->ProductId);

			//
			// VersionNumber
			// 
			setting = config_setting_get_member(dev, "VersionNumber");
			if (!setting)
				setting = config_setting_add(dev, "VersionNumber", CONFIG_TYPE_INT);
			config_setting_set_int(setting, pCfg->VersionNumber);

			free(buf);
		}
	}

	config_write_file(&cfg, DS_DRIVER_CFG_FILE_PATH);

	config_destroy(&cfg);
}
