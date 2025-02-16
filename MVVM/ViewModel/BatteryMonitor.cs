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
	[Export] VBoxContainer batBox1 = null!;
	[Export] VBoxContainer batBox2 = null!;
	[Export] VBoxContainer batBox3 = null!;
	[Export] VBoxContainer batBox4 = null!;

	public event Func<MqttClasses.BatteryInfo?, Task>? BatteryInfoChanged;

	private MqttClasses.BatteryInfo data;

	private MqttClasses.BatteryInfo batt1;
	private MqttClasses.BatteryInfo batt2;
	private MqttClasses.BatteryInfo batt3;
	private MqttClasses.BatteryInfo batt4;

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += OnBatteryInfoChanged;
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= OnBatteryInfoChanged;
	}

	public Task OnBatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicBatteryInfo) || subTopic != LocalSettings.Singleton.Mqtt.TopicBatteryInfo)
			return Task.CompletedTask;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}

		VBoxContainer updateBox = null;
		data = JsonSerializer.Deserialize<MqttClasses.BatteryInfo>(msg.ConvertPayloadToString());

		switch (data.Slot)
		{
			case 1:
				CallDeferred("UpdateLabels", batBox1);
				batt1 = data;
				break;
			case 2:
				CallDeferred("UpdateLabels", batBox2);
				batt2 = data;
				break;
			case 3:
				CallDeferred("UpdateLabels", batBox3);
				batt3 = data;
				break;
			case 4:
				CallDeferred("UpdateLabels", batBox4);
				batt4 = data;
				break;
			default:
				EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Invalid battery slot");
				return Task.CompletedTask;
		}

		
		return Task.CompletedTask;
	}

	void UpdateLabels(VBoxContainer container)
	{
		container.GetNode<Label>("IdLabel").Text = "Battery ID: " + data.ID;
		container.GetNode<Label>("PercLabel").Text = "Battery %: " + data.ChargePercent.ToString("F1") + "%";
		
		container.GetNode<Label>("VbatLabel").Text = "VBat: " + data.Voltage.ToString("F1") + "V";
		if(data.Voltage < 6*LocalSettings.Singleton.Battery.WarningVoltage)
			container.GetNode<Label>("VbatLabel").SetModulate(Colors.Yellow);
		else if(data.Voltage < 6*LocalSettings.Singleton.Battery.CriticalVoltage)
			container.GetNode<Label>("VbatLabel").SetModulate(Colors.Red);
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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
