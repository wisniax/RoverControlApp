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
		_roverManipulatorController = 3;
		_holdToChangeManipulatorAxes = false;
	}

	public Manipulator(int roverManipulatorController, bool holdToChangeManipulatorAxes)
	{
		_roverManipulatorController = roverManipulatorController;
		_holdToChangeManipulatorAxes = holdToChangeManipulatorAxes;
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
					   "3 - MultiMode (Default)"
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

	int _roverManipulatorController;
	bool _holdToChangeManipulatorAxes;
}



