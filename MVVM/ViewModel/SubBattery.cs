using Godot;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using RoverControlApp.MVVM.ViewModel;

public partial class SubBattery : VBoxContainer
{
	[Export] private Label _slotLabel = new ();
	[Export] private Label _slotEmptyLabel = new ();
	[Export] private Label _idLabel = new();
	[Export] private Label _percLabel = new();
	[Export] private Label _vbatLabel = new();
	[Export] private Label _hotswapLabel = new();
	[Export] private Label _statusLabel = new();
	[Export] private Label _currentLabel = new();
	[Export] private Label _temperatureLabel = new();
	[Export] private Label _delayLabel = new();

	[Export] private Button _autoButton = new();
	[Export] private Button _onButton = new();
	[Export] private Button _offButton = new();

	[Export] private Timer _timer;
	[Export] private VBoxContainer _labels;

	private BatteryMonitor _batteryMonitor;

	public volatile MqttClasses.BatteryInfo myData = new();

	private volatile int _slot;

	public event Func<Task>? NewBatteryInfo;
	public event Func<int, MqttClasses.BatterySet, Task>? OnBatteryControl; //slot, command

	public volatile bool UpToDate = false;
	public volatile bool IsHotswapClosed = false;

	private long LastTimestamp = 0;

	public void SetSlotNumber(int slot)
	{
		_slot = slot;
		_slotLabel.SetText($"Battery slot: {slot}");
	}

	public override void _Ready()
	{
		_batteryMonitor = GetParent<HBoxContainer>().GetParent<BatteryMonitor>();
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

		myData = data;

		_idLabel.Text = "Battery ID: " + data.ID;
		_percLabel.Text = "Battery %: " + data.ChargePercent.ToString("F1") + "%";

		_vbatLabel.Text = "VBat: " + data.Voltage.ToString("F1") + "V";

		if (data.Voltage < LocalSettings.Singleton.Battery.CriticalVoltage)
			_vbatLabel.SetModulate(Colors.Red);
		else if (data.Voltage < LocalSettings.Singleton.Battery.WarningVoltage)
			_vbatLabel.SetModulate(Colors.Yellow);
		else
			_vbatLabel.SetModulate(Colors.White);

		_statusLabel.Text = "Status: " +
			((LocalSettings.Singleton.Battery.BatteryStatusByBMS) ?
				data.Status.ToString() :
				((data.Current < 0) ? "Discarging" : "Charging"));
		_currentLabel.Text = "Current: " + ((data.Current > 0) ? "+" : "") + (data.Current * 4).ToString("F1") + "A";
		_temperatureLabel.Text = "Temperature: " + data.Temperature.ToString("F1") + "C";
		if (data.Temperature > LocalSettings.Singleton.Battery.WarningTemperature)
			_temperatureLabel.SetModulate(Colors.Orange);
		else
			_temperatureLabel.SetModulate(Colors.White);


		_delayLabel.Text = "Delay: " + (data.Timestamp - LastTimestamp) + " ms";
		LastTimestamp = data.Timestamp;

		NewBatteryInfo.Invoke();
		
		
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
		_batteryMonitor.SendToHUD();
		if (!detected)
		{
			_batteryMonitor.ClearQuickDataHandler();
		}
	}

	public void ShowHotswapNoDataHandler()
	{
		IsHotswapClosed = false;
		CallDeferred("ShowHotswapNoData");
	}

	public void ShowHotswapNoData()
	{
		_hotswapLabel.Text = "Hotswap: NoData";
	}

	public void ShowHotswapStatusHandler(bool? closed)
	{
		if (closed == null)
		{
			ShowHotswapNoDataHandler();
			return;
		}
		IsHotswapClosed = (bool)closed;
		CallDeferred("ShowHotswapStatus", (bool)closed);
	}

	public void ShowHotswapStatus(bool closed)
	{
		_hotswapLabel.Text = (closed ? "Hotswap: Closed" : "Hotswap: Opened");
	}
}
