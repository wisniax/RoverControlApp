using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(BatteryConverter))]
public partial class Battery : SettingBase, ICloneable
{

	public Battery()
	{
		_warningVoltage = 3.6f;
		_criticalVoltage = 3.2f;
		_warningTemperature = 70f;
	}

	public Battery(float warningVoltage, float criticalVoltage, float warningTemperature)
	{
		_warningVoltage = warningVoltage;
		_criticalVoltage = criticalVoltage;
		_warningTemperature = warningTemperature;
	}

	public object Clone()
	{
		return new Battery()
		{
			WarningVoltage = _warningVoltage,
			CriticalVoltage = _criticalVoltage,
			WarningTemperature = _warningTemperature
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "3.2;4.3;0.1;f;f")]
	public float WarningVoltage
	{
		get => _warningVoltage;
		set => EmitSignal_SettingChanged(ref _warningVoltage, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "3.2;4.3;0.1;f;f")]
	public float CriticalVoltage
	{
		get => _criticalVoltage;
		set => EmitSignal_SettingChanged(ref _criticalVoltage, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "30;120;5;f;f")]
	public float WarningTemperature
	{
		get => _warningTemperature;
		set => EmitSignal_SettingChanged(ref _warningTemperature, value);
	}


	float _warningVoltage;
	float _criticalVoltage;
	float _warningTemperature;
}


