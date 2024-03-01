using Godot;
using Newtonsoft.Json;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using FileAccess = System.IO.FileAccess;

namespace RoverControlApp.MVVM.Model;

public partial class LocalSettings : GodotObject
{
	private class PackedSettings
	{
		public Core.Settings.Camera? Camera { get; set; } = null;
		public Core.Settings.Mqtt? Mqtt { get; set; } = null;
		public Core.Settings.Joystick? Joystick { get; set; } = null;
		public Core.Settings.General? General { get; set; } = null;
	}

	private static readonly JsonSerializerSettings serializerSettings = new()
	{
		NullValueHandling = NullValueHandling.Include,
		Formatting = Formatting.Indented
	};

	private static readonly string _settingsPath = "user://RoverControlAppSettings.json";

	[Signal]
	public delegate void WholeSectionChangedEventHandler(StringName property);

	public LocalSettings()
	{
		_camera = new();
		_mqtt = new();
		_joystick = new();
		_general = new();

		if (LoadSettings()) return;

		ForceDefaultSettings();
	}

	public bool LoadSettings()
	{
		try
		{
			using var settingsFileAccess = Godot.FileAccess.Open(_settingsPath, Godot.FileAccess.ModeFlags.Read);

			if (settingsFileAccess is null)
				throw new FieldAccessException(Godot.FileAccess.GetOpenError().ToString());

			var serializedSettings = settingsFileAccess.GetAsText(true);

			var packedSettings = JsonConvert.DeserializeObject<PackedSettings>(serializedSettings);

			if (packedSettings is null)
				throw new DataException("unknown reason");

			Camera = packedSettings.Camera ?? new();
			Mqtt = packedSettings.Mqtt ?? new();
			Joystick = packedSettings.Joystick ?? new();
			General = packedSettings.General ?? new();
		}
		catch (Exception e)
		{
			EventLogger.LogMessage($"LocalSettings: ERROR Loading settings failed:\n\t{e}");
			return false;
		}

		EventLogger.LogMessage("LocalSettings: INFO Loading settings succeeded");
		return true;
	}

	public bool SaveSettings()
	{
		try
		{
			using var settingsFileAccess = Godot.FileAccess.Open(_settingsPath, Godot.FileAccess.ModeFlags.Write);

			if (settingsFileAccess is null)
				throw new FieldAccessException(Godot.FileAccess.GetOpenError().ToString());

			PackedSettings packedSettings = new()
			{
				Camera = Camera,
				Mqtt = Mqtt,
				Joystick = Joystick,
				General = General
			};

			settingsFileAccess.StoreString(JsonConvert.SerializeObject(packedSettings, serializerSettings));
		}
		catch (Exception e)
		{
			EventLogger.LogMessage($"LocalSettings: ERROR Saving settings failed with:\n\t{e}");
			return false;
		}

		EventLogger.LogMessage("LocalSettings: INFO Saving settings succeeded");
		return true;
	}

	public void ForceDefaultSettings()
	{
		EventLogger.LogMessage("LocalSettings: INFO Loading default settings");
		Camera = new();
		Mqtt = new();
		Joystick = new();
		General = new();
		SaveSettings();
	}


	[SettingsManagerVisible(customName: "Camera Settings")]
	public Core.Settings.Camera Camera
	{
		get => _camera;
		set
		{
			_camera = value;
			EmitSignal(SignalName.WholeSectionChanged, nameof(Camera));
		}
	}

	[SettingsManagerVisible(customName: "MQTT Settings")]
	public Core.Settings.Mqtt Mqtt
	{
		get => _mqtt;
		set
		{
			_mqtt = value;
			EmitSignal(SignalName.WholeSectionChanged, nameof(Mqtt));
		}
	}

	[SettingsManagerVisible(customName: "Joystick Settings")]
	public Core.Settings.Joystick Joystick
	{
		get => _joystick;
		set
		{
			_joystick = value;
			EmitSignal(SignalName.WholeSectionChanged, nameof(Joystick));
		}
	}

	[SettingsManagerVisible(customName: "General Settings")]
	public Core.Settings.General General
	{
		get => _general;
		set
		{
			_general = value;
			EmitSignal(SignalName.WholeSectionChanged, nameof(General));
		}
	}

	Core.Settings.Camera _camera;
	Core.Settings.Mqtt _mqtt;
	Core.Settings.Joystick _joystick;
	Core.Settings.General _general;
}


