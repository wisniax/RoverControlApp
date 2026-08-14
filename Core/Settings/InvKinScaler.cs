using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;
using RoverControlApp.Core.RoverControllerPresets;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(InvKinScalerConverter))]
public partial class InvKinScaler : SettingBase, ICloneable
{
	public InvKinScaler()
	{
		_maxLinearSpeed = 100f;
		_maxAngularSpeed = 0.67f;
	}

	public InvKinScaler(float maxLinearSpeed, float maxAngularSpeed)
	{
		_maxLinearSpeed = maxLinearSpeed;
		_maxAngularSpeed = maxAngularSpeed;
	}

	public object Clone()
	{
		return new InvKinScaler()
		{
			MaxAngularSpeed = _maxAngularSpeed,
			MaxLinearSpeed = _maxLinearSpeed
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;150;5;f;f", customTooltip: "Max speed linear in inverseKinematicsSpeed mode [cm/s]")]
	public float MaxLinearSpeed
	{
		get => _maxLinearSpeed;
		set => EmitSignal_SettingChanged(ref _maxLinearSpeed, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;1;0.01;f;f", customTooltip: "Max speed angular in inverseKinematicsSpeed mode [rad/s]")]
	public float MaxAngularSpeed
	{
		get => _maxAngularSpeed;
		set => EmitSignal_SettingChanged(ref _maxAngularSpeed, value);
	}

	float _maxLinearSpeed;
	float _maxAngularSpeed;
}
