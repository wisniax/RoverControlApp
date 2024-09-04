using Godot;
using RoverControlApp.Core.JSONConverters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(CameraConnectionConverter))]
public partial class CameraConnection : RefCounted
{

	public CameraConnection(int id)
	{
		switch (id)
		{
			case 0:
				Ip = "192.168.1.35";
				Login = "admin";
				Password = "admin";
				RtspStreamPathHD = "admin:admin@192.168.1.35:554/live/0/MAIN";
				RtspStreamPathSD = "admin:admin@192.168.1.35:554/live/0/MAIN";
				break;
			case 1:
				Ip = "192.168.1.35";
				Login = "admin";
				Password = "admin";
				RtspStreamPathHD = "admin:Faptors69@192.168.1.36:554/1/1";
				RtspStreamPathSD = "admin:Faptors69@192.168.1.36:554/1/2";
				break;
			case 2:
				Ip = "192.168.1.35";
				Login = "admin";
				Password = "admin";
				RtspStreamPathHD = "192.168.1.37:8554/cam0";
				RtspStreamPathSD = "192.168.1.37:8554/cam0";
				break;
			case 3:
				Ip = "192.168.1.35";
				Login = "admin";
				Password = "admin";
				RtspStreamPathHD = "192.168.1.37:8554/cam1";
				RtspStreamPathSD = "192.168.1.37:8554/cam1";
				break;
			case 4:
				Ip = "192.168.1.35";
				Login = "admin";
				Password = "admin";
				RtspStreamPathHD = "192.168.1.38:8554/cam1";
				RtspStreamPathSD = "192.168.1.38:8554/cam1";
				break;
			case 5:
				Ip = "192.168.1.35";
				Login = "admin";
				Password = "admin";
				RtspStreamPathHD = "192.168.1.38:8554/cam0";
				RtspStreamPathSD = "192.168.1.38:8554/cam0";
				break;
			default:
				break;

		}
		
		RtspPort = 554;
		PtzPort = 80;
	}

	public CameraConnection(string ip, string login, string password, string streamPathHD, string streamPathSD, int rtspPort, int ptzPort)
	{
		Ip = ip;
		Login = login;
		Password = password;
		RtspStreamPathHD = streamPathHD;
		RtspStreamPathSD = streamPathSD;
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
	public string RtspStreamPathHD { get; init; }
	
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string RtspStreamPathSD { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int RtspPort { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int PtzPort { get; init; }
}



