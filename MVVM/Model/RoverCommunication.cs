using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.MVVM.Model
{
	public class RoverCommunication : IDisposable
	{
		private IManagedMqttClient _managedMqttClient;



		public async Task Connect_Client()
		{
			var mqttFactory = new MqttFactory();

			_managedMqttClient = mqttFactory.CreateManagedMqttClient();

			var mqttClientOptions = new MqttClientOptionsBuilder()
				.WithTcpServer(MainViewModel.Settings.Settings.Mqtt.MqttBrokerIp, MainViewModel.Settings.Settings.Mqtt.MqttBrokerPort)
				.WithKeepAlivePeriod(TimeSpan.FromSeconds(2.5))
				.WithWillTopic($"{MainViewModel.Settings.Settings.Mqtt.MqttTopic}/{MainViewModel.Settings.Settings.Mqtt.MqttTopicRoverStatus}")
				.WithWillPayload(JsonSerializer.Serialize(new MqttClasses.RoverStatus() { CommunicationState = CommunicationState.Faulted }))
				.Build();

			var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
				.WithClientOptions(mqttClientOptions)
				.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
				.WithMaxPendingMessages(3)
				.Build();

			await _managedMqttClient.StartAsync(managedMqttClientOptions);

			// The application message is not sent. It is stored in an internal queue and
			// will be sent when the client is connected.
			// await _managedMqttClient.EnqueueAsync("Topic", "Payload", MqttQualityOfServiceLevel.ExactlyOnce, true);

			MainViewModel.EventLogger.LogMessage("MQTT: The managed MQTT client is connected.");

			await _managedMqttClient.EnqueueAsync(
				$"{MainViewModel.Settings.Settings.Mqtt.MqttTopic}/{MainViewModel.Settings.Settings.Mqtt.MqttTopicRoverStatus}",
				JsonSerializer.Serialize(new MqttClasses.RoverStatus()
				{
					CommunicationState = CommunicationState.Opened, 
					ControlMode = MqttClasses.ControlMode.Rover
				}));

			MainViewModel.PressedKeys.OnRoverMovementVector += RoverMovementVectorChanged;
		}

		private async void RoverMovementVectorChanged(object sender, MqttClasses.RoverControl e)
		{
			await _managedMqttClient.EnqueueAsync($"{MainViewModel.Settings.Settings.Mqtt.MqttTopic}/{MainViewModel.Settings.Settings.Mqtt.MqttTopicRoverControl}",
				JsonSerializer.Serialize(e));
		}

		public async Task StopClient()
		{
			await _managedMqttClient.EnqueueAsync($"{MainViewModel.Settings.Settings.Mqtt.MqttTopic}/{MainViewModel.Settings.Settings.Mqtt.MqttTopicRoverStatus}",
				JsonSerializer.Serialize(new MqttClasses.RoverStatus() { CommunicationState = CommunicationState.Closed }));

			await _managedMqttClient.EnqueueAsync($"{MainViewModel.Settings.Settings.Mqtt.MqttTopic}/{MainViewModel.Settings.Settings.Mqtt.MqttTopicRoverControl}",
				JsonSerializer.Serialize(new MqttClasses.RoverControl()));
			
			await _managedMqttClient.StopAsync();
		}

		public void Dispose()
		{
			_managedMqttClient?.Dispose();
		}
	}
}
