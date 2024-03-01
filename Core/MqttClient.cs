using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Server;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model.Settings;
using RoverControlApp.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RoverControlApp.Core
{
    public class MqttClient : IDisposable
	{
		public event Func<CommunicationState?, Task>? OnConnectionChanged;
		public event Func<string, MqttApplicationMessage?, Task>? OnMessageReceivedAsync;
		public event Action? OnClientStarted;

		private IManagedMqttClient? _managedMqttClient;
		private Mqtt _settingsMqtt;
		private CancellationTokenSource _cts;
		private Thread? _mqttThread;

		private CommunicationState _connectionState;

		private Dictionary<string, MqttApplicationMessage?>? _responses;

		//public bool ConnectionState => _managedMqttClient?.ConnectionState ?? false;
		public CommunicationState ConnectionState
		{
			get => _connectionState;
			private set
			{
				if (_connectionState == value) return;
				_connectionState = value;
				OnConnectionChanged?.Invoke(value);
			}
		}

		public MqttClient(Mqtt settingsMqtt)
		{
			_settingsMqtt = settingsMqtt;
			_cts = new CancellationTokenSource();
			_responses = new Dictionary<string, MqttApplicationMessage?>();
			_mqttThread = new Thread(ThreadWork) { IsBackground = true, Name = "MqttThread", Priority = ThreadPriority.BelowNormal };
			_mqttThread.Start();
		}

		private async void ThreadWork()
		{
			EventLogger.LogMessage("MQTT: Thread started");

			await Connect_Client();
			SpinWait.SpinUntil(() => _cts.IsCancellationRequested);

			EventLogger.LogMessage("MQTT: Cancellation requested. Stopping.");
			await StopClient();
			_managedMqttClient!.DisconnectedAsync -= HandleDisconnected;
			_managedMqttClient.ConnectedAsync -= HandleConnected;
			_managedMqttClient.SynchronizingSubscriptionsFailedAsync -= OnSynchronizingSubscriptionsFailedAsync;
			_managedMqttClient?.Dispose();
		}

		private async Task Connect_Client()
		{
			var mqttFactory = new MqttFactory();

			_managedMqttClient = mqttFactory.CreateManagedMqttClient();

			var mqttClientOptions = new MqttClientOptionsBuilder()
				.WithTcpServer(_settingsMqtt.BrokerIp, _settingsMqtt.BrokerPort)
				.WithKeepAlivePeriod(TimeSpan.FromSeconds(_settingsMqtt.PingInterval))
				.WithWillTopic($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicRoverStatus}")
				.WithWillPayload(JsonSerializer.Serialize(new MqttClasses.RoverStatus() { CommunicationState = CommunicationState.Faulted }))
				.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
				.WithWillRetain()
				.Build();

			var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
				.WithClientOptions(mqttClientOptions)
				.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
				.WithMaxPendingMessages(99)
				.WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
				.Build();

			_managedMqttClient.DisconnectedAsync += HandleDisconnected;
			_managedMqttClient.ConnectedAsync += HandleConnected;
			_managedMqttClient.SynchronizingSubscriptionsFailedAsync += OnSynchronizingSubscriptionsFailedAsync;
			_managedMqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;

			await _managedMqttClient.StartAsync(managedMqttClientOptions);

			EventLogger.LogMessage("MQTT: The managed MQTT client started.");


			await SubscribeToAllTopics();

			OnClientStarted?.Invoke();
		}

		private Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
		{
			//EventLogger.LogMessage($"MQTT: Message received on topic {arg.ApplicationMessage.Topic} with: " +
			//									  $"{arg.ApplicationMessage.ConvertPayloadToString()}");
			if (_responses == null) return Task.CompletedTask;

			var topic = arg.ApplicationMessage.Topic[(_settingsMqtt.TopicMain.Length + 1)..];
			var payload = arg.ApplicationMessage;

			if (_responses.ContainsKey(topic))
				_responses[topic] = payload;
			else if (!_responses.TryAdd(topic, payload))
				EventLogger.LogMessage($"MQTT: Adding {payload} on topic {topic} to dictionary failed");
			OnMessageReceivedAsync?.Invoke(topic, payload);
			return Task.CompletedTask;
		}

		public string? GetReceivedMessageOnTopicAsString(string? subtopic)
		{
			if (subtopic == null) return null;
			// var response = "";
			MqttApplicationMessage? response = new();
			var succ = _responses?.TryGetValue(subtopic, out response) ?? false;
			return succ ? response.ConvertPayloadToString() : null;
		}

		public MqttApplicationMessage? GetReceivedMessageOnTopic(string? subtopic)
		{
			if (subtopic == null) return null;
			MqttApplicationMessage? response = new();
			var succ = _responses?.TryGetValue(subtopic, out response) ?? false;
			return succ ? response : null;
		}

		private Task OnSynchronizingSubscriptionsFailedAsync(ManagedProcessFailedEventArgs arg)
		{
			EventLogger.LogMessage($"MQTT: Synchronizing subscriptions failed with: {arg}");
			return Task.CompletedTask;
		}

		private async Task SubscribeToAllTopics()
		{
			EventLogger.LogMessage("MQTT: Subscribing to all topics:");
			await SubscribeToTopic(_settingsMqtt.TopicRoverStatus, MqttQualityOfServiceLevel.ExactlyOnce);
			await SubscribeToTopic(_settingsMqtt.TopicMissionStatus, MqttQualityOfServiceLevel.ExactlyOnce);
			await SubscribeToTopic(_settingsMqtt.TopicKmlListOfActiveObj, MqttQualityOfServiceLevel.ExactlyOnce);
			await SubscribeToTopic(_settingsMqtt.TopicRoverFeedback, MqttQualityOfServiceLevel.ExactlyOnce);
			await SubscribeToTopic(_settingsMqtt.TopicWheelFeedback);
			await SubscribeToTopic(_settingsMqtt.TopicEStopStatus);
		}

		private async Task SubscribeToTopic(string subtopic, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
		{
			EventLogger.LogMessage($"MQTT: Subscribing to topic: {subtopic}");
			await _managedMqttClient.SubscribeAsync(_settingsMqtt.TopicMain + '/' + subtopic, qos);
		}

		public async Task EnqueueAsync(string subtopic, string? arg, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
		{
			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{subtopic}",
				arg, qos, retain);
		}

		private async Task StopClient()
		{
			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicRoverControl}",
				JsonSerializer.Serialize(new MqttClasses.RoverControl()));

			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicManipulatorControl}",
				JsonSerializer.Serialize(new MqttClasses.ManipulatorControl()));

			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicRoverStatus}",
				JsonSerializer.Serialize(new MqttClasses.RoverStatus() { CommunicationState = CommunicationState.Closed }),
				MqttQualityOfServiceLevel.ExactlyOnce, true);

			//await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicMissionStatus}",
			//	JsonSerializer.Serialize(new MqttClasses.RoverMissionStatus()), MqttQualityOfServiceLevel.ExactlyOnce, true);

			await Task.Run(async Task? () =>
			{
				for (int i = 0; (_managedMqttClient?.PendingApplicationMessagesCount > 0) && (i < 10); i++)
				{
					await Task.Delay(TimeSpan.FromMilliseconds(100));
				}
			});

			await _managedMqttClient?.StopAsync(_managedMqttClient?.PendingApplicationMessagesCount == 0)!;
			SpinWait.SpinUntil(() => _managedMqttClient.IsConnected, 250);
		}

		private Task HandleConnected(MqttClientConnectedEventArgs arg)
		{
			EventLogger.LogMessage("MQTT: Connected");
			ConnectionState = CommunicationState.Opened;
			return Task.CompletedTask;
		}

		private Task HandleDisconnected(MqttClientDisconnectedEventArgs arg)
		{
			EventLogger.LogMessage("MQTT: Disconnected");
			ConnectionState = CommunicationState.Faulted;
			return Task.CompletedTask;
		}


		public void Dispose()
		{
			//_eventsToDispose.ForEach(o => o.Dispose());
			_cts.Cancel();
			_mqttThread?.Join(500);
			Thread.Sleep(1000);
			_mqttThread = null;
		}
	}
}
