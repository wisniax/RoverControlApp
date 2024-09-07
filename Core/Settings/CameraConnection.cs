using Godot;
using RoverControlApp.Core.JSONConverters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(CameraConnectionConverter))]
public partial class CameraConnection : RefCounted
{

	public CameraConnection()
	{
		Ip = "192.168.1.35";
		Login = "admin";
		Password = "admin";
		RtspLink = "http://pendelcam.kip.uni-heidelberg.de/mjpg/video.mjpg";
		RtspPort = 554;
		PtzPort = 80;
	}

	public CameraConnection(string ip, string login, string password, string rtspLink, int rtspPort, int ptzPort)
	{
		Ip = ip;
		Login = login;
		Password = password;
		RtspLink = rtspLink;
		RtspPort = rtspPort;
		PtzPort = ptzPort;
	}

	public override string ToString()
	{
		return JsonSerializer.Serialize(this);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"(?i)(?:[0-9a-f]{1,4}:){7}[0-9a-f]{1,4}|(?:\d{1,3}\.){3}\d{1,3}|(?:http:\/\/|https:\/\/)\S+")]
	public string Ip { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string Login { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string Password { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string RtspLink { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int RtspPort { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int PtzPort { get; init; }
}



