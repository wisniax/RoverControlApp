using RoverControlApp.Core;
using Godot;
using Newtonsoft.Json;
using System;

namespace RoverControlApp.MVVM.Model.Settings;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public partial class General : GodotObject, ICloneable
{
 	[Signal]
    public delegate void SettingChangedEventHandler(StringName name, Variant oldValue, Variant newValue);

	public General()
	{
        _verboseDebug = false;
        _missionControlPosition = "20;30";
        _missionControlSize = "480;360";
        _backCaptureLength = 15000;
	}

	public object Clone()
	{
		return new General()
		{
			VerboseDebug = _verboseDebug,
			MissionControlPosition = _missionControlPosition,
			MissionControlSize = _missionControlSize,
			BackCaptureLength= _backCaptureLength
		};
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool VerboseDebug
	{
		get => _verboseDebug;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.VerboseDebug, _verboseDebug, value);
			_verboseDebug = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"-?[0-9]+;-?[0-9]+")]
	public string MissionControlPosition
	{
		get => _missionControlPosition;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.MissionControlPosition, _missionControlPosition, value);
			_missionControlPosition = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"-?[0-9]+;-?[0-9]+")]
	public string MissionControlSize
	{
		get => _missionControlSize;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.MissionControlSize, _missionControlSize, value);
			_missionControlSize = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;60000;100;f;l", customTooltip: "How long is history [ms]")]
	public long BackCaptureLength
	{
		get => _backCaptureLength;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.BackCaptureLength, _backCaptureLength, value);
			_backCaptureLength = value;
		}
	}

	
	bool _verboseDebug;
	string _missionControlPosition;
	string _missionControlSize;
	long _backCaptureLength;
}

