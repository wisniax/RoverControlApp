using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp
{

	public class LocalSettings
	{
		public class LocalSettingsVars
		{
			public string CameraIp { get; set; } = "192.168.5.35";
			public string CameraPtzPort { get; set; } = "80";
			public string CameraRtspPort { get; set; } = "554";
			public string CameraRtspStreamPath { get; set; } = "/live/0/MAIN";
			public string CameraLogin { get; set; } = "admin";
			public string CameraPassword { get; set; } = "admin";
			public bool CameraInverseAxis { get; set; } = false;
			public bool EnableRtspStream {get; set; } = true;
			public bool EnablePtzControl {get; set; } = true;
			public bool VerboseDebug {get; set; } = false;
			public float JoyPadDeadzone { get; set; } = 0.15f;
			public double PtzRequestFrequency { get; set; } = 2.69;
			public string MqttBrokerIp { get; set; } = "broker.hivemq.com";
			public int MqttBrokerPort { get; set; } = 1883;
			public string MqttTopic { get; set; } = "RappTORS";
			public string MqttTopicJoyStatus { get; set; } = "JoyStatus";
			public string MqttTopicRoverControl { get; set; } = "RoverControl";
			public string MqttTopicManipulatorControl { get; set; } = "ManipulatorControl";
			public string MqttTopicRoverFeedback { get; set; } = "RoverFeedback";
			public string MqttTopicRoverStatus { get; set; } = "RoverStatus";
		}

		public LocalSettingsVars Settings { get; private set; }

		private readonly string _settingsPath = "user://RoverControlAppDefault.cfg";


		public LocalSettings()
		{
			if (LoadSettings()) return;
			Settings = new();
			if (SaveSettings()) return;
			throw new Exception("Can't create settings file...");
		}

		public bool LoadSettings()
		{
			var config = new ConfigFile();
			Error err = config.Load(_settingsPath);
			if (err != Error.Ok)
			{
				MainViewModel.EventLogger.LogMessage($"Loading local settings failed: {err}");
				return false;
			}
			string serializedSettings = (string)config.GetValue("Default", "defaultSettings");
			Settings = JsonSerializer.Deserialize<LocalSettingsVars>(serializedSettings);
			MainViewModel.EventLogger.LogMessage("Loading local settings succeeded");
			return true;
		}

		public bool SaveSettings()
		{
			var config = new ConfigFile();
			string serializedSettings = JsonSerializer.Serialize(Settings);
			config.SetValue("Default", "defaultSettings", serializedSettings);
			Error err = config.Save(_settingsPath);

			if (err != Error.Ok)
			{
				MainViewModel.EventLogger.LogMessage($"Saving settings failed: {err}");
				return false;
			}

			MainViewModel.EventLogger.LogMessage("Saving settings succeeded");
			return true;

		}

		public void ForceDefaultSettings()
		{
			Settings = new LocalSettingsVars();
			SaveSettings();
		}
	}

}
