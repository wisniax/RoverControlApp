using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(DriveMotorCanIdsConverter))]
public partial class DriveMotorCanIds : SettingBase, ICloneable
{
	public DriveMotorCanIds()
	{
		_frontLeft = "0x50";
		_frontRight = "0x51";
		_rearRight = "0x52";
		_rearLeft = "0x53";
		_expectedFeedbackInterval = 500;
	}

	public DriveMotorCanIds(string frontLeft, string frontRight, string rearRight, string rearLeft, int expectedFeedbackInterval)
	{
		_frontLeft = frontLeft;
		_frontRight = frontRight;
		_rearRight = rearRight;
		_rearLeft = rearLeft;
		_expectedFeedbackInterval = expectedFeedbackInterval;
	}

	public object Clone()
	{
		return new DriveMotorCanIds(_frontLeft, _frontRight, _rearRight, _rearLeft, _expectedFeedbackInterval);
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
