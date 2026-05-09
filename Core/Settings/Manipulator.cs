using System;
using System.Text.Json.Serialization;

using Godot;

using RoverControlApp.Core.JSONConverters;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(ManipulatorConverter))]
public partial class Manipulator : SettingBase, ICloneable
{

	public Manipulator()
	{
		_roverManipulatorController = 0;
		_holdToChangeManipulatorAxes = false;
		_useLegacyFrames = true;
		_invKinScaler = new();
	}

	public Manipulator(int roverManipulatorController, bool holdToChangeManipulatorAxes, bool useLegacyFrames, InvKinScaler invKinScaler)
	{
		_roverManipulatorController = roverManipulatorController;
		_holdToChangeManipulatorAxes = holdToChangeManipulatorAxes;
		_useLegacyFrames = useLegacyFrames;
		_invKinScaler = invKinScaler;
	}

	public object Clone()
	{
		return new Manipulator()
		{
			RoverManipulatorController = _roverManipulatorController,
			HoldToChangeManipulatorAxes = _holdToChangeManipulatorAxes
		};
	}


	[SettingsManagerVisible(
		cellMode: TreeItem.TreeCellMode.Range,
		formatData: "0;3;1;f;i",
		customTooltip: "0 - MultiAxis\n" +
					   "1 - SingleAxis\n" +
					   "2 - InvKinJoystick\n" +
					   "3 - MultiMode (Default)\n" +
					   "Inverse kinematics modes require legacy frames to be disabled."
	)]
	public int RoverManipulatorController
	{
		get => _roverManipulatorController;
		set => EmitSignal_SettingChanged(ref _roverManipulatorController, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check, customTooltip: "When checked you have to hold the button to change axes.")]
	public bool HoldToChangeManipulatorAxes
	{
		get => _holdToChangeManipulatorAxes;
		set => EmitSignal_SettingChanged(ref _holdToChangeManipulatorAxes, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check, customTooltip: "When enabled, the manipulator will use legacy frames.")]
	public bool UseLegacyFrames
	{
		get => _useLegacyFrames;
		set => EmitSignal_SettingChanged(ref _useLegacyFrames, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public Settings.InvKinScaler? InvKinScaler
	{
		get => _invKinScaler;
		set => EmitSignal_SectionChanged(ref _invKinScaler, value);
	}


	int _roverManipulatorController;
	bool _holdToChangeManipulatorAxes;
	bool _useLegacyFrames;
	Settings.InvKinScaler? _invKinScaler;
}



