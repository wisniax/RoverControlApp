using Godot;
using RoverControlApp.Core;
using System;

public partial class ThreeDBattery : Node3D
{
	[Export] Label3D _batteryLabel = null!;
	[Export] CsgBox3D _batteryFilled = null!;
	[Export] CsgBox3D _batteryBody = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void UpdateBatteryLevel(MqttClasses.BatteryInfo batteryInfo)
	{
		CallDeferred(nameof(UpdateBatteryLabel), batteryInfo.ChargePercent, batteryInfo.Voltage, batteryInfo.Current);

		// Color the battery based on the charge percentage
		CallDeferred(nameof(UpdateBatteryColor), batteryInfo.ChargePercent);

		// Scale the battery fill based on the charge percentage
		CallDeferred(nameof(UpdateBatteryFill), batteryInfo.ChargePercent);
	}

	void UpdateBatteryLabel(float chargePercent, float voltage, float current)
	{
		_batteryLabel.Text = $"{voltage:F1} V\n{chargePercent:F1}%\n{current:F1} A";
	}

	void UpdateBatteryColor(float chargePercent)
	{
		// 0XFF0000 -> 0xFFFF00 -> 0x00FF00
		int red = (chargePercent > 50) ? ((int)(100 - chargePercent) * 255 / 50) : 255;
		int green = (chargePercent > 50) ? 255 : ((int)chargePercent * 255 / 50);

		_batteryFilled.MaterialOverride = new StandardMaterial3D
		{
			AlbedoColor = new Color(red / 255f, green / 255f, 0)
		};
	}

	void UpdateBatteryFill(float chargePercent)
	{
		_batteryFilled.Size = _batteryFilled.Size with { Y = chargePercent / 100f * _batteryBody.Size.Y };
		_batteryFilled.Position = _batteryFilled.Position with { Y = _batteryBody.Position.Y - (_batteryBody.Size.Y - _batteryFilled.Size.Y) / 2f };
	}
}
