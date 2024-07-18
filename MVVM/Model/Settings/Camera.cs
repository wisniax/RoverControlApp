using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.MVVM.Model.Settings;

[JsonConverter(typeof(CameraConverter))]
public partial class Camera : SettingBase, ICloneable
{

	public Camera()
    {
		_connectionSettings = new();

		_inverseAxis = false;
		_enableRtspStream = true;
		_enablePtzControl = true;
		_ptzRequestFrequency = 2.69;
	}

	public Camera(CameraConnection connectionSettings, bool inverseAxis, bool enableRtspStream, bool enablePtzControl, double ptzRequestFrequency)
	{
		_connectionSettings = connectionSettings;

		_inverseAxis = inverseAxis;
		_enableRtspStream = enableRtspStream;
		_enablePtzControl = enablePtzControl;
		_ptzRequestFrequency = ptzRequestFrequency;
	}

	public object Clone()
	{
		return new Camera()
		{
			ConnectionSettings = _connectionSettings,

			InverseAxis  = _inverseAxis,
			EnableRtspStream  = _enableRtspStream,
			EnablePtzControl  = _enablePtzControl,
			PtzRequestFrequency  = _ptzRequestFrequency
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public CameraConnection ConnectionSettings
	{
		get => _connectionSettings;
		set => EmitSignal_SectionChanged(ref _connectionSettings, value);
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

	CameraConnection _connectionSettings;
	bool _inverseAxis;
	bool _enableRtspStream;
	bool _enablePtzControl;
	double _ptzRequestFrequency;
}


