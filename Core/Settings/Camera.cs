using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(CameraConverter))]
public partial class Camera : SettingBase, ICloneable
{

	public Camera()
	{
		_streamPathHD = "http://pendelcam.kip.uni-heidelberg.de/mjpg/video.mjpg";
		_streamPathSD = "http://158.58.130.148/mjpg/video.mjpg";
		_inverseAxis = false;
		_enableRtspStream = true;
		_enablePtzControl = true;
		_ptzRequestFrequency = 2.69;
	}

	public Camera(string streamPathHD, string streamPathSD, bool inverseAxis, bool enableRtspStream, bool enablePtzControl, double ptzRequestFrequency, bool HdEnabled)
	{
		_streamPathHD = streamPathHD;
		_streamPathSD = streamPathSD;
		_inverseAxis = inverseAxis;
		_enableRtspStream = enableRtspStream;
		_enablePtzControl = enablePtzControl;
		_ptzRequestFrequency = ptzRequestFrequency;
	}

	public object Clone()
	{
		return new Camera()
		{
			StreamPathHD = _streamPathHD,
			StreamPathSD = _streamPathSD,
			InverseAxis  = _inverseAxis,
			EnableRtspStream  = _enableRtspStream,
			EnablePtzControl  = _enablePtzControl,
			PtzRequestFrequency  = _ptzRequestFrequency
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string StreamPathHD
	{
		get => _streamPathHD;
		set => EmitSignal_SettingChanged(ref _streamPathHD,value);
	}
	
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string StreamPathSD
	{
		get => _streamPathSD;
		set => EmitSignal_SettingChanged(ref _streamPathSD,value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool InverseAxis
	{
		get => _inverseAxis;
		set => EmitSignal_SettingChanged(ref _inverseAxis,value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool EnableRtspStream
	{
		get => _enableRtspStream;
		set => EmitSignal_SettingChanged(ref _enableRtspStream, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool EnablePtzControl
	{
		get => _enablePtzControl;
		set => EmitSignal_SettingChanged(ref _enablePtzControl, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "1;4;0.01;f;d")]
	public double PtzRequestFrequency
	{
		get => _ptzRequestFrequency;
		set => EmitSignal_SettingChanged(ref _ptzRequestFrequency, value);
	}

	public bool HdEnabled
	{
		get => _hdEnabled;
		set => EmitSignal_SettingChanged(ref _hdEnabled, value);
	}

	string _streamPathHD;
	string _streamPathSD;
	bool _inverseAxis;
	bool _enableRtspStream;
	bool _enablePtzControl;
	double _ptzRequestFrequency;
	bool _hdEnabled;
}


