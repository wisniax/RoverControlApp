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
	[Export] VBoxContainer batt4 = null!;

	private MqttClasses.BatteryInfo data;

	private MqttClasses.BatteryInfo[] battery = new MqttClasses.BatteryInfo[4];

	public event Func<int, int, Task>? OnBatteryPercentageChanged;

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += BatteryInfoChanged;
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= BatteryInfoChanged;
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
				battery[1] = data;
				break;
			case 2:
				CallDeferred("UpdateLabels", batt2);
				battery[2] = data;
				break;
			case 3:
				CallDeferred("UpdateLabels", batt3);
				battery[3] = data;
				break;
			case 4:
				CallDeferred("UpdateLabels", batt4);
				battery[4] = data;
				break;
			default:
				EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Invalid battery slot");
				return Task.CompletedTask;
		}

		OnBatteryPercentageChanged.Invoke(CalculateAverageBatteryPercent(), CheckForWarnings());
		
		return Task.CompletedTask;
	}

	int CheckForWarnings()
	{
		bool warning = false;
		foreach (var batt in battery)
		{
			if(batt == null) continue;
			if (batt.Temperature > LocalSettings.Singleton.Battery.WarningTemperature) return 2;
			if (batt.Voltage < 6 * LocalSettings.Singleton.Battery.CriticalVoltage) return 2;
			if (batt.Voltage < 6 * LocalSettings.Singleton.Battery.WarningVoltage) warning = true;
		}

		return warning ? 1 : 0;
	}

	void UpdateLabels(VBoxContainer outContainer)
	{
		
		VBoxContainer container = outContainer.GetNode<HBoxContainer>("HBoxContainer").GetNode<VBoxContainer>("BatBox");

		if (data.Status == MqttClasses.BatteryStatus.Disconnected)
		{
			container.Visible = false;
			outContainer.GetNode<Label>("SlotEmpty").Visible = true;
		}
		else
		{
			container.Visible = true;
			outContainer.GetNode<Label>("SlotEmpty").Visible = false;
		}

		container.GetNode<Label>("IdLabel").Text = "Battery ID: " + data.ID;
		container.GetNode<Label>("PercLabel").Text = "Battery %: " + data.ChargePercent.ToString("F1") + "%";
		
		container.GetNode<Label>("VbatLabel").Text = "VBat: " + data.Voltage.ToString("F1") + "V";

		if(data.Voltage < 6*LocalSettings.Singleton.Battery.CriticalVoltage)
			container.GetNode<Label>("VbatLabel").SetModulate(Colors.Red);
		else if(data.Voltage < 6*LocalSettings.Singleton.Battery.WarningVoltage)
			container.GetNode<Label>("VbatLabel").SetModulate(Colors.Yellow);
		else
			container.GetNode<Label>("VbatLabel").SetModulate(Colors.White);

		container.GetNode<Label>("StatusLabel").Text = "Status: " + data.Status.ToString();
		container.GetNode<Label>("CurrentLabel").Text = "Current: " + data.Current.ToString("F1") + "A";
		container.GetNode<Label>("TemperatureLabel").Text = "Temperature: " + data.Temperature.ToString("F1") + "C";
		if (data.Temperature > LocalSettings.Singleton.Battery.WarningTemperature)
		{
			container.GetNode<Label>("TemperatureLabel").SetModulate(Colors.Orange);
			
		}
		else
			container.GetNode<Label>("TemperatureLabel").SetModulate(Colors.White);

		container.GetNode<Label>("TimeLabel").Text = "Est. Time: " + data.Time.ToString("F0") + "min";
		container.GetNode<Label>("SetLabel").Text = "Set: " + data.Set.ToString();
	}

	void OnBatteryControl(int slot, MqttClasses.BatterySet set)
	{
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
			if (batt != null && batt.Status != MqttClasses.BatteryStatus.Fault && batt.Status != MqttClasses.BatteryStatus.Disconnected)
			{
				sum += (int)batt.ChargePercent;
				count++;
			}
		}

		if(LocalSettings.Singleton.Battery.AverageAll) count = 4;
		return sum / count;
	}

	public override void _Ready()
	{
		//batt1.GetNode<Button>("RequestDataButton1").Pressed += () => OnBatteryControl(1, MqttClasses.BatterySet.RequestData);
		//batt2.GetNode<Button>("RequestDataButton2").Pressed += () => OnBatteryControl(2, MqttClasses.BatterySet.RequestData);
		//batt3.GetNode<Button>("RequestDataButton3").Pressed += () => OnBatteryControl(3, MqttClasses.BatterySet.RequestData);
		//batt4.GetNode<Button>("RequestDataButton4").Pressed += () => OnBatteryControl(4, MqttClasses.BatterySet.RequestData);

		batt1.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("AutoButton1").Pressed += () => OnBatteryControl(1, MqttClasses.BatterySet.Auto);
		batt2.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("AutoButton2").Pressed += () => OnBatteryControl(2, MqttClasses.BatterySet.Auto);
		batt3.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("AutoButton3").Pressed += () => OnBatteryControl(3, MqttClasses.BatterySet.Auto);
		batt4.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("AutoButton4").Pressed += () => OnBatteryControl(4, MqttClasses.BatterySet.Auto);

		batt1.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OnButton1").Pressed += () => OnBatteryControl(1, MqttClasses.BatterySet.On);
		batt2.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OnButton2").Pressed += () => OnBatteryControl(2, MqttClasses.BatterySet.On);
		batt3.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OnButton3").Pressed += () => OnBatteryControl(3, MqttClasses.BatterySet.On);
		batt4.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OnButton4").Pressed += () => OnBatteryControl(4, MqttClasses.BatterySet.On);

		batt1.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OffButton1").Pressed += () => OnBatteryControl(1, MqttClasses.BatterySet.Off);
		batt2.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OffButton2").Pressed += () => OnBatteryControl(2, MqttClasses.BatterySet.Off);
		batt3.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OffButton3").Pressed += () => OnBatteryControl(3, MqttClasses.BatterySet.Off);
		batt4.GetNode<HBoxContainer>("HBoxContainer2").GetNode<Button>("OffButton4").Pressed += () => OnBatteryControl(4, MqttClasses.BatterySet.Off);
	}
}
