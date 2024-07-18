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
	}

	public Joystick(bool newFancyRoverController, float deadzone, bool vibrateOnModeChange)
	{
		_newFancyRoverController = newFancyRoverController;
		_deadzone = deadzone;
		_vibrateOnModeChange = vibrateOnModeChange;
	}

	public object Clone()
	{
		return new Joystick()
		{
			NewFancyRoverController = _newFancyRoverController,
			Deadzone = _deadzone,
			VibrateOnModeChange = _vibrateOnModeChange
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

	bool _newFancyRoverController;
	float _deadzone;
	bool _vibrateOnModeChange;
}



