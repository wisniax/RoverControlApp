using Godot;
using RoverControlApp.Core.JSONConverters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(WheelDataConverter))]
public partial class WheelData : RefCounted
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
	}

	public WheelData(string frontLeftDrive, string frontRightDrive, string backRightDrive, string backLeftDrive, string frontLeftTurn, string frontRightTurn, string backRightTurn, string backLeftTurn)
	{
		FrontLeftTurn = frontLeftTurn;
		FrontRightTurn = frontRightTurn;
		BackRightTurn = backRightTurn;
		BackLeftTurn = backLeftTurn;

		FrontLeftDrive = frontLeftDrive;
		FrontRightDrive = frontRightDrive;
		BackRightDrive = backRightDrive;
		BackLeftDrive = backLeftDrive;
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string FrontLeftDrive { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string FrontRightDrive { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BackRightDrive { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BackLeftDrive { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string FrontLeftTurn { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string FrontRightTurn { get; init; }
	
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BackRightTurn { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BackLeftTurn { get; init; }
}