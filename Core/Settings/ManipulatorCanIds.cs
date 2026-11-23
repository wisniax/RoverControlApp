using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(ManipulatorCanIdsConverter))]
public partial class ManipulatorCanIds : SettingBase, ICloneable
{
	public ManipulatorCanIds()
	{
		_axis1 = "0x70";
		_axis2 = "0x71";
		_axis3 = "0x72";
		_axis4 = "0x73";
		_axis5 = "0x74";
		_axis6 = "0x75";
		_gripper = "0x76";
		_expectedFeedbackInterval = 500;
	}

	public ManipulatorCanIds(string axis1, string axis2, string axis3, string axis4, string axis5, string axis6, string gripper, int expectedFeedbackInterval)
	{
		_axis1 = axis1;
		_axis2 = axis2;
		_axis3 = axis3;
		_axis4 = axis4;
		_axis5 = axis5;
		_axis6 = axis6;
		_gripper = gripper;
		_expectedFeedbackInterval = expectedFeedbackInterval;
	}

	public object Clone()
	{
		return new ManipulatorCanIds(_axis1, _axis2, _axis3, _axis4, _axis5, _axis6, _gripper, _expectedFeedbackInterval);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "10;10000;10;f;i",
		customTooltip: "Expected interval between feedback frames from this group, in milliseconds.")]
	public int ExpectedFeedbackInterval
	{
		get => _expectedFeedbackInterval;
		set => EmitSignal_SettingChanged(ref _expectedFeedbackInterval, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string Axis1
	{
		get => _axis1;
		set => EmitSignal_SettingChanged(ref _axis1, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string Axis2
	{
		get => _axis2;
		set => EmitSignal_SettingChanged(ref _axis2, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string Axis3
	{
		get => _axis3;
		set => EmitSignal_SettingChanged(ref _axis3, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string Axis4
	{
		get => _axis4;
		set => EmitSignal_SettingChanged(ref _axis4, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string Axis5
	{
		get => _axis5;
		set => EmitSignal_SettingChanged(ref _axis5, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string Axis6
	{
		get => _axis6;
		set => EmitSignal_SettingChanged(ref _axis6, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string Gripper
	{
		get => _gripper;
		set => EmitSignal_SettingChanged(ref _gripper, value);
	}

	private string _axis1;
	private string _axis2;
	private string _axis3;
	private string _axis4;
	private string _axis5;
	private string _axis6;
	private string _gripper;
	private int _expectedFeedbackInterval;
}
