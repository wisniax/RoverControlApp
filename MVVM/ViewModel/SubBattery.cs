using Godot;
using System;
using RoverControlApp.Core;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

public partial class SubBattery : VBoxContainer
{
	[Export] private Label _slotLabel;
	[Export] private Label _slotEmptyLabel;
	[Export] private Label _idLabel;
	[Export] private Label _percLabel;
	[Export] private Label _vbatLabel;
	[Export] private Label _hotswapLabel;
	[Export] private Label _statusLabel;
	[Export] private Label _currentLabel;
	[Export] private Label _temperatureLabel;
	[Export] private Label _timeLabel;

	[Export] private Button _autoButton;
	[Export] private Button _onButton;
	[Export] private Button _offButton;

	private MqttClasses.BatteryInfo myData;

	private int _slot;

	public SubBattery(int slot)
	{
		_slot = slot;
		_slotLabel.SetText($"Battery slot: {_slot}");
	}

	public override void _Ready()
	{
	}

	public void UpdateBattInfo(MqttClasses.BatteryInfo data)
	{
		myData = data;

		_idLabel.Text = "Battery ID: " + data.ID;
		_percLabel.Text = "Battery %: " + data.ChargePercent.ToString("F1") + "%";

		_vbatLabel.Text = "VBat: " + data.Voltage.ToString("F1") + "V";

		if (data.Voltage < 6 * LocalSettings.Singleton.Battery.CriticalVoltage)
			_vbatLabel.SetModulate(Colors.Red);
		else if (data.Voltage < 6 * LocalSettings.Singleton.Battery.WarningVoltage)
			_vbatLabel.SetModulate(Colors.Yellow);
		else
			_vbatLabel.SetModulate(Colors.White);

		_hotswapLabel.Text = "Hotswap: " + data.HotswapStatus.ToString();
		_statusLabel.Text = "Status: " + data.Status.ToString();
		_currentLabel.Text = "Current: " + data.Current.ToString("F1") + "A";
		_temperatureLabel.Text = "Temperature: " + data.Temperature.ToString("F1") + "C";
		if (data.Temperature > LocalSettings.Singleton.Battery.WarningTemperature)
			_temperatureLabel.SetModulate(Colors.Orange);
		else
			_temperatureLabel.SetModulate(Colors.White);

		_timeLabel.Text = "Est. Time: " + data.Time.ToString("F0") + "min";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
