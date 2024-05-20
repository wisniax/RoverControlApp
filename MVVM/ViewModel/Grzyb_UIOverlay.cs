using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class Grzyb_UIOverlay : UIOverlay
{
	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.DarkGreen, Colors.LightGreen, "Moshroom: Free", "Moshroom: ") },
		{ 1, new(Colors.DarkRed, Colors.Orange, "Moshroom: MOLDED", "Moshroom: ") }
	};

	public override void _Ready()
	{
		base._Ready();
		ControlMode = 1;

		var lastMessage = MqttNode.Singleton.GetReceivedMessageOnTopic(LocalSettings.Singleton.Mqtt.TopicEStopStatus);

		if(lastMessage is not null)
			MqttSubscriber(LocalSettings.Singleton.Mqtt.TopicEStopStatus, new MqttNodeMessage(lastMessage));
	}

	public void MqttSubscriber(string subTopic, MqttNodeMessage msg)
	{
		if (MainViewModel.Settings?.Mqtt.TopicEStopStatus is null || subTopic != MainViewModel.Settings?.Mqtt.TopicEStopStatus || msg == null || msg.Message.PayloadSegment.Count == 0)
			return;

		//skip first 4bytes dunno what it is
		string payloadStingified = Encoding.UTF8.GetString(msg.Message.PayloadSegment.Array, 4, msg.Message.PayloadSegment.Count - 4);

		var doc = JsonDocument.Parse(payloadStingified);
		doc.RootElement.GetProperty("mushroom");

		ControlMode = doc.RootElement.GetProperty("mushroom").GetInt32();
	}
}
