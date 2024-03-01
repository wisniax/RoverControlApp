using Godot;
using Godot.Collections;
using Newtonsoft.Json;

namespace RoverControlApp.Core.Settings;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public partial class Camera : GodotObject
{
    [Signal]
    public delegate void SettingChangedEventHandler(StringName name, Variant value);

	public Camera()
    {
		_ip = "192.168.1.35";
		_rtspStreamPath = "/live/0/MAIN";
		_rtspPort = 554;
		_ptzPort = 80;
		_login = "admin";
		_password = "admin";
		_inverseAxis = false;
		_enableRtspStream = true;
		_enablePtzControl = true;
		_ptzRequestFrequency = 2.69;
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"(?i)(?:[0-9a-f]{1,4}:){7}[0-9a-f]{1,4}|(?:\d{1,3}\.){3}\d{1,3}|(?:http:\/\/|https:\/\/)\S+")]
	public string Ip { get => _ip;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.Ip, value);
			_ip = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string RtspStreamPath { get => _rtspStreamPath;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.RtspStreamPath, value);
			_rtspStreamPath = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int RtspPort { get => _rtspPort;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.RtspPort, value);
			_rtspPort = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int PtzPort { get => _ptzPort;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.PtzPort, value);
			_ptzPort = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string Login { get => _login;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.Login, value);
			_login = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string Password { get => _password;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.Password, value);
			_password = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool InverseAxis { get => _inverseAxis;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.InverseAxis, value);
			_inverseAxis = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool EnableRtspStream { get => _enableRtspStream;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.EnableRtspStream, value);
			_enableRtspStream = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool EnablePtzControl { get => _enablePtzControl;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.EnablePtzControl, value);
			_enablePtzControl = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "1;4;0.01;f;d")]
	public double PtzRequestFrequency { get => _ptzRequestFrequency;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.PtzRequestFrequency, value);
			_ptzRequestFrequency = value;
		}
	}

	string _ip;
	string _rtspStreamPath;
	int _rtspPort;
	int _ptzPort;
	string _login;
	string _password;
	bool _inverseAxis;
	bool _enableRtspStream;
	bool _enablePtzControl;
	double _ptzRequestFrequency;
}


