using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(WheelDataConverter))]
public partial class WheelData : SettingBase, ICloneable
{
	public WheelData()
	{
		_maxRPM = 1000;
	}

	public WheelData(int maxRPM)
	{
		_maxRPM = maxRPM;
	}

	public object Clone()
	{
		return new WheelData(
			MaxRPM = _maxRPM
			);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "100;10000;100;f;i")]
	public int MaxRPM
	{
		get => _maxRPM;
		set => EmitSignal_SettingChanged(ref _maxRPM, value);
	}

	private int _maxRPM;
}
