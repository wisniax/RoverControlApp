using Godot;

namespace RoverControlApp.Core;

public class CameraSettings
{

	public static readonly CameraSettings DEFAULT = new()
	{
		Ip = "192.168.1.35",
		RtspStreamPath = "/live/0/MAIN",
		RtspPort = 554,
		PtzPort = 80,
		Login = "admin",
		Password = "admin",
		InverseAxis = false,
		EnableRtspStream = true,
		EnablePtzControl = true,
		PtzRequestFrequency = 2.69
	};

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"(?i)(?:[0-9a-f]{1,4}:){7}[0-9a-f]{1,4}|(?:\d{1,3}\.){3}\d{1,3}|(?:http:\/\/|https:\/\/)\S+")]
	public string Ip { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string RtspStreamPath { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int RtspPort { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int PtzPort { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string Login { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string Password { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool InverseAxis { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool EnableRtspStream { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool EnablePtzControl { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "1;4;0.01;f;d")]
	public double PtzRequestFrequency { get; set; }
}


