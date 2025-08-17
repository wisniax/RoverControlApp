﻿using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Godot;

namespace RoverControlApp.Core;

/// <summary>
/// Master class for settings storage. Can be fetched by LocalSettings.Singleton<br/>
/// </summary>
public partial class LocalSettings : Node
{
	private sealed class PackedSettings
	{
		public Settings.Camera? Camera { get; set; } = null;
		public Settings.Mqtt? Mqtt { get; set; } = null;
		public Settings.Joystick? Joystick { get; set; } = null;
		public Settings.SpeedLimiter? SpeedLimiter { get; set; } = null;
		public Settings.General? General { get; set; } = null;
		public Settings.Sampler? Sampler { get; set; } = null;
		public Settings.Battery? Battery { get; set; } = null;
	}

	private JsonSerializerOptions serializerOptions = new() { WriteIndented = true };

	private static readonly string _settingsPath = "user://RoverControlAppSettings.json";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public static LocalSettings Singleton { get; private set; }
#pragma warning restore CS8618

	/// <summary>
	/// Signal stating that one of categories was overwritten (reference changed)
	/// </summary>
	[Signal]
	public delegate void CategoryChangedEventHandler(StringName category);

	/// <summary>
	/// Signal SubcategoryChanged propagated from one of categories. Prefer this over SettingBase.SubcategoryChanged
	/// </summary>
	[Signal]
	public delegate void PropagatedSubcategoryChangedEventHandler(StringName category, StringName subcategory, Variant oldValue, Variant newValue);

	/// <summary>
	/// Signal PropertyChanged propagated from one of categories. Prefer this over SettingBase.PropertyChanged
	/// </summary>
	[Signal]
	public delegate void PropagatedPropertyChangedEventHandler(StringName category, StringName property, Variant oldValue, Variant newValue);

	public LocalSettings()
	{
		_camera = new();
		_mqtt = new();
		_joystick = new();
		_speedLimiter = new();
		_general = new();
		_sampler = new();
		_battery = new();

		if (LoadSettings()) return;

		ForceDefaultSettings();
		SaveSettings();
	}

	public override void _Ready()
	{
		//first ever call to _Ready will be on singletone instance.
		Singleton ??= this;
	}

	private static Settings.Camera LoadSettings_CameraSafety(Settings.Camera camera)
	{
		if (OS.GetName() != "Windows")
		{
			if (camera.EnableRtspStream)
			{
				EventLogger.LogMessage(
					nameof(LocalSettings),
					EventLogger.LogLevel.Warning,
					"Detected non Windows OS platform! RTSP will be loaded as disabled, enable it manually.");

				camera.EnableRtspStream = false;
			}
		}

		return camera;
	}

	/// <summary>
	/// Load settings form file
	/// </summary>
	/// <exception cref="FieldAccessException"/>
	/// <exception cref="JsonException"/>
	/// <exception cref="DataException"/>
	/// <returns>true on success</returns>
	public bool LoadSettings()
	{
		try
		{
			using var settingsFileAccess = FileAccess.Open(_settingsPath, FileAccess.ModeFlags.Read) ?? throw new FieldAccessException(FileAccess.GetOpenError().ToString());

			var serializedSettings = settingsFileAccess.GetAsText(true);

			var packedSettings = JsonSerializer.Deserialize<PackedSettings>(serializedSettings, serializerOptions) ?? throw new DataException("unknown reason");

			Camera = LoadSettings_CameraSafety(packedSettings.Camera ?? new());
			Mqtt = packedSettings.Mqtt ?? new();
			Joystick = packedSettings.Joystick ?? new();
			SpeedLimiter = packedSettings.SpeedLimiter ?? new();
			General = packedSettings.General ?? new();
			Sampler = packedSettings.Sampler ?? new();
			Battery = packedSettings.Battery ?? new();


		}
		catch (Exception e)
		{
			EventLogger.LogMessage("LocalSettings", EventLogger.LogLevel.Error, $"Loading settings failed:\n\t{e}");
			return false;
		}

		EventLogger.LogMessage("LocalSettings", EventLogger.LogLevel.Info, "Loading settings succeeded");
		return true;
	}

	/// <summary>
	/// Save settings to file
	/// </summary>
	///	<exception cref="FieldAccessException"/>
	///	<exception cref="JsonException"/>
	/// <returns>true on success</returns>
	public bool SaveSettings()
	{
		try
		{
			using var settingsFileAccess = FileAccess.Open(_settingsPath, FileAccess.ModeFlags.Write) ?? throw new FieldAccessException(FileAccess.GetOpenError().ToString());

			PackedSettings packedSettings = new()
			{
				Camera = Camera,
				Mqtt = Mqtt,
				Joystick = Joystick,
				SpeedLimiter = SpeedLimiter,
				General = General,
				Sampler = Sampler,
				Battery = Battery
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

	/// <summary>
	/// Reset settings to default state
	/// </summary>
	public void ForceDefaultSettings()
	{
		EventLogger.LogMessage("LocalSettings", EventLogger.LogLevel.Info, "Loading default settings");

		Camera = LoadSettings_CameraSafety(new());
		Mqtt = new();
		Joystick = new();
		SpeedLimiter = new();
		General = new();
		Sampler = new();
		Battery = new();
	}

	private void EmitSignalCategoryChanged(string sectionName)
	{
		EmitSignal(SignalName.CategoryChanged, sectionName);
		EventLogger.LogMessageDebug("LocalSettings", EventLogger.LogLevel.Verbose, $"Section \"{sectionName}\" was overwritten");
	}

	private void PropagateSignal(StringName signal, StringName category, params Variant[] args)
	{
		Variant[] combined = new Variant[args.Length + 1];

		combined[0] = category;
		args.CopyTo(combined, 1);

		EventLogger.LogMessageDebug("LocalSettings", EventLogger.LogLevel.Verbose, $"Field \"{args[0].AsStringName()}\" from \"{combined[0]}\" was changed. Signal propagated to LocalSettings.");

		EmitSignal(signal, combined);
	}

	/// <summary>
	/// Fany pants lambda creator for propagator
	/// </summary>
	/// <param name="signal">SignalName.PropagatedPropertyChanged or SignalName.PropagatedSubcategoryChanged</param>
	/// <param name="category">name of category (when used in Property setter should be empty)</param>
	/// <returns></returns>
	private Action<StringName, Variant, Variant> CreatePropagator(StringName signal, [CallerMemberName] string category = "")
	{
		return (field, oldVal, newVal) => PropagateSignal(signal, category, field, oldVal, newVal);
	}


	[SettingsManagerVisible(customName: "Camera Settings")]
	public Settings.Camera Camera
	{
		get => _camera;
		set
		{
			_camera = value;

			_camera.Connect(
				Settings.Camera.SignalName.SubcategoryChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedSubcategoryChanged))
			);
			_camera.Connect(
				Settings.Camera.SignalName.PropertyChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedPropertyChanged))
			);

			EmitSignalCategoryChanged(nameof(Camera));
		}
	}

	[SettingsManagerVisible(customName: "MQTT Settings")]
	public Settings.Mqtt Mqtt
	{
		get => _mqtt;
		set
		{
			_mqtt = value;

			_mqtt.Connect(
				Settings.Mqtt.SignalName.SubcategoryChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedSubcategoryChanged))
			);
			_mqtt.Connect(
				Settings.Mqtt.SignalName.PropertyChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedPropertyChanged))
			);

			EmitSignalCategoryChanged(nameof(Mqtt));
		}
	}

	[SettingsManagerVisible(customName: "Joystick Settings")]
	public Settings.Joystick Joystick
	{
		get => _joystick;
		set
		{
			_joystick = value;

			_joystick.Connect(
				Settings.Joystick.SignalName.SubcategoryChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedSubcategoryChanged))
			);
			_joystick.Connect(
				Settings.Joystick.SignalName.PropertyChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedPropertyChanged))
			);

			EmitSignalCategoryChanged(nameof(Joystick));
		}
	}

	[SettingsManagerVisible(customName: "SpeedLimiter Settings")]
	public Settings.SpeedLimiter SpeedLimiter
	{
		get => _speedLimiter;
		set
		{
			_speedLimiter = value;

			_speedLimiter.Connect(
				Settings.SpeedLimiter.SignalName.SubcategoryChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedSubcategoryChanged))
			);
			_speedLimiter.Connect(
				Settings.SpeedLimiter.SignalName.PropertyChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedPropertyChanged))
			);

			EmitSignalCategoryChanged(nameof(SpeedLimiter));
		}
	}

	[SettingsManagerVisible(customName: "General Settings")]
	public Settings.General General
	{
		get => _general;
		set
		{
			_general = value;

			_general.Connect(
				Settings.General.SignalName.SubcategoryChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedSubcategoryChanged))
			);
			_general.Connect(
				Settings.General.SignalName.PropertyChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedPropertyChanged))
			);

			EmitSignalCategoryChanged(nameof(General));
		}
	}

	[SettingsManagerVisible(customName: "Sampler Settings")]
	public Settings.Sampler Sampler
	{
		get => _sampler;
		set
		{
			_sampler = value;

			_sampler.Connect(
				Settings.Sampler.SignalName.SubcategoryChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedSubcategoryChanged))
			);
			_sampler.Connect(
				Settings.Sampler.SignalName.PropertyChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedPropertyChanged))
			);

			EmitSignalCategoryChanged(nameof(Sampler));
		}
	}

	[SettingsManagerVisible(customName: "Battery Settings")]
	public Settings.Battery Battery
	{
		get => _battery;
		set
		{
			_battery = value;

			_battery.Connect(
				Settings.Battery.SignalName.SubcategoryChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedSubcategoryChanged))
			);
			_battery.Connect(
				Settings.Battery.SignalName.PropertyChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedPropertyChanged))
			);

			EmitSignalCategoryChanged(nameof(Battery));
		}
	}

	Settings.Camera _camera;
	Settings.Mqtt _mqtt;
	Settings.Joystick _joystick;
	Settings.SpeedLimiter _speedLimiter;
	Settings.General _general;
	Settings.Sampler _sampler;
	Settings.Battery _battery;
}


