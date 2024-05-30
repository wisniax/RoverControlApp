using Godot;
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
		private MqttClasses.ControlMode ControlMode => pressedKeys.ControlMode;

		private readonly PressedKeys pressedKeys;
		private readonly MissionStatus missionStatus;

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
					CommunicationState = MqttNode.Singleton.ConnectionState,
					ControlMode = ControlMode,
					PadConnected = pressedKeys.PadConnected
				};
				RoverStatus = obj;
				return obj;
			}
		}


		public RoverCommunication(PressedKeys pressedKeys, MissionStatus missionStatus)
		{
			this.pressedKeys = pressedKeys;
			this.missionStatus = missionStatus;

			pressedKeys.OnControlModeChanged += PressedKeys_OnControlModeChanged;

			pressedKeys.OnPadConnectionChanged += OnPadConnectionChanged;
			pressedKeys.OnRoverMovementVector += RoverMovementVectorChanged;
			pressedKeys.OnManipulatorMovement += RoverManipulatorVectorChanged;
			pressedKeys.OnContainerMovement += PressedKeysOnOnContainerMovement;

			missionStatus.OnRoverMissionStatusChanged += OnRoverMissionStatusChanged;

			MqttNode.Singleton.Connect(MqttNode.SignalName.ConnectionChanged, Callable.From<CommunicationState>(OnMqttConnectionChanged));

			if (MqttNode.Singleton.ConnectionState == CommunicationState.Opened)
				RoverCommunication_OnControlStatusChanged(GenerateRoverStatus).Wait(250);
		}

		private async Task PressedKeysOnOnContainerMovement(MqttClasses.RoverContainer arg)
		{
			await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicRoverContainer,
				JsonSerializer.Serialize(arg));
		}

		private void OnMqttConnectionChanged(CommunicationState arg)
		{
			Task.Run( async () => await RoverCommunication_OnControlStatusChanged(GenerateRoverStatus) );
		}

		private async Task OnPadConnectionChanged(bool arg)
		{
			await RoverCommunication_OnControlStatusChanged(GenerateRoverStatus);
		}


		private async Task OnRoverMissionStatusChanged(MqttClasses.RoverMissionStatus? arg)
		{
			await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicMissionStatus,
				JsonSerializer.Serialize(arg), MqttQualityOfServiceLevel.ExactlyOnce, true);
		}

		private async Task PressedKeys_OnControlModeChanged(MqttClasses.ControlMode arg)
		{
			await RoverCommunication_OnControlStatusChanged(GenerateRoverStatus);
		}

		private async Task RoverCommunication_OnControlStatusChanged(MqttClasses.RoverStatus roverStatus)
		{
			await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicRoverStatus,
				JsonSerializer.Serialize(roverStatus), MqttQualityOfServiceLevel.ExactlyOnce, true);
		}

		private async Task RoverMovementVectorChanged(MqttClasses.RoverControl roverControl)
		{
			await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicRoverControl,
				JsonSerializer.Serialize(roverControl));
		}
		private async Task RoverManipulatorVectorChanged(MqttClasses.ManipulatorControl manipulatorControl)
		{
			await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicManipulatorControl,
				JsonSerializer.Serialize(manipulatorControl));
		}

		public void Dispose()
		{
			pressedKeys.OnControlModeChanged -= PressedKeys_OnControlModeChanged;

			pressedKeys.OnPadConnectionChanged -= OnPadConnectionChanged;
			pressedKeys.OnRoverMovementVector -= RoverMovementVectorChanged;
			pressedKeys.OnManipulatorMovement -= RoverManipulatorVectorChanged;

			missionStatus.OnRoverMissionStatusChanged -= OnRoverMissionStatusChanged;

			//_eventsToDispose.ForEach(o => o.Dispose());
			//_cts.Cancel();
			//_mqttThread?.Join();
			//_mqttThread = null;
		}
	}
}
