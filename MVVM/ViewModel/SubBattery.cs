using System;
using System.Text.Json;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;

public partial class SubBattery : VBoxContainer
{
	[Export] private Label _slotLabel = null!;
	[Export] private Label _slotEmptyLabel = null!;
	[Export] private Label _idLabel = null!;
	[Export] private Label _percLabel = null!;
	[Export] private Label _vbatLabel = null!;
	[Export] private Label _hotswapLabel = null!;
	[Export] private Label _statusLabel = null!;
	[Export] private Label _currentLabel = null!;
	[Export] private Label _temperatureLabel = null!;
	[Export] private Label _timeLabel = null!;

	[Export] private Button _autoButton = null!;
	[Export] private Button _onButton = null!;
	[Export] private Button _offButton = null!;

	[Export] private Timer _timer = null!;
	[Export] private VBoxContainer _labels = null!;

	[Export]
	private BatteryMonitor _batteryMonitor = null!;

	public volatile MqttClasses.BatteryInfo myData = new();

	private volatile int _slot;

	public event Func<Task>? NewBatteryInfo;
	public event Func<int, MqttClasses.BatterySet, Task>? OnBatteryControl; //slot, command

	public volatile bool UpToDate = false;

	public void SetSlotNumber(int slot)
	{
		_slot = slot;
		_slotLabel.SetText($"Battery slot: {slot}");
	}

	public override void _Ready()
	{
		if (_batteryMonitor is null)
		{
			GD.PushError("_batteryMonitor is not set!");
		}
		batteryDetectedHandler(UpToDate);
	}

	public Task UpdateBattInfoHandler(string msg)
	{
		CallDeferred("UpdateBattInfo", msg);
		return Task.CompletedTask;
	}

	public void UpdateBattInfo(string msg)
	{
		var data = JsonSerializer.Deserialize<MqttClasses.BatteryInfo>(msg);

		myData = data ?? new();

		_idLabel.Text = "Battery ID: " + myData.ID;
		_percLabel.Text = "Battery %: " + myData.ChargePercent.ToString("F1") + "%";

		_vbatLabel.Text = "VBat: " + myData.Voltage.ToString("F1") + "V";

		if (myData.Voltage < 6 * LocalSettings.Singleton.Battery.CriticalVoltage)
			_vbatLabel.SetModulate(Colors.Red);
		else if (myData.Voltage < 6 * LocalSettings.Singleton.Battery.WarningVoltage)
			_vbatLabel.SetModulate(Colors.Yellow);
		else
			_vbatLabel.SetModulate(Colors.White);

		_hotswapLabel.Text = "Hotswap: " + myData.HotswapStatus.ToString();
		_statusLabel.Text = "Status: " + myData.Status.ToString();
		_currentLabel.Text = "Current: " + myData.Current.ToString("F1") + "A";
		_temperatureLabel.Text = "Temperature: " + myData.Temperature.ToString("F1") + "C";
		if (myData.Temperature > LocalSettings.Singleton.Battery.WarningTemperature)
			_temperatureLabel.SetModulate(Colors.Orange);
		else
			_temperatureLabel.SetModulate(Colors.White);

		_timeLabel.Text = "Est. Time: " + myData.Time.ToString("F0") + "min";

		batteryDetectedHandler(true);
		_timer.SetWaitTime(LocalSettings.Singleton.Battery.ExpectedMessageInterval);
		_timer.Start();
	}

	void BatteryControl(MqttClasses.BatterySet set)
	{
		if(set == MqttClasses.BatterySet.Off)
		{
			if (_batteryMonitor.ConnectedBatts < 2) return;
		}

		OnBatteryControl?.Invoke(_slot, set);
	}

	void batteryDetectedHandler(bool detected)
	{
		UpToDate = detected;
		_slotEmptyLabel.SetVisible(!detected);
		_labels.SetVisible(detected);                //buttons stay visible so that we can force close the hotswap even if bms died or we use a non-bms battery
		NewBatteryInfo?.Invoke();
	}
}
