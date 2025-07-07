using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(WheelDataConverter))]
public partial class WheelData : SettingBase, ICloneable
{

	public WheelData()
	{
		FrontLeftDrive = "0x50";
		FrontRightDrive = "0x51";
		BackRightDrive = "0x52";
		BackLeftDrive = "0x53";

		FrontLeftTurn = "0x60";
		FrontRightTurn = "0x61";
		BackRightTurn = "0x62";
		BackLeftTurn = "0x63";

		MaxRPM = 1000;
	}

	public WheelData(string frontLeftDrive, string frontRightDrive, string backRightDrive, string backLeftDrive, string frontLeftTurn, string frontRightTurn, string backRightTurn, string backLeftTurn, int maxRPM)
	{
		FrontLeftTurn = frontLeftTurn;
		FrontRightTurn = frontRightTurn;
		BackRightTurn = backRightTurn;
		BackLeftTurn = backLeftTurn;

		FrontLeftDrive = frontLeftDrive;
		FrontRightDrive = frontRightDrive;
		BackRightDrive = backRightDrive;
		BackLeftDrive = backLeftDrive;

		MaxRPM = maxRPM;
	}

	public object Clone()
	{
		return new WheelData()
		{
			FrontLeftDrive = _frontLeftDrive,
			FrontRightDrive = _frontRightDrive,
			BackRightDrive = _backRightDrive,
			BackLeftDrive = _backLeftDrive,

			FrontLeftTurn = _frontLeftTurn,
			FrontRightTurn = _frontRightTurn,
			BackRightTurn = _backRightTurn,
			BackLeftTurn = _backLeftTurn
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string FrontLeftDrive
	{
		get => _frontLeftDrive;
		set => EmitSignal_SettingChanged(ref _frontLeftDrive, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string FrontRightDrive
	{
		get => _frontRightDrive;
		set => EmitSignal_SettingChanged(ref _frontRightDrive, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BackRightDrive
	{
		get => _backRightDrive;
		set => EmitSignal_SettingChanged(ref _backRightDrive, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BackLeftDrive
	{
		get => _backLeftDrive;
		set => EmitSignal_SettingChanged(ref _backLeftDrive, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string FrontLeftTurn
	{
		get => _frontLeftTurn;
		set => EmitSignal_SettingChanged(ref _frontLeftTurn, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string FrontRightTurn
	{
		get => _frontRightTurn;
		set => EmitSignal_SettingChanged(ref _frontRightTurn, value);
	}
	
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BackRightTurn
	{
		get => _backRightTurn;
		set => EmitSignal_SettingChanged(ref _backRightTurn, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BackLeftTurn
	{
		get => _backLeftTurn;
		set => EmitSignal_SettingChanged(ref _backLeftTurn, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "100;10000;100;f;i")]
	public int MaxRPM
	{
		get => _maxRPM;
		set => EmitSignal_SettingChanged(ref _maxRPM, value);
	}

	private string _frontLeftDrive;
	private string _frontRightDrive;
	private string _backRightDrive;
	private string _backLeftDrive;
	private string _frontLeftTurn;
	private string _frontRightTurn;
	private string _backRightTurn;
	private string _backLeftTurn;
	private int _maxRPM;
}