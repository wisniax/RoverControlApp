using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Server;
using RoverControlApp.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RoverControlApp.Core
{
	public class MqttClient : IDisposable
	{
		public event Func<CommunicationState?, Task>? OnConnectionChanged;
		public event Action? OnClientStarted;

		private IManagedMqttClient? _managedMqttClient;
		private LocalSettings.Mqtt _settingsMqtt;
		private CancellationTokenSource _cts;
		private Thread? _mqttThread;

		private CommunicationState _connectionState;

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

		public MqttClient(LocalSettings.Mqtt settingsMqtt)
		{
			_settingsMqtt = settingsMqtt;
			_cts = new CancellationTokenSource();
			_mqttThread = new Thread(ThreadWork) { IsBackground = true, Name = "MqttStream_Thread", Priority = ThreadPriority.BelowNormal };
			_mqttThread.Start();
		}

		private async void ThreadWork()
		{
			MainViewModel.EventLogger?.LogMessage("MQTT: Thread started");

			await Connect_Client();
			SpinWait.SpinUntil(() => _cts.IsCancellationRequested);

			MainViewModel.EventLogger?.LogMessage("MQTT: Cancellation requested. Stopping.");
			await StopClient();
			_managedMqttClient!.DisconnectedAsync -= HandleDisconnected;
			_managedMqttClient.ConnectedAsync -= HandleConnected;
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

			await _managedMqttClient.StartAsync(managedMqttClientOptions);

			MainViewModel.EventLogger?.LogMessage("MQTT: The managed MQTT client started.");
			_managedMqttClient.DisconnectedAsync += HandleDisconnected;
			_managedMqttClient.ConnectedAsync += HandleConnected;

			OnClientStarted?.Invoke();
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

			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicMissionStatus}",
				JsonSerializer.Serialize(new MqttClasses.RoverMissionStatus()), MqttQualityOfServiceLevel.ExactlyOnce, true);

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
			MainViewModel.EventLogger?.LogMessage("MQTT: Connected");
			ConnectionState = CommunicationState.Opened;
			return Task.CompletedTask;
		}

		private Task HandleDisconnected(MqttClientDisconnectedEventArgs arg)
		{
			MainViewModel.EventLogger?.LogMessage("MQTT: Disconnected");
			ConnectionState = CommunicationState.Faulted;
			return Task.CompletedTask;
		}


		public void Dispose()
		{
			//_eventsToDispose.ForEach(o => o.Dispose());
			_cts.Cancel();
			_mqttThread?.Join(500);
			Thread.Sleep(250);
			_mqttThread = null;
		}
	}
}
