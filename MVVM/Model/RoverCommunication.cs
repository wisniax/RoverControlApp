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
using GodotPlugins.Game;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Server;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.MVVM.Model
{
	public class RoverCommunication : IDisposable
	{
		public event Func<MqttClasses.RoverStatus?, Task>? OnRoverStatusChanged;

		private IManagedMqttClient? _managedMqttClient;
		private LocalSettings.Mqtt _settingsMqtt;
		private CancellationTokenSource _cts;
		private Thread? _mqttThread;
		//List<IDisposable> _eventsToDispose = new List<IDisposable>();

		private MqttClasses.ControlMode ControlMode => MainViewModel.PressedKeys.ControlMode;

		private MqttClasses.RoverStatus? _roverStatus;
		public MqttClasses.RoverStatus? RoverStatus
		{
			get => _roverStatus;
			private set
			{
				_roverStatus = value;
				OnRoverStatusChanged?.Invoke(value);
			}
		}

		private MqttClasses.RoverStatus GenerateRoverStatus
		{
			get
			{
				var obj = new MqttClasses.RoverStatus
				{
					CommunicationState = IsConnected ? CommunicationState.Opened : CommunicationState.Faulted,
					ControlMode = ControlMode,
					PadConnected = MainViewModel.PressedKeys.PadConnected
				};
				RoverStatus = obj;
				return obj;
			}
		}

		public bool IsConnected => _managedMqttClient?.IsConnected ?? false;

		public RoverCommunication(LocalSettings.Mqtt settingsMqtt)
		{
			_settingsMqtt = settingsMqtt;
			_cts = new CancellationTokenSource();
			_mqttThread = new Thread(ThreadWork) { IsBackground = true, Name = "MqttStream_Thread", Priority = ThreadPriority.BelowNormal };
			_mqttThread.Start();
			MainViewModel.PressedKeys.OnControlModeChanged += PressedKeys_OnControlModeChanged;

		}

		private async void ThreadWork()
		{
			MainViewModel.EventLogger.LogMessage("MQTT: Thread started");

			await Connect_Client();
			SpinWait.SpinUntil(() => _cts.IsCancellationRequested);

			MainViewModel.EventLogger.LogMessage("MQTT: Cancellation requested. Stopping.");
			await StopClient();
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

			MainViewModel.EventLogger.LogMessage("MQTT: The managed MQTT client started.");

			_managedMqttClient.DisconnectedAsync += HandleDisconnected;
			_managedMqttClient.ConnectedAsync += HandleConnected;

			MainViewModel.PressedKeys.OnPadConnectionChanged += b => RoverCommunication_OnControlStatusChanged();
			MainViewModel.PressedKeys.OnRoverMovementVector += RoverMovementVectorChanged!;
			MainViewModel.PressedKeys.OnManipulatorMovement += RoverManipulatorVectorChanged;
		}

		private async Task PressedKeys_OnControlModeChanged(MqttClasses.ControlMode arg)
		{
			MainViewModel.EventLogger.LogMessage($"MQTT: Control Mode changed {arg}");
			await RoverCommunication_OnControlStatusChanged();
		}

		private async Task HandleConnected(MqttClientConnectedEventArgs arg)
		{
			MainViewModel.EventLogger.LogMessage($"MQTT: Connected");
			await RoverCommunication_OnControlStatusChanged();
		}

		private Task HandleDisconnected(MqttClientDisconnectedEventArgs arg)
		{
			MainViewModel.EventLogger.LogMessage($"MQTT: Disconnected");
			return Task.CompletedTask;
			//await RoverCommunication_OnControlStatusChanged();
		}

		private async Task RoverCommunication_OnControlStatusChanged()
		{
			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicRoverStatus}",
				JsonSerializer.Serialize(GenerateRoverStatus), MqttQualityOfServiceLevel.ExactlyOnce, true);
		}

		private async Task RoverMovementVectorChanged(MqttClasses.RoverControl roverControl)
		{
			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicRoverControl}",
				JsonSerializer.Serialize(roverControl));
		}
		private async Task RoverManipulatorVectorChanged(MqttClasses.ManipulatorControl manipulatorControl)
		{
			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicManipulatorControl}",
				JsonSerializer.Serialize(manipulatorControl));
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

			await Task.Run(async Task? () =>
			{
				for (int i = 0; (_managedMqttClient?.PendingApplicationMessagesCount > 0) && (i < 10); i++)
				{
					await Task.Delay(TimeSpan.FromMilliseconds(100));
				}
			});

			await _managedMqttClient?.StopAsync(_managedMqttClient?.PendingApplicationMessagesCount == 0)!;
		}

		public void Dispose()
		{
			//_eventsToDispose.ForEach(o => o.Dispose());
			_cts.Cancel();
			_mqttThread?.Join();
			_mqttThread = null;
		}
	}
}
