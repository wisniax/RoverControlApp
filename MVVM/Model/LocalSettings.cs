using Godot;
using RoverControlApp.Core;
using System;
using System.Data;
using System.Text.Json;

namespace RoverControlApp.MVVM.Model;

public partial class LocalSettings : Node
{
	private sealed class PackedSettings
	{
		public Settings.Camera? Camera { get; set; } = null;
		public Settings.Mqtt? Mqtt { get; set; } = null;
		public Settings.Joystick? Joystick { get; set; } = null;
		public Settings.General? General { get; set; } = null;
	}

	private JsonSerializerOptions serializerOptions = new() { WriteIndented = true };

	private static readonly string _settingsPath = "user://RoverControlAppSettings.json";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public static LocalSettings Singleton { get; private set; }
#pragma warning restore CS8618 

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

	public override void _Ready()
	{
		//first ever call to _Ready will be on singletone instance.
		Singleton ??= this;
	}

	public bool LoadSettings()
	{
		try
		{
			using var settingsFileAccess = Godot.FileAccess.Open(_settingsPath, Godot.FileAccess.ModeFlags.Read);

			if (settingsFileAccess is null)
				throw new FieldAccessException(Godot.FileAccess.GetOpenError().ToString());

			var serializedSettings = settingsFileAccess.GetAsText(true);

			var packedSettings = JsonSerializer.Deserialize<PackedSettings>(serializedSettings, serializerOptions);

			if (packedSettings is null)
				throw new DataException("unknown reason");

			Camera = packedSettings.Camera ?? new();
			Mqtt = packedSettings.Mqtt ?? new();
			Joystick = packedSettings.Joystick ?? new();
			General = packedSettings.General ?? new();
		}
		catch (Exception e)
		{
			EventLogger.LogMessage("LocalSettings", EventLogger.LogLevel.Error, $"Loading settings failed:\n\t{e}");
			return false;
		}

		EventLogger.LogMessage("LocalSettings", EventLogger.LogLevel.Info, "Loading settings succeeded");
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

			settingsFileAccess.StoreString(JsonSerializer.Serialize(packedSettings, serializerOptions));
		}
		catch (Exception e)
		{
			EventLogger.LogMessage("LocalSettings", EventLogger.LogLevel.Error, $"Saving settings failed with:\n\t{e}");
			return false;
		}

		EventLogger.LogMessage("LocalSettings", EventLogger.LogLevel.Info, "Saving settings succeeded");
		return true;
	}

	public void ForceDefaultSettings()
	{
		EventLogger.LogMessage("LocalSettings", EventLogger.LogLevel.Info, "Loading default settings");
		Camera = new();
		Mqtt = new();
		Joystick = new();
		General = new();
	}

	private void EmitSignalWholeSectionChanged(string sectionName)
	{
		EmitSignal(SignalName.WholeSectionChanged, sectionName);
		EventLogger.LogMessageDebug("LocalSettings", EventLogger.LogLevel.Verbose, $"Section \"{sectionName}\" was overwritten");
	}


	[SettingsManagerVisible(customName: "Camera Settings")]
	public Settings.Camera Camera
	{
		get => _camera;
		set
		{
			_camera = value;
			EmitSignalWholeSectionChanged(nameof(Camera));
		}
	}

	[SettingsManagerVisible(customName: "MQTT Settings")]
	public Settings.Mqtt Mqtt
	{
		get => _mqtt;
		set
		{
			_mqtt = value;
			EmitSignalWholeSectionChanged(nameof(Mqtt));
		}
	}

	[SettingsManagerVisible(customName: "Joystick Settings")]
	public Settings.Joystick Joystick
	{
		get => _joystick;
		set
		{
			_joystick = value;
			EmitSignalWholeSectionChanged(nameof(Joystick));
		}
	}

	[SettingsManagerVisible(customName: "General Settings")]
	public Settings.General General
	{
		get => _general;
		set
		{
			_general = value;
			EmitSignalWholeSectionChanged(nameof(General));
		}
	}

	Settings.Camera _camera;
	Settings.Mqtt _mqtt;
	Settings.Joystick _joystick;
	Settings.General _general;
}


