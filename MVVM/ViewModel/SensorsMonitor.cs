using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

public partial class SensorsMonitor : Panel
{
	[Export] Label[] sensorLabel = new Label[5];
	[Export] HSlider[] sensorSlider = new HSlider[5];


	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += OnSensorDataReceived;
	}

	public override void _ExitTree()
	{
		MqttNode.Singleton.MessageReceivedAsync -= OnSensorDataReceived;
	}

	public Task OnSensorDataReceived(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicSamplerFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicSamplerFeedback)
			return Task.CompletedTask;
		if (msg is null || msg.PayloadSegment.Count == 0)
		{
			EventLogger.LogMessage("SamplerMonitor", EventLogger.LogLevel.Error, "Empty payload");
			return Task.CompletedTask;
		}

		try
		{
			CallDeferred("SensorsUpdate", msg.ConvertPayloadToString());

			return Task.CompletedTask;
		}
		catch (Exception e)
		{
			EventLogger.LogMessage("SensorMonitor", EventLogger.LogLevel.Error, $"{e.Message}");
			return Task.CompletedTask;
		}
	}

	void SensorsUpdate(string msg)
	{
		MqttClasses.SamplerFeedback message = JsonSerializer.Deserialize<MqttClasses.SamplerFeedback>(msg);
		if (message is null)
			throw new InvalidDataException("Invalid SamplerFeedback payload.");

		sensorLabel[0].Text = "Distance: " + message.Distance.ToString() + " cm";
		sensorSlider[0].Value = message.Distance;

		sensorLabel[1].Text = "WeightA: " + message.WeightA.ToString() + " g";
		sensorSlider[1].Value = message.WeightA;

		sensorLabel[2].Text = "WeightB: " + message.WeightB.ToString() + " g";
		sensorSlider[2].Value = message.WeightA;

		sensorLabel[3].Text = "WeightC: " + message.WeightC.ToString() + " g";
		sensorSlider[3].Value = message.WeightA;

		sensorLabel[4].Text = "Ph: " + message.Ph.ToString() + "";
		sensorSlider[4].Value = message.Ph;

	}
}
