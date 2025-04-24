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
		_expectedMessageInterval = 10;
		_averageAll = false;
		_altMode = false;
	}

	public Battery(float warningVoltage, float criticalVoltage, float warningTemperature, int expectedMessageInterval, bool averageAll, bool altMode)
	{
		_warningVoltage = warningVoltage;
		_criticalVoltage = criticalVoltage;
		_warningTemperature = warningTemperature;
		_expectedMessageInterval = expectedMessageInterval;
		_averageAll = averageAll;
		_altMode = altMode;
	}

	public object Clone()
	{
		return new Battery()
		{
			WarningVoltage = _warningVoltage,
			CriticalVoltage = _criticalVoltage,
			WarningTemperature = _warningTemperature,
			ExpectedMessageInterval = _expectedMessageInterval,
			AverageAll = _averageAll,
			AltMode = _altMode
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
	public int ExpectedMessageInterval
	{
		get => _expectedMessageInterval;
		set => EmitSignal_SettingChanged(ref _expectedMessageInterval, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "30;120;5;f;f")]
	public float WarningTemperature
	{
		get => _warningTemperature;
		set => EmitSignal_SettingChanged(ref _warningTemperature, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool AverageAll
	{
		get => _averageAll;
		set => EmitSignal_SettingChanged(ref _averageAll, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool AltMode
	{
		get => _altMode;
		set => EmitSignal_SettingChanged(ref _altMode, value);
	}

	float _warningVoltage;
	float _criticalVoltage;
	float _warningTemperature;
	int _expectedMessageInterval;
	bool _averageAll;
	bool _altMode;
}


