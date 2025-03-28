using Godot;
using System;
using RoverControlApp.Core;
using MQTTnet;
using System.Threading.Tasks;
using System.Text.Json;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

public partial class BatteryMonitor : Panel
{
	[Export] VBoxContainer batt1 = null!;
	[Export] VBoxContainer batt2 = null!;
	[Export] VBoxContainer batt3 = null!;
	[Export] VBoxContainer altDisp = null!;

	private MqttClasses.BatteryInfo data;

	private MqttClasses.BatteryInfo[] battery = new MqttClasses.BatteryInfo[3];
	private float _currentVoltageAlt = 0;

	public event Func<int, Color, Task>? OnBatteryPercentageChanged;

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += BatteryInfoChanged;
		MqttNode.Singleton.MessageReceivedAsync += AltBatteryInfoChanged;
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= BatteryInfoChanged;
		MqttNode.Singleton.MessageReceivedAsync -= AltBatteryInfoChanged;
	}

	public Task BatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicBatteryInfo) || subTopic != LocalSettings.Singleton.Mqtt.TopicBatteryInfo)
			return Task.CompletedTask;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}

		data = JsonSerializer.Deserialize<MqttClasses.BatteryInfo>(msg.ConvertPayloadToString());

		switch (data.Slot)
		{
			case 1:
				CallDeferred("UpdateLabels", batt1);
				battery[0] = data;
				break;
			case 2:
				CallDeferred("UpdateLabels", batt2);
				battery[1] = data;
				break;
			case 3:
				CallDeferred("UpdateLabels", batt3);
				battery[2] = data;
				break;
			default:
				EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Invalid battery slot");
				return Task.CompletedTask;
		}

		OnBatteryPercentageChanged.Invoke(CalculateAverageBatteryPercent(), CheckForWarnings());
		
		return Task.CompletedTask;
	}
	public Task AltBatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (!LocalSettings.Singleton.Battery.AltMode)
			return Task.CompletedTask;
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicWheelFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicWheelFeedback)
			return Task.CompletedTask;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("AltBatteryMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}
		
		var altData = JsonSerializer.Deserialize<MqttClasses.WheelFeedback>(msg.ConvertPayloadToString());
		
		if (!(altData.VescId == 0x50 || altData.VescId == 0x51 || altData.VescId == 0x52 || altData.VescId == 0x53)) return Task.CompletedTask;

		_currentVoltageAlt = _currentVoltageAlt * 0.9f + 0.1f * (float)altData.VoltsIn;
		GD.Print($"{altData.VescId}:{altData.VoltsIn}");
		CallDeferred("ShowAltVoltage");

		OnBatteryPercentageChanged.Invoke((int)(_currentVoltageAlt*10),CheckForWarningsAlt());

		return Task.CompletedTask;
	}

	void ShowAltVoltage()
	{
		altDisp.GetNode<Label>("Voltage").SetText($"{_currentVoltageAlt:F2} V");
	}

	Color CheckForWarnings()
	{
		bool warning = false;
		foreach (var batt in battery)
		{
			if(batt == null) continue;
			if (batt.Temperature > LocalSettings.Singleton.Battery.WarningTemperature) return Colors.Red;
			if (batt.Voltage < 6 * LocalSettings.Singleton.Battery.CriticalVoltage) return Colors.Red;
			if (batt.Voltage < 6 * LocalSettings.Singleton.Battery.WarningVoltage) warning = true;
		}

		return warning ? Colors.Yellow : Colors.White;
	}

	Color CheckForWarningsAlt()
	{
		if (_currentVoltageAlt < LocalSettings.Singleton.Battery.CriticalVoltage)
		{
			return Colors.Red;
		}
		else
		{
			return Colors.White;
		}
	}

	void UpdateLabels(VBoxContainer outContainer)
	{
		VBoxContainer container = outContainer.GetNode<HBoxContainer>("HBoxContainer").GetNode<VBoxContainer>("BatBox");

		container.GetNode<Label>("IdLabel").Text = "Battery ID: " + data.ID;
		container.GetNode<Label>("PercLabel").Text = "Battery %: " + data.ChargePercent.ToString("F1") + "%";
		
		container.GetNode<Label>("VbatLabel").Text = "VBat: " + data.Voltage.ToString("F1") + "V";

		if(data.Voltage < 6*LocalSettings.Singleton.Battery.CriticalVoltage)
			container.GetNode<Label>("VbatLabel").SetModulate(Colors.Red);
		else if(data.Voltage < 6*LocalSettings.Singleton.Battery.WarningVoltage)
			container.GetNode<Label>("VbatLabel").SetModulate(Colors.Yellow);
		else
			container.GetNode<Label>("VbatLabel").SetModulate(Colors.White);

		container.GetNode<Label>("HotswapLabel").Text = "Hotswap: " + data.HotswapStatus.ToString();
		container.GetNode<Label>("StatusLabel").Text = "Status: " + data.Status.ToString();
		container.GetNode<Label>("CurrentLabel").Text = "Current: " + data.Current.ToString("F1") + "A";
		container.GetNode<Label>("TemperatureLabel").Text = "Temperature: " + data.Temperature.ToString("F1") + "C";
		if (data.Temperature > LocalSettings.Singleton.Battery.WarningTemperature)
			container.GetNode<Label>("TemperatureLabel").SetModulate(Colors.Orange);
		else
			container.GetNode<Label>("TemperatureLabel").SetModulate(Colors.White);

		container.GetNode<Label>("TimeLabel").Text = "Est. Time: " + data.Time.ToString("F0") + "min";
	}

	void OnBatteryControl(int slot, MqttClasses.BatterySet set)
	{
		int temp = 0;
		foreach (var batt in battery)
		{
			if (batt.HotswapStatus == MqttClasses.HotswapStatus.OnMan ||
			    batt.HotswapStatus == MqttClasses.HotswapStatus.OnAuto)
			{
				temp++;
			}
		}

		if(temp < 2 && set == MqttClasses.BatterySet.Off) return;

		MqttClasses.BatteryControl control = new MqttClasses.BatteryControl()
		{
			Slot = slot,
			Set = set
		};
		OnBatteryControlChanged(control);
	}

	private async Task OnBatteryControlChanged(MqttClasses.BatteryControl arg)
	{
		await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicBatteryControl,
			JsonSerializer.Serialize(arg));
	}

	int CalculateAverageBatteryPercent()
	{
		int sum = 0;
		int count = 0;
		foreach (var batt in battery)
		{
			if (batt != null && batt.Status != MqttClasses.BatteryStatus.Fault)
			{
				sum += (int)batt.ChargePercent;
				count++;
			}
		}

		if(LocalSettings.Singleton.Battery.AverageAll) count = 3;

		return count == 0 ? 0 : sum / count;
	}

	public override void _Ready()
	{
		//batt1.GetNode<Button>("RequestDataButton1").Pressed += () => OnBatteryControl(1, MqttClasses.BatterySet.RequestData);
		//batt2.GetNode<Button>("RequestDataButton2").Pressed += () => OnBatteryControl(2, MqttClasses.BatterySet.RequestData);
		//batt3.GetNode<Button>("RequestDataButton3").Pressed += () => OnBatteryControl(3, MqttClasses.BatterySet.RequestData);

		batt1.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("AutoButton1").Pressed += () => OnBatteryControl(1, MqttClasses.BatterySet.Auto);
		batt2.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("AutoButton2").Pressed += () => OnBatteryControl(2, MqttClasses.BatterySet.Auto);
		batt3.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("AutoButton3").Pressed += () => OnBatteryControl(3, MqttClasses.BatterySet.Auto);
		
		batt1.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OnButton1").Pressed += () => OnBatteryControl(1, MqttClasses.BatterySet.On);
		batt2.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OnButton2").Pressed += () => OnBatteryControl(2, MqttClasses.BatterySet.On);
		batt3.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OnButton3").Pressed += () => OnBatteryControl(3, MqttClasses.BatterySet.On);
		
		batt1.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OffButton1").Pressed += () => OnBatteryControl(1, MqttClasses.BatterySet.Off);
		batt2.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OffButton2").Pressed += () => OnBatteryControl(2, MqttClasses.BatterySet.Off);
		batt3.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OffButton3").Pressed += () => OnBatteryControl(3, MqttClasses.BatterySet.Off);
	}
}
