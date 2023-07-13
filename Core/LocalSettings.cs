using System;
using System.IO;
using System.Text.Json;
using Godot;
using RoverControlApp.MVVM.ViewModel;
using FileAccess = System.IO.FileAccess;

namespace RoverControlApp.Core
{

	public class LocalSettings
	{
		public class Camera
		{
			public string Ip { get; set; } = "192.168.5.35";
			public string PtzPort { get; set; } = "80";
			public string RtspPort { get; set; } = "554";
			public string RtspStreamPath { get; set; } = "/live/0/MAIN";
			public string Login { get; set; } = "admin";
			public string Password { get; set; } = "admin";
			public bool InverseAxis { get; set; } = false;
			public bool EnableRtspStream { get; set; } = true;
			public bool EnablePtzControl { get; set; } = true;
			public double PtzRequestFrequency { get; set; } = 2.69;
		}

		public class Mqtt
		{
			public string BrokerIp { get; set; } = "broker.hivemq.com";
			public int BrokerPort { get; set; } = 1883;
			public double PingInterval { get; set; } = 2.5;
			public string MainTopic { get; set; } = "RappTORS";
			public string TopicRoverControl { get; set; } = "RoverControl";
			public string TopicManipulatorControl { get; set; } = "ManipulatorControl";
			public string TopicRoverFeedback { get; set; } = "RoverFeedback";
			public string TopicRoverStatus { get; set; } = "RoverStatus";
		}

		public class Vars
		{
			public Camera Camera { get; set; } = new();
			public Mqtt Mqtt { get; set; } = new();
			public bool VerboseDebug { get; set; } = false;
			public float JoyPadDeadzone { get; set; } = 0.15f;
			public bool NewFancyRoverController { get; set; } = false;
		}

		public Vars Settings { get; private set; }

		private readonly string _settingsPath = Path.Join(OS.GetUserDataDir(), "RoverControlAppSettings.json");

		public LocalSettings()
		{
			if (LoadSettings()) return;
			Settings = new();
			if (SaveSettings()) return;
			throw new Exception("Can't create settings file...");
		}

		public bool LoadSettings()
		{
			if (!Directory.Exists(OS.GetUserDataDir())) return false;
			if (!File.Exists(_settingsPath)) return false;
			string serializedSettings;
			try
			{
				using var fs = new FileStream(_settingsPath, FileMode.Open, FileAccess.Read);
				using var sr = new StreamReader(fs);
				serializedSettings = sr.ReadToEnd();
				sr.Close();
				fs.Close();
			}
			catch (Exception e)
			{
				MainViewModel.EventLogger.LogMessage($"Loading local settings failed: {e}");
				return false;
			}

			Settings = JsonSerializer.Deserialize<Vars>(serializedSettings);
			MainViewModel.EventLogger.LogMessage("Loading local settings succeeded");
			return true;
		}

		public bool SaveSettings()
		{
			try
			{
				if (!Directory.Exists(OS.GetUserDataDir())) Directory.CreateDirectory(OS.GetUserDataDir());
				// if (!File.Exists(OS.GetUserDataDir())) File.Create(OS.GetUserDataDir());
				using var fs = new FileStream(_settingsPath, FileMode.Create, FileAccess.Write);
				using var sw = new StreamWriter(fs);
				string serializedSettings = JsonSerializer.Serialize(Settings);

				sw.WriteLine(serializedSettings);
				sw.Flush();
				sw.Close();
				fs.Close();
			}
			catch (Exception e)
			{
				MainViewModel.EventLogger.LogMessage($"Saving settings failed with: {e}");
				return false;
			}

			//config.SetValue("Default", "defaultSettings", serializedSettings);
			//Error err = config.Save(_settingsPath);

			MainViewModel.EventLogger.LogMessage("Saving settings succeeded");
			return true;

		}

		public void ForceDefaultSettings()
		{
			Settings = new Vars();
			SaveSettings();
		}
	}

}
