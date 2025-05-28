using System;
using System.Text.Json.Serialization;

using Godot;

using RoverControlApp.Core.JSONConverters;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(GeneralConverter))]
public partial class General : SettingBase, ICloneable
{

	public General()
	{
		_verboseDebug = false;
		_missionControlPosition = "20;30";
		_missionControlSize = "480;360";
		_backCaptureLength = 15000;
		_noInputSecondsToEstop = 120;
		_pedanticEstop = true;
	}

	public General(bool verboseDebug, string missionControlPosition, string missionControlSize, long backCaptureLength, int noInputSecondsToEstop, bool pedanticEstop)
	{
		_verboseDebug = verboseDebug;
		_missionControlPosition = missionControlPosition;
		_missionControlSize = missionControlSize;
		_backCaptureLength = backCaptureLength;
		_noInputSecondsToEstop = noInputSecondsToEstop;
		_pedanticEstop = pedanticEstop;
	}

	public object Clone()
	{
		return new General()
		{
			VerboseDebug = _verboseDebug,
			MissionControlPosition = _missionControlPosition,
			MissionControlSize = _missionControlSize,
			BackCaptureLength = _backCaptureLength,
			NoInputSecondsToEstop = _noInputSecondsToEstop,
			PedanticEstop = _pedanticEstop
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool VerboseDebug
	{
		get => _verboseDebug;
		set => EmitSignal_SettingChanged(ref _verboseDebug, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"-?[0-9]+;-?[0-9]+")]
	public string MissionControlPosition
	{
		get => _missionControlPosition;
		set => EmitSignal_SettingChanged(ref _missionControlPosition, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"-?[0-9]+;-?[0-9]+")]
	public string MissionControlSize
	{
		get => _missionControlSize;
		set => EmitSignal_SettingChanged(ref _missionControlSize, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;60000;100;f;l", customTooltip: "How long is history [ms]")]
	public long BackCaptureLength
	{
		get => _backCaptureLength;
		set => EmitSignal_SettingChanged(ref _backCaptureLength, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;1800;1;f;i", customTooltip: "How many seconds have to pass for auto switch to EStop. (0 to disable)")]
	public int NoInputSecondsToEstop
	{
		get => _noInputSecondsToEstop;
		set => EmitSignal_SettingChanged(ref _noInputSecondsToEstop, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check, customTooltip: "No input will be accepted when in EStop (beside mode change)")]
	public bool PedanticEstop
	{
		get => _pedanticEstop;
		set => EmitSignal_SettingChanged(ref _pedanticEstop, value);
	}

	public ulong NoInputMsecToEstop => (ulong)_noInputSecondsToEstop * 1000;


	bool _verboseDebug;
	string _missionControlPosition;
	string _missionControlSize;
	long _backCaptureLength;
	int _noInputSecondsToEstop;
	bool _pedanticEstop;
}