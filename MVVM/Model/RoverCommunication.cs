using MQTTnet.Protocol;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;
using System;
using System.ServiceModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model
{
    public class RoverCommunication : IDisposable
	{
		public event Func<MqttClasses.RoverStatus?, Task>? OnRoverStatusChanged;
		private MqttClasses.ControlMode ControlMode => MainViewModel.PressedKeys?.ControlMode ?? MqttClasses.ControlMode.EStop;

		private MqttClient? _mqttClient => MainViewModel.MqttClient;

		//List<IDisposable> _eventsToDispose = new List<IDisposable>();
		private Settings.Mqtt _settingsMqtt;

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
					CommunicationState = MainViewModel.MqttClient?.ConnectionState ?? CommunicationState.Closed,
					ControlMode = ControlMode,
					PadConnected = MainViewModel.PressedKeys?.PadConnected ?? false
				};
				RoverStatus = obj;
				return obj;
			}
		}


		public RoverCommunication(Settings.Mqtt settingsMqtt)
		{
			_settingsMqtt = settingsMqtt;
			if (MainViewModel.PressedKeys != null)
				MainViewModel.PressedKeys.OnControlModeChanged += PressedKeys_OnControlModeChanged;
			if (MainViewModel.PressedKeys != null)
			{
				MainViewModel.PressedKeys.OnPadConnectionChanged += OnPadConnectionChanged;
				MainViewModel.PressedKeys.OnRoverMovementVector += RoverMovementVectorChanged;
				MainViewModel.PressedKeys.OnManipulatorMovement += RoverManipulatorVectorChanged;
				MainViewModel.PressedKeys.OnContainerMovement += PressedKeysOnOnContainerMovement;
			}

			if (MainViewModel.MissionStatus != null)
				MainViewModel.MissionStatus.OnRoverMissionStatusChanged += OnRoverMissionStatusChanged;

			if (_mqttClient != null)
				_mqttClient.OnConnectionChanged += OnMqttConnectionChanged;
			else EventLogger.LogMessage("RoverCommunication: Mqtt was null");

			if (_mqttClient?.ConnectionState == CommunicationState.Opened)
				RoverCommunication_OnControlStatusChanged(GenerateRoverStatus).Wait(250);
		}

		private async Task PressedKeysOnOnContainerMovement(MqttClasses.RoverContainer arg)
		{
			if (_mqttClient == null) return;
			await _mqttClient.EnqueueAsync(_settingsMqtt.TopicRoverContainer,
				JsonSerializer.Serialize(arg));
		}

		private async Task OnMqttConnectionChanged(CommunicationState? arg)
		{
			await RoverCommunication_OnControlStatusChanged(GenerateRoverStatus);
		}

		private async Task OnPadConnectionChanged(bool arg)
		{
			await RoverCommunication_OnControlStatusChanged(GenerateRoverStatus);
		}


		private async Task OnRoverMissionStatusChanged(MqttClasses.RoverMissionStatus? arg)
		{
			if (_mqttClient == null) return;
			await _mqttClient.EnqueueAsync(_settingsMqtt.TopicMissionStatus,
				JsonSerializer.Serialize(arg), MqttQualityOfServiceLevel.ExactlyOnce, true);
		}

		private async Task PressedKeys_OnControlModeChanged(MqttClasses.ControlMode arg)
		{
			await RoverCommunication_OnControlStatusChanged(GenerateRoverStatus);
		}

		private async Task RoverCommunication_OnControlStatusChanged(MqttClasses.RoverStatus roverStatus)
		{
			if (_mqttClient == null) return;
			await _mqttClient.EnqueueAsync(_settingsMqtt.TopicRoverStatus,
				JsonSerializer.Serialize(roverStatus), MqttQualityOfServiceLevel.ExactlyOnce, true);
		}

		private async Task RoverMovementVectorChanged(MqttClasses.RoverControl roverControl)
		{
			if (_mqttClient == null) return;
			await _mqttClient.EnqueueAsync(_settingsMqtt.TopicRoverControl,
				JsonSerializer.Serialize(roverControl));
		}
		private async Task RoverManipulatorVectorChanged(MqttClasses.ManipulatorControl manipulatorControl)
		{
			if (_mqttClient == null) return;
			await _mqttClient.EnqueueAsync(_settingsMqtt.TopicManipulatorControl,
				JsonSerializer.Serialize(manipulatorControl));
		}

		public void Dispose()
		{
			if (MainViewModel.PressedKeys != null)
				MainViewModel.PressedKeys.OnControlModeChanged -= PressedKeys_OnControlModeChanged;
			if (MainViewModel.PressedKeys != null)
			{
				MainViewModel.PressedKeys.OnPadConnectionChanged -= OnPadConnectionChanged;
				MainViewModel.PressedKeys.OnRoverMovementVector -= RoverMovementVectorChanged;
				MainViewModel.PressedKeys.OnManipulatorMovement -= RoverManipulatorVectorChanged;
			}

			if (MainViewModel.MissionStatus != null)
				MainViewModel.MissionStatus.OnRoverMissionStatusChanged -= OnRoverMissionStatusChanged;

			//_eventsToDispose.ForEach(o => o.Dispose());
			//_cts.Cancel();
			//_mqttThread?.Join();
			//_mqttThread = null;
		}
	}
}
