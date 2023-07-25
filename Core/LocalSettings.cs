using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RoverControlApp.MVVM.ViewModel;
using RoverControlApp.MVVM.Model;
using FileAccess = System.IO.FileAccess;

namespace RoverControlApp.Core;

public class LocalSettings
{
	public class LocalSettingsCamera
	{
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string CameraIp { get; set; } = "192.168.5.35";
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string CameraPtzPort { get; set; } = "80";
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string CameraRtspPort { get; set; } = "554";
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string CameraRtspStreamPath { get; set; } = "/live/0/MAIN";
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string CameraLogin { get; set; } = "admin";
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string CameraPassword { get; set; } = "admin";
		[SettingsManagerVisible(TreeItem.TreeCellMode.Check)]
		public bool CameraInverseAxis { get; set; } = false;
		[SettingsManagerVisible(TreeItem.TreeCellMode.Check)]
		public bool EnableRtspStream { get; set; } = true;
		[SettingsManagerVisible(TreeItem.TreeCellMode.Check)]
		public bool EnablePtzControl { get; set; } = true;
		[SettingsManagerVisible(TreeItem.TreeCellMode.Range,"1;4;0.01;f;d")]
		public double PtzRequestFrequency { get; set; } = 2.69;
	}

	public class LocalSettingsMqtt
	{
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string MqttBrokerIp { get; set; } = "broker.hivemq.com";
		[SettingsManagerVisible(TreeItem.TreeCellMode.Range, "0;65535;1;f;i")]
		public int MqttBrokerPort { get; set; } = 1883;
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string MqttTopic { get; set; } = "RappTORS";
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string MqttTopicRoverControl { get; set; } = "RoverControl";
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string MqttTopicManipulatorControl { get; set; } = "ManipulatorControl";
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string MqttTopicRoverFeedback { get; set; } = "RoverFeedback";
		[SettingsManagerVisible(TreeItem.TreeCellMode.String)]
		public string MqttTopicRoverStatus { get; set; } = "RoverStatus";
	}


	public class LocalSettingsVars
	{
		[SettingsManagerVisible]
		public LocalSettingsCamera Camera { get; set; } = new();
		[SettingsManagerVisible]
		public LocalSettingsMqtt Mqtt { get; set; } = new();
		[SettingsManagerVisible(TreeItem.TreeCellMode.Check)]
		public bool VerboseDebug { get; set; } = false;
		[SettingsManagerVisible(TreeItem.TreeCellMode.Range, "0;1;0.01;f;f")]
		public float JoyPadDeadzone { get; set; } = 0.15f;
		[SettingsManagerVisible(TreeItem.TreeCellMode.Check)]
		public bool NewFancyRoverController { get; set; } = false;

	}

	[SettingsManagerVisible]
	public LocalSettingsVars Settings { get; private set; }

	private readonly string _settingsPath = Path.Join(OS.GetUserDataDir(), "RoverControlAppSettings.json");

	public LocalSettings()
	{
		if (LoadSettings())
			return;
		Settings = new();
		if (SaveSettings())
			return;
		throw new Exception("Can't create settings file...");
	}

	public bool LoadSettings()
	{
		if (!Directory.Exists(OS.GetUserDataDir()))
			return false;
		if (!File.Exists(_settingsPath))
			return false;
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

		Settings = JsonSerializer.Deserialize<LocalSettingsVars>(serializedSettings);
		MainViewModel.EventLogger.LogMessage("Loading local settings succeeded");
		return true;
	}

	public bool SaveSettings()
	{
		try
		{
			if (!Directory.Exists(OS.GetUserDataDir()))
				Directory.CreateDirectory(OS.GetUserDataDir());
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
		Settings = new LocalSettingsVars();
		SaveSettings();
	}
}
