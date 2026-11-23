using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(ImuBoardCanIdsConverter))]
public partial class ImuBoardCanIds : SettingBase, ICloneable
{
	public ImuBoardCanIds()
	{
		_frontLeft = "0xB0";
		_frontRight = "0xB1";
		_rearRight = "0xB2";
		_rearLeft = "0xB3";
		_body = "0xB4";
		_expectedFeedbackInterval = 500;
	}

	public ImuBoardCanIds(string frontLeft, string frontRight, string rearRight, string rearLeft, string body, int expectedFeedbackInterval)
	{
		_frontLeft = frontLeft;
		_frontRight = frontRight;
		_rearRight = rearRight;
		_rearLeft = rearLeft;
		_body = body;
		_expectedFeedbackInterval = expectedFeedbackInterval;
	}

	public object Clone()
	{
		return new ImuBoardCanIds(_frontLeft, _frontRight, _rearRight, _rearLeft, _body, _expectedFeedbackInterval);
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

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.OptionalIdPattern,
		customTooltip: "Body IMU CAN ID. Leave empty when unconfigured.")]
	public string Body
	{
		get => _body;
		set => EmitSignal_SettingChanged(ref _body, value);
	}

	private string _frontLeft;
	private string _frontRight;
	private string _rearRight;
	private string _rearLeft;
	private string _body;
	private int _expectedFeedbackInterval;
}
