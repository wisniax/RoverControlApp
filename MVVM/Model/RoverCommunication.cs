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
			/*
			 * This sample creates a simple managed MQTT client and connects to a public broker.
			 *
			 * The managed client extends the existing _MqttClient_. It adds the following features.
			 * - Reconnecting when connection is lost.
			 * - Storing pending messages in an internal queue so that an enqueue is possible while the client remains not connected.
			 */

			var mqttFactory = new MqttFactory();

			_managedMqttClient = mqttFactory.CreateManagedMqttClient();

			var mqttClientOptions = new MqttClientOptionsBuilder()
				.WithTcpServer(MainViewModel.Settings.Settings.MqttBrokerIp, MainViewModel.Settings.Settings.MqttBrokerPort)
				.WithKeepAlivePeriod(TimeSpan.FromSeconds(5))
				.WithWillTopic($"{MainViewModel.Settings.Settings.MqttTopic}/{MainViewModel.Settings.Settings.MqttTopicRoverStatus}")
				.WithWillPayload(JsonSerializer.Serialize(new Core.MqttClasses.JoyStatus() { CommunicationState = CommunicationState.Faulted }))
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

			MainViewModel.PressedKeys.OnRoverMovementVector += RoverMovementVectorChanged;

			// Wait until the queue is fully processed.
			//SpinWait.SpinUntil(() => _managedMqttClient.PendingApplicationMessagesCount == 0, 10000);

			//Console.WriteLine($"Pending messages = {_managedMqttClient.PendingApplicationMessagesCount}");
		}

		private async void RoverMovementVectorChanged(object sender, MqttClasses.RoverControl e)
		{
			await _managedMqttClient.EnqueueAsync($"{MainViewModel.Settings.Settings.MqttTopic}/{MainViewModel.Settings.Settings.MqttTopicRoverControl}",
				JsonSerializer.Serialize(e), MqttQualityOfServiceLevel.ExactlyOnce, true);
		}

		public void Dispose()
		{
			_managedMqttClient?.Dispose();
		}
	}
}
