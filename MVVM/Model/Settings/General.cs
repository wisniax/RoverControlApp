using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.MVVM.Model.Settings;

[JsonConverter(typeof(GeneralConverter))]
public partial class General : SettingBase, ICloneable
{
	
	public General()
	{
        _verboseDebug = false;
        _missionControlPosition = "20;30";
        _missionControlSize = "480;360";
        _backCaptureLength = 15000;
	}

	public General(bool verboseDebug, string missionControlPosition, string missionControlSize, long backCaptureLength)
	{
		_verboseDebug = verboseDebug;
		_missionControlPosition = missionControlPosition;
		_missionControlSize = missionControlSize;
		_backCaptureLength = backCaptureLength;
	}

	public object Clone()
	{
		return new General()
		{
			VerboseDebug = _verboseDebug,
			MissionControlPosition = _missionControlPosition,
			MissionControlSize = _missionControlSize,
			BackCaptureLength = _backCaptureLength,
		};
	}

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

