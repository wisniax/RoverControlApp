using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace RoverControlApp
{
	public class LocalSettings
	{
		public string CameraRtspIp { get; set; } = "rtsp://localhost:554";
		public string CameraPtzIp { get; set; } = "localhost:80";
		public string CameraLogin { get; set; } = "admin";
		public string CameraPassword { get; set; } = "admin";
		public bool CameraInverseAxis { get; set; } = false;
		public float JoyPadDeadzone { get; set; } = 0.2f;
		public double PtzRequestFrequency { get; set; } = 1.69;

		private readonly string _settingsPath = "user://RoverControlAppDefault.cfg";

		public LocalSettings()
		{
			if (LoadSettings()) return;
			if (!SaveSettings()) return;
			throw new Exception("Can't create settings file...");
		}

		public bool LoadSettings()
		{
			var config = new ConfigFile();
			Error err = config.Load(_settingsPath);
			if (err != Error.Ok) return false;

			string serializedSettings = (string)config.GetValue("Default", "defaultSettings");
			var settings = JsonSerializer.Deserialize<LocalSettings>(serializedSettings);

			CameraRtspIp = settings.CameraRtspIp;
			CameraPtzIp = settings.CameraPtzIp;
			CameraInverseAxis = settings.CameraInverseAxis;

			return true;
		}

		public bool SaveSettings()
		{
			var config = new ConfigFile();
			string serializedSettings = JsonSerializer.Serialize(this);
			config.SetValue("Default", "defaultSettings", serializedSettings);
			Error err = config.Save(_settingsPath);
			return err == Error.Ok;
		}
	}
}
