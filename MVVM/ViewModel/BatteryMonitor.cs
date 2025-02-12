using Godot;
using System;
using RoverControlApp.Core;
using MQTTnet;
using System.Threading.Tasks;
using System.Text.Json;
using RoverControlApp.MVVM.Model;

public partial class BatteryMonitor : Panel
{
	[Export] VBoxContainer batBox1;
	[Export] VBoxContainer batBox2;
	[Export] VBoxContainer batBox3;
	[Export] VBoxContainer batBox4;

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

		VBoxContainer updateBox;
		MqttClasses.BatteryInfo data = JsonSerializer.Deserialize<MqttClasses.BatteryInfo>(msg.ConvertPayloadToString());

		switch (data.Slot)
		{
			case 1:
				updateBox = batBox1;
				break;
			case 2:
				updateBox = batBox2;
				break;
			case 3:
				updateBox = batBox3;
				break;
			case 4:
				updateBox = batBox4;
				break;
			default:
				EventLogger.LogMessage("BatteryMonitor", EventLogger.LogLevel.Error, "Invalid battery slot");
				return Task.CompletedTask;
		}

		updateBox.GetNode<Label>("IdLabel").Text = "Battery ID: " + data.ID;
		updateBox.GetNode<Label>("PercLabel").Text = "Battery %: " + data.ChargePercent.ToString("F1") + "%";
		updateBox.GetNode<Label>("VbatLabel").Text = "VBat: " + data.Voltage.ToString("F2") + "V";
		//todo czerwony kolor dla niskiego poziomu baterii
		updateBox.GetNode<Label>("StatusLabel").Text = "Status: " + data.Status.ToString();
		updateBox.GetNode<Label>("CurrentLabel").Text = "Current: " + data.Current.ToString() + "A";
		updateBox.GetNode<Label>("TemperatureLabel").Text = "Temperature: " + data.Temperature.ToString("F2") + "°C";
		updateBox.GetNode<Label>("TimeLabel").Text = "Est. Time: " + data.Time.ToString("F2") + "h";
		return Task.CompletedTask;
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
