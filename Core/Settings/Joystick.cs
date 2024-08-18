using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;
using RoverControlApp.Core.RoverControllerPresets;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(JoystickConverter))]
public partial class Joystick : SettingBase, ICloneable
{

	public Joystick()
	{
		_roverDriveController = 3;
		_deadzone = 0.15f;
		_vibrateOnModeChange = true;
	}

	public Joystick(int roverDriveController, float deadzone, bool vibrateOnModeChange)
	{
		_roverDriveController = roverDriveController;
		_deadzone = deadzone;
		_vibrateOnModeChange = vibrateOnModeChange;
	}

	public object Clone()
	{
		return new Joystick()
		{
			RoverDriveController = _roverDriveController,
			Deadzone = _deadzone,
			VibrateOnModeChange = _vibrateOnModeChange
		};
	}

	[SettingsManagerVisible(
		cellMode: TreeItem.TreeCellMode.Range,
		formatData:"0;3;1;f;i",
		customTooltip:	"0 - GoodOldGamesLikeController\n" +
						"1 - EricSOnController\n" +
						"2 - ForzaLikeController\n" +
						"3 - DirectDriveController (Default)"
	)]
	public int RoverDriveController
	{
		get => _roverDriveController;
		set => EmitSignal_SettingChanged(ref _roverDriveController, value);
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

	int _roverDriveController;
	float _deadzone;
	bool _vibrateOnModeChange;
}



