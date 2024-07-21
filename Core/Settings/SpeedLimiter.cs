using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(SpeedLimiterConverter))]
public partial class SpeedLimiter : SettingBase, ICloneable
{

	public SpeedLimiter()
	{
		_enabled = false;
		_maxSpeed = 0.5f;
	}

	public SpeedLimiter(bool enabled, float maxSpeed)
	{
		_enabled = enabled;
		_maxSpeed = maxSpeed;
	}

	public object Clone()
	{
		return new SpeedLimiter()
		{
			Enabled = _enabled,
			MaxSpeed = _maxSpeed
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool Enabled
	{
		get => _enabled;
		set => EmitSignal_SettingChanged(ref _enabled, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range,  formatData: "0.1;0.95;0.05;f;f", customName: "MaxSpeed (multiplier)")]
	public float MaxSpeed
	{
		get => _maxSpeed;
		set => EmitSignal_SettingChanged(ref _maxSpeed, value);
	}
	
	bool _enabled;
	float _maxSpeed;
}
