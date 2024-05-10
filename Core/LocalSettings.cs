using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Godot;
using RoverControlApp.MVVM.ViewModel;
using RoverControlApp.MVVM.Model;
using FileAccess = System.IO.FileAccess;

namespace RoverControlApp.Core;

public class LocalSettings
{
	public class Camera
	{
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"(?i)(?:[0-9a-f]{1,4}:){7}[0-9a-f]{1,4}|(?:\d{1,3}\.){3}\d{1,3}|(?:http:\/\/|https:\/\/)\S+")]
		public string Ip { get; set; } = "192.168.1.35";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string RtspStreamPath { get; set; } = "/live/0/MAIN";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
		public int RtspPort { get; set; } = 554;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
		public int PtzPort { get; set; } = 80;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string Login { get; set; } = "admin";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string Password { get; set; } = "admin";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
		public bool InverseAxis { get; set; } = false;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
		public bool EnableRtspStream { get; set; } = true;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
		public bool EnablePtzControl { get; set; } = true;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "1;4;0.01;f;d")]
		public double PtzRequestFrequency { get; set; } = 2.69;
	}

	public class Mqtt
	{
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string BrokerIp { get; set; } = "broker.hivemq.com";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
		public int BrokerPort { get; set; } = 1883;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0.1;60;0.1;t;d")]
		public double PingInterval { get; set; } = 2.5;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicMain { get; set; } = "RappTORS";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicRoverControl { get; set; } = "RoverControl";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicManipulatorControl { get; set; } = "ManipulatorControl";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicRoverFeedback { get; set; } = "RoverFeedback";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicRoverStatus { get; set; } = "RoverStatus";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicRoverContainer { get; set; } = "RoverContainer";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicMissionStatus { get; set; } = "MissionStatus";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicKmlSetPoint { get; set; } = "KMLNode/SetPoint";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicWheelFeedback { get; set; } = "wheel_feedback";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
        public string TopicZedImuData { get; set; } = "ZedImuData";
        [SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
        public string TopicEStopStatus { get; set; } = "button_stop";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
		public string TopicKmlListOfActiveObj { get; set; } = "KMLNode/ActiveKMLObjects";

	}


	public class Vars
	{
		[SettingsManagerVisible(customName: "Camera Settings")]
		public Camera Camera { get; set; } = new();
		[SettingsManagerVisible(customName: "MQTT Settings")]
		public Mqtt Mqtt { get; set; } = new();
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
		public bool VerboseDebug { get; set; } = false;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;1;0.01;f;f")]
		public float JoyPadDeadzone { get; set; } = 0.15f;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
		public bool NewFancyRoverController { get; set; } = true;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
		public bool JoyVibrateOnModeChange { get; set; } = true;
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"-?[0-9]+;-?[0-9]+")]
		public string MissionControlPosition { get; set; } = "20;30";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"-?[0-9]+;-?[0-9]+")]
		public string MissionControlSize { get; set; } = "480;360";
		[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData:"0;60000;100;f;l", customTooltip: "How long is history [ms]")]
		public long BackCaptureLength { get; set; } = 15000;
	}

	[SettingsManagerVisible]
	public Vars? Settings { get; private set; }

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
		try
		{
			using var fs = new FileStream(_settingsPath, FileMode.Open, FileAccess.Read);
			using var sr = new StreamReader(fs);
			var serializedSettings = sr.ReadToEnd();
			sr.Close();
			fs.Close();
			Settings = JsonSerializer.Deserialize<Vars>(serializedSettings);
		}
		catch (Exception e)
		{
			MainViewModel.EventLogger.LogMessage($"Loading local settings failed: {e}");
			return false;
		}

		MainViewModel.EventLogger.LogMessage("Loading local settings succeeded");
		return true;
	}

	public bool SaveSettings()
	{
		try
		{
			if (!Directory.Exists(OS.GetUserDataDir())) Directory.CreateDirectory(OS.GetUserDataDir());
			using var fs = new FileStream(_settingsPath, FileMode.Create, FileAccess.Write);
			using var sw = new StreamWriter(fs);
			string serializedSettings = JsonSerializer.Serialize(Settings, new JsonSerializerOptions() { WriteIndented = true });

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

		MainViewModel.EventLogger.LogMessage("Saving settings succeeded");
		return true;
	}

	public void ForceDefaultSettings()
	{
		Settings = new Vars();
		SaveSettings();
	}
}


