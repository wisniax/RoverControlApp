using System;
using System.Text.Json.Serialization;

using Godot;

using RoverControlApp.Core.JSONConverters;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(CalibrationMotorsConverter))]
public partial class CalibrationMotor : SettingBase, ICloneable
{

	public CalibrationMotor()
	{
		_minSpeed = 2000f;
		_maxSpeed = 10000f;
	}

	public CalibrationMotor(float minSpeed, float maxSpeed)
	{
		_minSpeed = minSpeed;
		_maxSpeed = maxSpeed;
	}

	public object Clone()
	{
		return new CalibrationMotor()
		{
			MinSpeed = _minSpeed,
			MaxSpeed = _maxSpeed,
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "2000;10000;100;f;f")]
	public float MinSpeed
	{
		get => _minSpeed;
		set => EmitSignal_SettingChanged(ref _minSpeed, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "8000;20000;100;f;f")]
	public float MaxSpeed
	{
		get => _maxSpeed;
		set => EmitSignal_SettingChanged(ref _maxSpeed, value);
	}

	float _minSpeed;
	float _maxSpeed;
}
