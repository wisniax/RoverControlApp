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

	private MqttClasses.BatteryInfo battery1;
	private MqttClasses.BatteryInfo battery2;
	private MqttClasses.BatteryInfo battery3;
	private MqttClasses.BatteryInfo battery4;

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
				break;
			case 2:
				CallDeferred("UpdateLabels", batt2);
				break;
			case 3:
				CallDeferred("UpdateLabels", batt3);
				break;
			case 4:
				CallDeferred("UpdateLabels", batt4);
				break;
			default:
				EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Invalid battery slot");
				return Task.CompletedTask;
		}

		
		return Task.CompletedTask;
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
			container.GetNode<Label>("TemperatureLabel").SetModulate(Colors.Red);
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

	// Called when the node enters the scene tree for the first time.
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
