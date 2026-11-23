using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(TurnMotorCanIdsConverter))]
public partial class TurnMotorCanIds : SettingBase, ICloneable
{
	public TurnMotorCanIds()
	{
		_frontLeft = "0x60";
		_frontRight = "0x61";
		_rearRight = "0x62";
		_rearLeft = "0x63";
		_expectedFeedbackInterval = 500;
	}

	public TurnMotorCanIds(string frontLeft, string frontRight, string rearRight, string rearLeft, int expectedFeedbackInterval)
	{
		_frontLeft = frontLeft;
		_frontRight = frontRight;
		_rearRight = rearRight;
		_rearLeft = rearLeft;
		_expectedFeedbackInterval = expectedFeedbackInterval;
	}

	public object Clone()
	{
		return new TurnMotorCanIds(_frontLeft, _frontRight, _rearRight, _rearLeft, _expectedFeedbackInterval);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "10;10000;10;f;i",
		customTooltip: "Expected interval between feedback frames from this group, in milliseconds.")]
	public int ExpectedFeedbackInterval
	{
		get => _expectedFeedbackInterval;
		set => EmitSignal_SettingChanged(ref _expectedFeedbackInterval, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string FrontLeft
	{
		get => _frontLeft;
		set => EmitSignal_SettingChanged(ref _frontLeft, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string FrontRight
	{
		get => _frontRight;
		set => EmitSignal_SettingChanged(ref _frontRight, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string RearRight
	{
		get => _rearRight;
		set => EmitSignal_SettingChanged(ref _rearRight, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string RearLeft
	{
		get => _rearLeft;
		set => EmitSignal_SettingChanged(ref _rearLeft, value);
	}

	private string _frontLeft;
	private string _frontRight;
	private string _rearRight;
	private string _rearLeft;
	private int _expectedFeedbackInterval;
}
