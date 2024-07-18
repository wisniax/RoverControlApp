using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.MVVM.Model.Settings;

[JsonConverter(typeof(JoystickConverter))]
public partial class Joystick : SettingBase, ICloneable
{

	public Joystick()
	{
		_newFancyRoverController = false;
		_deadzone = 0.15f;
		_vibrateOnModeChange = true;
		_enabled = false;
		_maxSpeed = .5f;
	}

	public Joystick(bool newFancyRoverController, float deadzone, bool vibrateOnModeChange, bool enabled, float maxSpeed)
	{
		_newFancyRoverController = newFancyRoverController;
		_deadzone = deadzone;
		_vibrateOnModeChange = vibrateOnModeChange;
		_enabled = enabled;
		_maxSpeed = maxSpeed;
	}

	public object Clone()
	{
		return new Joystick()
		{
			NewFancyRoverController = _newFancyRoverController,
			Deadzone = _deadzone,
			VibrateOnModeChange = _vibrateOnModeChange,
			Enabled = _enabled,
			MaxSpeed = _maxSpeed
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool NewFancyRoverController
	{
		get => _newFancyRoverController;
		set => EmitSignal_SettingChanged(ref _newFancyRoverController, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;1;0.01;f;f")]
	public float Deadzone
	{
		get => _deadzone;
		set => EmitSignal_SettingChanged(ref _deadzone, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool VibrateOnModeChange
	{
		get => _vibrateOnModeChange;
		set => EmitSignal_SettingChanged(ref _vibrateOnModeChange, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool Enabled
	{
		get => _enabled;
		set => EmitSignal_SettingChanged(ref _enabled, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0.2;1;0.05;f;f", customName: "MaxSpeed (multiplier)")]
	public float MaxSpeed
	{
		get => _maxSpeed;
		set => EmitSignal_SettingChanged(ref _maxSpeed, value);
	}

	bool _newFancyRoverController;
	float _deadzone;
	bool _vibrateOnModeChange;
	bool _enabled;
	float _maxSpeed;
}



