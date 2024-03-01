using Godot;
using Godot.Collections;
using Newtonsoft.Json;

namespace RoverControlApp.Core.Settings;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public partial class Joystick : GodotObject
{
	[Signal]
	public delegate void SettingChangedEventHandler(StringName name, Variant value);

	public Joystick()
	{
        _newFancyRoverController = false;
        _deadzone = 0.15f;
        _vibrateOnModeChange = true;
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool NewFancyRoverController { get => _newFancyRoverController;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.NewFancyRoverController, value);
			_newFancyRoverController = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0f;1;0.01;f;f")]
	public float Deadzone { get => _deadzone;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.Deadzone, value);
			_deadzone = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool VibrateOnModeChange { get => _vibrateOnModeChange;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.VibrateOnModeChange, value);
			_vibrateOnModeChange = value;
		}
	}

	bool _newFancyRoverController;
	float _deadzone;
	bool _vibrateOnModeChange;
}



