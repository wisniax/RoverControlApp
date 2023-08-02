using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Server;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;
using System;
using System.ServiceModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model
{
	public class RoverCommunication : IDisposable
	{
		public event Func<MqttClasses.RoverStatus?, Task>? OnRoverStatusChanged;
		private MqttClasses.ControlMode ControlMode => MainViewModel.PressedKeys?.ControlMode ?? MqttClasses.ControlMode.EStop;

		//List<IDisposable> _eventsToDispose = new List<IDisposable>();

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
					CommunicationState = (bool)MainViewModel.MqttClient?.IsConnected ? CommunicationState.Opened : CommunicationState.Faulted,
					ControlMode = ControlMode,
					PadConnected = MainViewModel.PressedKeys?.PadConnected ?? false
				};
				RoverStatus = obj;
				return obj;
			}
		}


		public RoverCommunication(LocalSettings.Mqtt settingsMqtt)
		{
			if (MainViewModel.PressedKeys != null)
				MainViewModel.PressedKeys.OnControlModeChanged += PressedKeys_OnControlModeChanged;
		}

		

		private async Task OnRoverMissionStatusChanged(MqttClasses.RoverMissionStatus? arg)
		{
			await _managedMqttClient.EnqueueAsync($"{_settingsMqtt.TopicMain}/{_settingsMqtt.TopicMissionStatus}",
				JsonSerializer.Serialize(arg), MqttQualityOfServiceLevel.ExactlyOnce, true);
		}

		private async Task PressedKeys_OnControlModeChanged(MqttClasses.ControlMode arg)
		{
			MainViewModel.EventLogger?.LogMessage($"MQTT: Control Mode changed {arg}");
			await RoverCommunication_OnControlStatusChanged();
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

		public void Dispose()
		{
			//_eventsToDispose.ForEach(o => o.Dispose());
			//_cts.Cancel();
			//_mqttThread?.Join();
			//_mqttThread = null;
		}
	}
}
