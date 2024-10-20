using Godot;
using RoverControlApp.Core;
using System;
using System.Threading.Tasks;
using RoverControlApp.Core.RoverControllerPresets;
using RoverControlApp.Core.RoverControllerPresets.DriveControllers;
using RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;
using RoverControlApp.Core.RoverControllerPresets.SamplerControllers;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.MVVM.Model
{
	public class PressedKeys : IDisposable
	{
		private volatile ControlMode _controlMode;
		private Vector4 _lastAbsoluteVector;
		private RoverControl _roverMovement;
		private ManipulatorControl _manipulatorMovement;
		private SamplerControl _samplerControl = null!;
		private RoverContainer _containerMovement;
		private IRoverDriveController _roverDriveControllerPreset = null!;
		private IRoverManipulatorController _roverManipulatorControllerPreset = null!;
		private IRoverSamplerController _roverSamplerControllerPreset = null!;
		private bool _disposedValue;

		public event EventHandler<Vector4>? OnAbsoluteVectorChanged;
		public event Func<RoverControl, Task>? OnRoverMovementVector;
		public event Func<ManipulatorControl, Task>? OnManipulatorMovement;
		public event Func<SamplerControl, Task>? OnSamplerMovement;
		public event Func<RoverContainer, Task>? OnContainerMovement;
		public event Func<bool, Task>? OnPadConnectionChanged;
		public event Func<ControlMode, Task>? OnControlModeChanged;

		public ControlMode ControlMode
		{
			get => _controlMode;
			private set
			{
				_controlMode = value;
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Control Mode changed {value}");
				OnControlModeChanged?.Invoke(value);
			}
		}

		public static bool PadConnected => Input.GetConnectedJoypads().Count > 0;

		public Vector4 LastAbsoluteVector
		{
			get => _lastAbsoluteVector;
			private set
			{
				_lastAbsoluteVector = value;
				OnAbsoluteVectorChanged?.Invoke(this, value);
			}
		}

		public RoverControl RoverMovement
		{
			get => _roverMovement;
			private set
			{
				_roverMovement = value;
				OnRoverMovementVector?.Invoke(value);
			}
		}

		public ManipulatorControl ManipulatorMovement
		{
			get => _manipulatorMovement;
			private set
			{
				_manipulatorMovement = value;
				OnManipulatorMovement?.Invoke(value);
			}
		}

		public SamplerControl SamplerMovement
		{
			get => _samplerControl;
			private set
			{
				_samplerControl = value;
				OnSamplerMovement?.Invoke(value);
			}
		}

		public RoverContainer ContainerMovement
		{
			get => _containerMovement;
			private set
			{
				_containerMovement = value;
				OnContainerMovement?.Invoke(value);
			}
		}

		public PressedKeys()
		{
			Input.JoyConnectionChanged += InputOnJoyConnectionChanged;
			_lastAbsoluteVector = Vector4.Zero;
			_roverMovement = new();
			_manipulatorMovement = new();
			_samplerControl = new();
			_containerMovement = new();
			SetupControllerPresets();

			LocalSettings.Singleton.CategoryChanged += OnSettingsCategoryChanged;
			LocalSettings.Singleton.PropagatedPropertyChanged += OnSettingsPropertyChanged;
		}

		void SetupControllerPresets()
		{
			_manipulatorMovement = new();
			_roverDriveControllerPreset =
				RoverDriveControllerSelector.GetController(
					(RoverDriveControllerSelector.Controller)LocalSettings.Singleton.Joystick.RoverDriveController
				);
			_roverManipulatorControllerPreset = new SingleAxisManipulatorController();
			_roverSamplerControllerPreset = new SamplerController();
		}

		/*
		* Settings event handlers
		*/

		void OnSettingsCategoryChanged(StringName property)
		{
			if (property != nameof(LocalSettings.Joystick)) return;

			SetupControllerPresets();
		}

		void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
		{
			if(category != nameof(LocalSettings.Joystick)) return;

			switch (name)
			{
				case nameof(LocalSettings.Joystick.RoverDriveController):
					SetupControllerPresets();
					break;
			}
		}

		/*
		 * settings handlers end
		 */

		private void InputOnJoyConnectionChanged(long device, bool connected)
		{
			var status = connected ? "connected" : "disconnected";
			EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Pad {status}");
			OnPadConnectionChanged?.Invoke(PadConnected);
			StopAll();
		}

		public void HandleInputEvent(InputEvent _)
		{
			HandleFunctionInputEvent();
			HandleCameraInputEvent();
			HandleMovementInputEvent();
			HandleManipulatorInputEvent();
			HandleContainerInputEvent();
			HandleSamplerInputEvent();
		}

		private void HandleContainerInputEvent()
		{
			if (ControlMode != ControlMode.Manipulator) return;
			var axis = Input.GetAxis("camera_focus_out", "camera_focus_in");
			if (Mathf.IsEqualApprox(axis, ContainerMovement.Axis1, 0.01f)) return;
			ContainerMovement = new RoverContainer { Axis1 = axis };
		}

		private void HandleManipulatorInputEvent()
		{
			if (ControlMode != ControlMode.Manipulator) return;

			ManipulatorControl manipulatorControl = _roverManipulatorControllerPreset.CalculateMoveVector();
			if(_roverManipulatorControllerPreset.IsMoveVectorChanged(manipulatorControl, ManipulatorMovement))
				ManipulatorMovement = manipulatorControl;
		}
		private void HandleSamplerInputEvent()
		{
			if (ControlMode != ControlMode.Sampler) return;

			SamplerControl samplerControl = _roverSamplerControllerPreset.CalculateMoveVector(SamplerMovement);
			if (_roverSamplerControllerPreset.IsMoveVectorChanged(samplerControl, SamplerMovement))
				SamplerMovement = samplerControl;

		}

		private void HandleCameraInputEvent()
		{
			if (ControlMode == ControlMode.Manipulator) return;

			Vector4 absoluteVector4 = Vector4.Zero;

			Vector2 velocity = Input.GetVector("camera_move_left", "camera_move_right", "camera_move_down", "camera_move_up");
			velocity = velocity.Clamp(new Vector2(-1f, -1f), new Vector2(1f, 1f));
			absoluteVector4.X = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.Deadzone)) ? 0 : velocity.X;
			absoluteVector4.Y = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.Deadzone)) ? 0 : velocity.Y;
			velocity = Input.GetVector("camera_zoom_out", "camera_zoom_in", "camera_focus_out", "camera_focus_in");
			absoluteVector4.Z = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.Deadzone)) ? 0 : velocity.X;
			absoluteVector4.W = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, LocalSettings.Singleton.Joystick.Deadzone)) ? 0 : velocity.Y;

			if (Input.IsActionPressed("camera_zoom_mod"))
			{
				absoluteVector4.X /= 8f;
				absoluteVector4.Y /= 8f;
			}

			absoluteVector4 = absoluteVector4.Clamp(new Vector4(-1f, -1f, -1f, -1f), new Vector4(1f, 1f, 1f, 1f));
			LastAbsoluteVector = absoluteVector4;
		}

		private void HandleMovementInputEvent()
		{
			if (ControlMode != ControlMode.Rover) return;
			
			RoverControl roverControl = _roverDriveControllerPreset.CalculateMoveVector();
			if (_roverDriveControllerPreset.IsMoveVectorChanged(roverControl, RoverMovement))
				RoverMovement = roverControl;
		}
		private void HandleFunctionInputEvent()
		{
			HandleControlModeChange();
		}


		private void HandleControlModeChange()
		{
			if (!Input.IsActionJustPressed("ControlModeChange", true)) return;

			if ((int)ControlMode + 1 >= Enum.GetNames<ControlMode>().Length)
				ControlMode = ControlMode.EStop;
			else ControlMode++;

			StopAll();
		}

		private void StopAll()
		{
			EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, "Stopping all movement");
			RoverMovement = new RoverControl() { XVelAxis = 0, ZRotAxis = 0 };
			ContainerMovement = new RoverContainer { Axis1 = 0f };
			ManipulatorMovement = new ManipulatorControl();

			LastAbsoluteVector = Vector4.Zero;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposedValue)	return;

			if (disposing)
			{
				LocalSettings.Singleton.CategoryChanged -= OnSettingsCategoryChanged;
				LocalSettings.Singleton.PropagatedPropertyChanged -= OnSettingsPropertyChanged;
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
