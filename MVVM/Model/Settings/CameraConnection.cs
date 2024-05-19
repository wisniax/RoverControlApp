using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core.JSONConverters;
using System.Text.Json.Serialization;

namespace RoverControlApp.MVVM.Model.Settings;

[JsonConverter(typeof(CameraConnectionConverter))]
public partial class CameraConnection : RefCounted
{
	public CameraConnection()
	{
		Ip = "192.168.1.35";
		Login = "admin";
		Password = "admin";
		RtspStreamPath = "/live/0/MAIN";
		RtspPort = 554;
		PtzPort = 80;
	}

	public CameraConnection(string ip, string login, string password, string streamPath, int rtspPort, int ptzPort)
	{
		Ip = ip;
		Login = login;
		Password = password;
		RtspStreamPath = streamPath;
		RtspPort = rtspPort;
		PtzPort = ptzPort;
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"(?i)(?:[0-9a-f]{1,4}:){7}[0-9a-f]{1,4}|(?:\d{1,3}\.){3}\d{1,3}|(?:http:\/\/|https:\/\/)\S+")]
	public string Ip { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string Login { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string Password { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string RtspStreamPath { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int RtspPort { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int PtzPort { get; init; }
}



