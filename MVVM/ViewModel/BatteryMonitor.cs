using System;
using System.Text.Json;
using System.Threading.Tasks;

using Godot;

using MQTTnet;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

public partial class BatteryMonitor : PanelContainer
{
	[Export] volatile SubBattery[] battery = new SubBattery[3];
	[Export] private volatile VBoxContainer altDataDisp = new();

	private volatile float _currentVoltageAlt = 0;

	public event Action<int, int, Color>? OnBatteryDataChanged; //enabled batteries (closed hotswaps) (0 if it's in alt mode), percentages (volts from alt mode), color to check for warnings

	public int ConnectedBatts
	{
		get => CountConnectedBatts();
	}

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += BatteryInfoChanged;
		MqttNode.Singleton.MessageReceivedAsync += AltBatteryInfoChanged;
		LocalSettings.Singleton.PropagatedPropertyChanged += OnSettingsPropertyChanged;
		foreach (var batt in battery)
		{
			batt.NewBatteryInfo += SendToHUD;
			batt.OnBatteryControl += OnBatteryControl;
		}
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= BatteryInfoChanged;
		MqttNode.Singleton.MessageReceivedAsync -= AltBatteryInfoChanged;
		LocalSettings.Singleton.PropagatedPropertyChanged -= OnSettingsPropertyChanged;
		foreach (var batt in battery)
		{
			batt.NewBatteryInfo -= SendToHUD;
			batt.OnBatteryControl -= OnBatteryControl;
		}
	}

	public override void _Ready()
	{
		for (int i = 0; i < 3; i++)
		{
			battery[i].SetSlotNumber(i + 1);
		}
	}

	public async Task BatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicBatteryInfo) || subTopic != LocalSettings.Singleton.Mqtt.TopicBatteryInfo)
			return;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return;
		}

		MqttClasses.BatteryInfo? data =
			JsonSerializer.Deserialize<MqttClasses.BatteryInfo>(msg.ConvertPayloadToString());

		if (data is null)
			return;

		await battery[data.Slot - 1].UpdateBattInfoHandler(msg.ConvertPayloadToString());

		return;
	}

	public Task AltBatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (!LocalSettings.Singleton.Battery.AltMode && CountConnectedBatts() != 0)
		{
			CallDeferred("ShowAltVoltage", false);
			return Task.CompletedTask;
		}
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicWheelFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicWheelFeedback)
			return Task.CompletedTask;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("AltBatteryMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}

		var altData = JsonSerializer.Deserialize<MqttClasses.WheelFeedback>(msg.ConvertPayloadToString());

		if (altData is null) return Task.CompletedTask;

		bool altDataValidId =
			altData.VescId == 0x50 ||
			altData.VescId == 0x51 ||
			altData.VescId == 0x52 ||
			altData.VescId == 0x53;

		if (!altDataValidId) return Task.CompletedTask;

		_currentVoltageAlt = _currentVoltageAlt * 0.9f + 0.1f * (float)altData.VoltsIn;
		CallDeferred("ShowAltVoltage", true);

		OnBatteryDataChanged?.Invoke(0, (int)(_currentVoltageAlt * 10), CheckForWarnings());

		return Task.CompletedTask;
	}

	void ShowAltVoltage(bool show)
	{
		altDataDisp.SetVisible(show);
		altDataDisp.GetChild(1).Set("text", "VBat: " + _currentVoltageAlt.ToString("F1") + "V");
	}

	Task SendToHUD()
	{
		OnBatteryDataChanged?.Invoke(CountConnectedBatts(), CalculateBatteryPercentSum(), CheckForWarnings());

		return Task.CompletedTask;
	}

	Color CheckForWarnings()
	{
		float battVoltage = 0;
		if (LocalSettings.Singleton.Battery.AltMode || CountConnectedBatts() == 0)
		{
			battVoltage = _currentVoltageAlt;
		}
		else
		{
			battVoltage = 999;
			foreach (var batt in battery)
			{
				if (batt.myData == null) continue;
				if ((batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto || batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan) &&
				   battVoltage > batt.myData.Voltage)
				{
					battVoltage = batt.myData.Voltage;
				}

				if (batt.myData.Temperature > LocalSettings.Singleton.Battery.WarningTemperature) return Colors.Red;
			}
		}

		if (battVoltage < 6 * LocalSettings.Singleton.Battery.CriticalVoltage) return Colors.Red;
		if (battVoltage < 6 * LocalSettings.Singleton.Battery.WarningVoltage) return Colors.Yellow;


		return Colors.White;
	}

	Task OnBatteryControl(int slot, MqttClasses.BatterySet set)
	{
		if (CountConnectedBatts() < 2 && set == MqttClasses.BatterySet.Off) return Task.CompletedTask;

		MqttClasses.BatteryControl control = new MqttClasses.BatteryControl()
		{
			Slot = slot,
			Set = set
		};
		_ = OnBatteryControlChanged(control);
		return Task.CompletedTask;
	}

	private async Task OnBatteryControlChanged(MqttClasses.BatteryControl arg)
	{
		await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicBatteryControl,
			JsonSerializer.Serialize(arg));
	}

	int CountConnectedBatts()
	{
		int count = 0;

		foreach (var batt in battery)
		{
			if (batt.myData == null) continue;
			if ((batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
				batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan) && batt.UpToDate)
				count++;
		}

		return count;
	}

	int CalculateBatteryPercentSum()
	{
		int sum = 0;

		foreach (var batt in battery)
		{
			if (batt.myData == null) continue;
			if ((batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnAuto ||
				batt.myData.HotswapStatus == MqttClasses.HotswapStatus.OnMan) && batt.UpToDate)
				sum += (int)batt.myData.ChargePercent;
		}

		return sum;
	}

	private void OnSettingsPropertyChanged(StringName category, StringName property, Variant oldValue, Variant newValue)
	{
		switch (category)
		{
			case nameof(LocalSettings.Battery):
				SendToHUD();
				break;
		}
	}
}
