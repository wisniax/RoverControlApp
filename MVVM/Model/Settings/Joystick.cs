using RoverControlApp.Core;
using Godot;
using Newtonsoft.Json;
using System;

namespace RoverControlApp.MVVM.Model.Settings;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public partial class Joystick : GodotObject, ICloneable
{
	[Signal]
	public delegate void SettingChangedEventHandler(StringName name, Variant oldValue, Variant newValue);

	public Joystick()
	{
        _newFancyRoverController = false;
        _deadzone = 0.15f;
        _vibrateOnModeChange = true;
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

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool NewFancyRoverController
	{
		get => _newFancyRoverController;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.NewFancyRoverController, _newFancyRoverController, value);
			_newFancyRoverController = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0f;1;0.01;f;f")]
	public float Deadzone
	{
		get => _deadzone;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.Deadzone, _deadzone, value);
			_deadzone = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool VibrateOnModeChange
	{
		get => _vibrateOnModeChange;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.VibrateOnModeChange, _vibrateOnModeChange, value);
			_vibrateOnModeChange = value;
		}
	}

	bool _newFancyRoverController;
	float _deadzone;
	bool _vibrateOnModeChange;
}



