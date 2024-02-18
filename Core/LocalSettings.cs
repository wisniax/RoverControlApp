using Godot;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FileAccess = System.IO.FileAccess;

namespace RoverControlApp.MVVM.Model;

public partial class LocalSettings : GodotObject
{
	[Signal]
	public delegate void SettingChangedEventHandler(StringName property);

	CameraSettings _camera;
	[SettingsManagerVisible(customName: "Camera Settings")]
	public CameraSettings Camera
	{
		get => _camera;
		set
		{
			_camera = value;
			EmitSignal(SignalName.SettingChanged, nameof(Camera));
		}
	}

	MqttSettings _mqtt;
	[SettingsManagerVisible(customName: "MQTT Settings")]
	public MqttSettings Mqtt
	{
		get => _mqtt;
		set
		{
			_mqtt = value;
			EmitSignal(SignalName.SettingChanged, nameof(Mqtt));
		}
	}

	JoystickSettings _joystick;
	[SettingsManagerVisible(customName: "Joystick Settings")]
	public JoystickSettings Joystick
	{
		get => _joystick;
		set
		{
			_joystick = value;
			EmitSignal(SignalName.SettingChanged, nameof(Joystick));
		}
	}

	GeneralSettings _general;
	[SettingsManagerVisible(customName: "General Settings")]
	public GeneralSettings General
	{
		get => _general;
		set
		{
			_general = value;
			EmitSignal(SignalName.SettingChanged, nameof(General));
		}
	}

	static readonly JsonSerializerOptions deserializerOptions = new JsonSerializerOptions
	{
		UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
	};


	private readonly string _settingsPath = Path.Join(OS.GetUserDataDir(), "RoverControlAppSettings.json");

	public LocalSettings()
	{
		if (LoadSettings()) return;

		ForceDefaultSettings();
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

			JsonDocument jsonSettings = JsonDocument.Parse(serializedSettings);

			foreach (var element in jsonSettings.RootElement.EnumerateObject().AsEnumerable())
			{
				switch (element.Name)
				{
					case nameof(Camera):
						Camera = element.Value.Deserialize<CameraSettings>(deserializerOptions);
						break;
					case nameof(Mqtt):
						Mqtt = element.Value.Deserialize<MqttSettings>(deserializerOptions);
						break;
					case nameof(Joystick):
						Joystick = element.Value.Deserialize<JoystickSettings>(deserializerOptions);
						break;
					case nameof(General):
						General = element.Value.Deserialize<GeneralSettings>(deserializerOptions);
						break;
				}
			}
		}
		catch (Exception e)
		{
			EventLogger.LogMessage($"LocalSettings: ERROR Loading settings failed:\n\t{e}");
			return false;
		}

		//if any instance is same as default constructed, well its not loaded.
		if (new List<object> { Camera, Mqtt, Joystick, General }.Exists(obj => Activator.CreateInstance(obj.GetType())!.Equals(obj)))
		{
			EventLogger.LogMessage($"LocalSettings: ERROR Loading settings failed: Settings corrupted!");
			return false;
		}

		EventLogger.LogMessage("LocalSettings: Loading settings succeeded");
		return true;
	}

	public bool SaveSettings()
	{
		try
		{
			if (!Directory.Exists(OS.GetUserDataDir())) Directory.CreateDirectory(OS.GetUserDataDir());
			using var fs = new FileStream(_settingsPath, FileMode.Create, FileAccess.Write);
			using var sw = new StreamWriter(fs);

			JsonNode? cameraNode = JsonSerializer.SerializeToNode(Camera);
			JsonNode? mqttNode = JsonSerializer.SerializeToNode(Mqtt);
			JsonNode? joystickNode = JsonSerializer.SerializeToNode(Joystick);
			JsonNode? generalNode = JsonSerializer.SerializeToNode(General);

			if (new List<object?> { Camera, Mqtt, Joystick, General }.Exists(obj => obj is null))
				throw new JsonException("Cannot convert to JsonNode");

			JsonObject jsonSettings = new()
			{
				{ nameof(Camera), cameraNode },
				{ nameof(Mqtt), mqttNode },
				{ nameof(Joystick), joystickNode },
				{ nameof(General), generalNode }
			};

			jsonSettings.ToJsonString();

			sw.WriteLine(jsonSettings);
			sw.Flush();
			sw.Close();
			fs.Close();
		}
		catch (Exception e)
		{
			EventLogger.LogMessage($"LocalSettings: ERROR Saving settings failed with:\n\t{e}");
			return false;
		}

		EventLogger.LogMessage("LocalSettings: Saving settings succeeded");
		return true;
	}

	public void ForceDefaultSettings()
	{
		EventLogger.LogMessage("LocalSettings: Loading default settings");
		Camera = CameraSettings.DEFAULT;
		Mqtt = MqttSettings.DEFAULT;
		Joystick = JoystickSettings.DEFAULT;
		General = GeneralSettings.DEFAULT;
		SaveSettings();
	}
}


