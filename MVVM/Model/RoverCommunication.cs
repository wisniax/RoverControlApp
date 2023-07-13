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
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.MVVM.Model
{
	public class RoverCommunication : IDisposable
	{
		public event Func<MqttClasses.ControlMode, Task>? OnControlModeChanged;
		//public event Func<MqttClasses.ControlMode, Task>? OnControlStatusChanged;

		private IManagedMqttClient? _managedMqttClient;
		private LocalSettings.Mqtt _settingsMqtt;
		private CancellationTokenSource _cts;
		private Thread? _mqttThread;
		private volatile MqttClasses.ControlMode _controlMode;

		public MqttClasses.ControlMode ControlMode
		{
			get => _controlMode;
			set
			{
				_controlMode = value;
				OnControlModeChanged?.Invoke(value);
			}
		}


		private MqttClasses.RoverStatus RoverStatus => new()
		{
			CommunicationState = IsConnected ? CommunicationState.Opened : CommunicationState.Closed,
			ControlMode = ControlMode,
			PadConnected = MainViewModel.PressedKeys.PadConnected
		};

		public bool IsConnected => _managedMqttClient?.IsConnected ?? false;

		public RoverCommunication(LocalSettings.Mqtt settingsMqtt)
		{
			_settingsMqtt = settingsMqtt;
			_cts = new CancellationTokenSource();
			_mqttThread = new Thread(ThreadWork) { IsBackground = true, Name = "MqttStream_Thread", Priority = ThreadPriority.BelowNormal };
			_mqttThread.Start();
			MainViewModel.PressedKeys.OnControlModeButtonPressed += PressedKeysOnOnControlModeButtonPressed;
		}

		private Task PressedKeysOnOnControlModeButtonPressed()
		{
			if ((int)ControlMode + 1 >= Enum.GetNames<MqttClasses.ControlMode>().Length)
				ControlMode = MqttClasses.ControlMode.EStop;
			else ControlMode++;
			return Task.CompletedTask;
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

		public async Task Connect_Client()
		{
			var mqttFactory = new MqttFactory();

			_managedMqttClient = mqttFactory.CreateManagedMqttClient();

			var mqttClientOptions = new MqttClientOptionsBuilder()
				.WithTcpServer(_settingsMqtt.BrokerIp, _settingsMqtt.BrokerPort)
				.WithKeepAlivePeriod(TimeSpan.FromSeconds(_settingsMqtt.PingInterval))
				.WithWillTopic($"{_settingsMqtt.MainTopic}/{_settingsMqtt.TopicRoverStatus}")
				.WithWillPayload(JsonSerializer.Serialize(new MqttClasses.RoverStatus() { CommunicationState = CommunicationState.Faulted }))
				.WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
				.WithWillRetain()
				.Build();

			var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
				.WithClientOptions(mqttClientOptions)
				.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
				.WithMaxPendingMessages(3)
				.Build();

			await _managedMqttClient.StartAsync(managedMqttClientOptions);

			MainViewModel.EventLogger.LogMessage("MQTT: The managed MQTT client started.");

			_managedMqttClient.DisconnectedAsync += HandleDisconnected;
			_managedMqttClient.ConnectedAsync += HandleConnected;

			OnControlModeChanged += RoverCommunication_OnControlModeChanged;
			MainViewModel.PressedKeys.OnPadConnectionChanged += b => RoverCommunication_OnControlStatusChanged();
			MainViewModel.PressedKeys.OnRoverMovementVector += RoverMovementVectorChanged!;
		}

		private async Task RoverCommunication_OnControlModeChanged(MqttClasses.ControlMode arg)
		{
			MainViewModel.EventLogger.LogMessage($"MQTT: Control Mode changed {arg}");
			await RoverCommunication_OnControlStatusChanged();
		}

		private async Task HandleConnected(MqttClientConnectedEventArgs arg)
		{
			await RoverCommunication_OnControlStatusChanged();
			MainViewModel.EventLogger.LogMessage($"MQTT: Connected");
		}

		private Task HandleDisconnected(MqttClientDisconnectedEventArgs arg)
		{
			MainViewModel.EventLogger.LogMessage($"MQTT: Disconnected");
			return Task.CompletedTask;
		}

		private async Task RoverCommunication_OnControlStatusChanged()
		{
			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.MainTopic}/{_settingsMqtt.TopicRoverStatus}",
				JsonSerializer.Serialize(RoverStatus), MqttQualityOfServiceLevel.ExactlyOnce, true);
		}

		private async void RoverMovementVectorChanged(object sender, MqttClasses.RoverControl e)
		{
			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.MainTopic}/{_settingsMqtt.TopicRoverControl}",
				JsonSerializer.Serialize(e));
		}

		public async Task StopClient()
		{
			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.MainTopic}/{_settingsMqtt.TopicRoverStatus}",
				JsonSerializer.Serialize(new MqttClasses.RoverStatus() { CommunicationState = CommunicationState.Closed }),
				MqttQualityOfServiceLevel.ExactlyOnce, true);

			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.MainTopic}/{_settingsMqtt.TopicRoverControl}",
				JsonSerializer.Serialize(new MqttClasses.RoverControl()));

			await _managedMqttClient?.StopAsync()!;
		}

		public void Dispose()
		{
			_cts.Cancel();
			_mqttThread?.Join();
			_mqttThread = null;
		}
	}
}
