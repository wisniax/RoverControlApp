using System;
using System.ServiceModel;
using System.Text.Json;
using System.Threading.Tasks;

using Godot;

using MQTTnet.Protocol;

using RoverControlApp.Core;

namespace RoverControlApp.MVVM.Model
{
	public partial class RoverCommunication : Node
	{
		public event Func<MqttClasses.RoverStatus?, Task>? OnRoverStatusChanged;
		private MqttClasses.ControlMode ControlMode => PressedKeys.Singleton.ControlMode;

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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public static RoverCommunication Singleton { get; private set; }
#pragma warning restore CS8618

        /*
		*	Godot overrides
		*/

        public override void _Ready()
        {
            base._Ready();
			PressedKeys.Singleton.OnControlModeChanged += PressedKeys_OnControlModeChanged;

			PressedKeys.Singleton.OnPadConnectionChanged += OnPadConnectionChanged;
			PressedKeys.Singleton.OnRoverMovementVector += RoverMovementVectorChanged;
			PressedKeys.Singleton.OnManipulatorMovement += RoverManipulatorVectorChanged;
			PressedKeys.Singleton.OnSamplerMovement += RoverSamplerVectorChanged;

			MissionStatus.Singleton.OnRoverMissionStatusChanged += OnRoverMissionStatusChanged;

			MqttNode.Singleton.Connect(MqttNode.SignalName.ConnectionChanged, Callable.From<CommunicationState>(OnMqttConnectionChanged));

			Singleton ??= this;

			if (MqttNode.Singleton.ConnectionState != CommunicationState.Opened)
				return;

			RoverCommunication_OnControlStatusChanged(GenerateRoverStatus()).Wait(250);
        }

		protected override void Dispose(bool disposing)
		{
			if (_disposedValue) return;

			if (disposing)
			{
				PressedKeys.Singleton.OnControlModeChanged -= PressedKeys_OnControlModeChanged;

				PressedKeys.Singleton.OnPadConnectionChanged -= OnPadConnectionChanged;
				PressedKeys.Singleton.OnRoverMovementVector -= RoverMovementVectorChanged;
				PressedKeys.Singleton.OnManipulatorMovement -= RoverManipulatorVectorChanged;

				MissionStatus.Singleton.OnRoverMissionStatusChanged -= OnRoverMissionStatusChanged;
				Singleton = null!;
			}

			_disposedValue = true;
			base.Dispose(disposing);
		}

		/*
		*	Godot overrides end
		*/

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

		private async void OnMqttConnectionChanged(CommunicationState arg)
		{
			//this needs to be called or else no updates.
			GenerateRoverStatus(connection: arg);
			switch (arg)
			{
				case CommunicationState.Opened:
				case CommunicationState.Faulted:
					break; // continue as normal
				default:
					return; // poker face
			}
			await RoverMovementVectorChanged(PressedKeys.Singleton.RoverMovement);
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
	}
}
