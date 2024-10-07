using Godot;
using MQTTnet.Protocol;
using RoverControlApp.Core;
using System;
using System.ServiceModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model
{
	public class RoverCommunication : IDisposable
	{
		public event Func<MqttClasses.RoverStatus?, Task>? OnRoverStatusChanged;
		private MqttClasses.ControlMode ControlMode => _pressedKeys.ControlMode;

		private readonly PressedKeys _pressedKeys;
		private readonly MissionStatus _missionStatus;

		private MqttClasses.RoverStatus? _roverStatus;
		private bool _disposedValue = false;

		public MqttClasses.RoverStatus? RoverStatus
		{
			get => _roverStatus;
			private set
			{
				_roverStatus = value;
				OnRoverStatusChanged?.Invoke(value);
			}
		}

		private MqttClasses.RoverStatus GenerateRoverStatus(CommunicationState? connection = null, MqttClasses.ControlMode? controlMode = null, bool? padConnected = null)
		{
			var obj = new MqttClasses.RoverStatus
			{
				CommunicationState = connection ?? (RoverStatus is not null ? RoverStatus.CommunicationState : MqttNode.Singleton.ConnectionState),
				ControlMode = controlMode ?? ControlMode,
				PadConnected = padConnected ?? PressedKeys.PadConnected,
			};
			RoverStatus = obj;
			return obj;
		}


		public RoverCommunication(PressedKeys pressedKeys, MissionStatus missionStatus)
		{
			_pressedKeys = pressedKeys;
			_missionStatus = missionStatus;

			pressedKeys.OnControlModeChanged += PressedKeys_OnControlModeChanged;

			pressedKeys.OnPadConnectionChanged += OnPadConnectionChanged;
			pressedKeys.OnRoverMovementVector += RoverMovementVectorChanged;
			pressedKeys.OnManipulatorMovement += RoverManipulatorVectorChanged;
			pressedKeys.OnSamplerMovement += RoverSamplerVectorChanged;
			pressedKeys.OnContainerMovement += PressedKeysOnOnContainerMovement;

			missionStatus.OnRoverMissionStatusChanged += OnRoverMissionStatusChanged;

			MqttNode.Singleton.Connect(MqttNode.SignalName.ConnectionChanged, Callable.From<CommunicationState>(OnMqttConnectionChanged));

			if (MqttNode.Singleton.ConnectionState != CommunicationState.Opened)
				return;

			RoverCommunication_OnControlStatusChanged(GenerateRoverStatus()).Wait(250);
		}

		private async Task PressedKeysOnOnContainerMovement(MqttClasses.RoverContainer arg)
		{
			await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicRoverContainer,
				JsonSerializer.Serialize(arg));
		}

		private async void OnMqttConnectionChanged(CommunicationState arg)
		{
			await RoverMovementVectorChanged(_pressedKeys.RoverMovement);
			await RoverCommunication_OnControlStatusChanged(GenerateRoverStatus(connection: arg));
		}

		private async Task OnPadConnectionChanged(bool arg)
		{
			await RoverCommunication_OnControlStatusChanged(GenerateRoverStatus(padConnected: arg));
		}


		private async Task OnRoverMissionStatusChanged(MqttClasses.RoverMissionStatus? arg)
		{
			await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicMissionStatus,
				JsonSerializer.Serialize(arg), MqttQualityOfServiceLevel.ExactlyOnce, true);
		}

		private async Task PressedKeys_OnControlModeChanged(MqttClasses.ControlMode arg)
		{
			await RoverCommunication_OnControlStatusChanged(GenerateRoverStatus(controlMode: arg));
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
		private async Task RoverSamplerVectorChanged(MqttClasses.SamplerControl samplerControl)
		{
			await MqttNode.Singleton.EnqueueMessageAsync(LocalSettings.Singleton.Mqtt.TopicSamplerControl,
				JsonSerializer.Serialize(samplerControl));
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposedValue) return;

			if (disposing)
			{
				_pressedKeys.OnControlModeChanged -= PressedKeys_OnControlModeChanged;

				_pressedKeys.OnPadConnectionChanged -= OnPadConnectionChanged;
				_pressedKeys.OnRoverMovementVector -= RoverMovementVectorChanged;
				_pressedKeys.OnManipulatorMovement -= RoverManipulatorVectorChanged;

				_missionStatus.OnRoverMissionStatusChanged -= OnRoverMissionStatusChanged;
			}

			_disposedValue = true;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
