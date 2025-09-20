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
		_warningVoltage = 21.6f;
		_criticalVoltage = 20f;
		_warningTemperature = 70f;
		_expectedMessageInterval = 5;
		_batteryStatusByBMS = false;

		_averageAll = true;
		_altMode = false;
		_showOnLow = false;
	}

	public Battery(float warningVoltage, float criticalVoltage, float warningTemperature, int expectedMessageInterval, bool batteryStatusByBMS, bool averageAll, bool altMode, bool showOnLow)
	{
		_warningVoltage = warningVoltage;
		_criticalVoltage = criticalVoltage;
		_warningTemperature = warningTemperature;
		_expectedMessageInterval = expectedMessageInterval;
		_batteryStatusByBMS = batteryStatusByBMS;
		_averageAll = averageAll;
		_altMode = altMode;
		_showOnLow = showOnLow;
	}

	public object Clone()
	{
		return new Battery()
		{
			WarningVoltage = _warningVoltage,
			CriticalVoltage = _criticalVoltage,
			WarningTemperature = _warningTemperature,
			ExpectedMessageInterval = _expectedMessageInterval,
			BatteryStatusByBMS = _batteryStatusByBMS,
			AverageAll = _averageAll,
			AltMode = _altMode,
			ShowOnLow = _showOnLow
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "19;25.2;0.1;f;f",
		 customTooltip: "Warning voltage (below that point battery label goes yellow)")]
	public float WarningVoltage
	{
		get => _warningVoltage;
		set => EmitSignal_SettingChanged(ref _warningVoltage, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "19;25.2;0.1;f;f",
		 customTooltip: "Warning voltage (below that point battery label goes red)")]
	public float CriticalVoltage
	{
		get => _criticalVoltage;
		set => EmitSignal_SettingChanged(ref _criticalVoltage, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "1;120;5;f;f",
		 customTooltip: "Time interval the app would wait, before assuming that BMS communication died,\n"+
						"and pulling voltage data from alt source (vesc)")]
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

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check,
		 customTooltip: "When enabled pulls battery status (full, charging, empty etc.) from BMS.\n" +
						"When disabled generates battery status based on battery current sign (+/-).")]
	public bool BatteryStatusByBMS
	{
		get => _batteryStatusByBMS;
		set => EmitSignal_SettingChanged(ref _batteryStatusByBMS, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check,
		 customTooltip: "(REQUIRES Alt Mode OFF)\n" +
						"When enabled shows average battery percentage and amount of (active) batteries.\n" +
						"When disabled shows a sum of battery percentages and number of (active) batteries")]
	public bool AverageAll
	{
		get => _averageAll;
		set => EmitSignal_SettingChanged(ref _averageAll, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check,
		 customTooltip: "When enabled always shows vesc voltage in HUD.\n" +
						"When disabled shows battery charge percent (if available)")]
	public bool AltMode
	{
		get => _altMode;
		set => EmitSignal_SettingChanged(ref _altMode, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check,
		 customTooltip: "When enabled every time a message, that warrants a warning arrives (overtemperature, undervoltage), the battery monitor shows up.\n" +
						"When disabled it doesn't do that")]
	public bool ShowOnLow
	{
		get => _showOnLow;
		set => EmitSignal_SettingChanged(ref _showOnLow, value);
	}

	float _warningVoltage;
	float _criticalVoltage;
	float _warningTemperature;
	int _expectedMessageInterval;
	bool _batteryStatusByBMS;
	bool _averageAll;
	bool _altMode;
	bool _showOnLow;
}


