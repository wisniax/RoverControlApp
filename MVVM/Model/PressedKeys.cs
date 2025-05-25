using System;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.Core.RoverControllerPresets;
using RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;
using RoverControlApp.Core.RoverControllerPresets.SamplerControllers;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.MVVM.Model;

public class PressedKeys : IDisposable
{
	#region Fields
	private ControlMode _controlMode;
	private KinematicMode _kinematicMode;
	private Vector4 _cameraMoveVector;
	private RoverControl _roverMovement;
	private ManipulatorControl _manipulatorMovement;
	private SamplerControl _samplerControl = null!;
	private RoverContainer _containerMovement;
	private IRoverDriveController _roverDriveControllerPreset = null!;
	private IRoverManipulatorController _roverManipulatorControllerPreset = null!;
	private IRoverSamplerController _roverSamplerControllerPreset = null!;
	private bool _disposedValue;

	#endregion Fields

	#region Events
	public event Action<Vector4>? CameraMoveVectorChanged;
	public event Func<RoverControl, Task>? OnRoverMovementVector;
	public event Func<ManipulatorControl, Task>? OnManipulatorMovement;
	public event Func<SamplerControl, Task>? OnSamplerMovement;
	public event Func<RoverContainer, Task>? OnContainerMovement;
	public event Func<bool, Task>? OnPadConnectionChanged;
	public event Func<ControlMode, Task>? OnControlModeChanged;
	public event Func<KinematicMode, Task>? OnKinematicModeChanged;

	#endregion Events

	#region Properties

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

	public Vector4 CameraMoveVector
	{
		get => _cameraMoveVector;
		private set
		{
			_cameraMoveVector = value;
			CameraMoveVectorChanged?.Invoke(value);
		}
	}

	public RoverControl RoverMovement
	{
		get => _roverMovement;
		private set
		{
			if (_roverDriveControllerPreset.IsMoveVectorChanged(value, _roverMovement))
			{
				OnKinematicModeChanged?.Invoke(value.Mode);
				OnRoverMovementVector?.Invoke(value);
				_roverMovement = value;
			}
			else if (_roverDriveControllerPreset.IsKinematicModeChanged(value.Mode, _roverMovement.Mode))
			{
				OnKinematicModeChanged?.Invoke(value.Mode);
				_roverMovement.Mode = value.Mode;
			}
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

	#endregion Properties

	#region Ctor

	public PressedKeys()
	{
		Input.JoyConnectionChanged += InputOnJoyConnectionChanged;
		_cameraMoveVector = Vector4.Zero;
		_roverMovement = new();
		_manipulatorMovement = new();
		_samplerControl = new();
		_containerMovement = new();
		SetupControllerPresets();

		LocalSettings.Singleton.CategoryChanged += OnSettingsCategoryChanged;
		LocalSettings.Singleton.PropagatedPropertyChanged += OnSettingsPropertyChanged;
	}

	#endregion Ctor

	#region Methods.Settings

	void OnSettingsCategoryChanged(StringName property)
	{
		if (property != nameof(LocalSettings.Joystick)) return;

		SetupControllerPresets();
	}

	void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(LocalSettings.Joystick)) return;

		switch (name)
		{
			case nameof(LocalSettings.Joystick.RoverDriveController):
				SetupControllerPresets();
				break;
		}
	}

	#endregion Methods.Settings

	#region Methods

	private void SetupControllerPresets()
	{
		_manipulatorMovement = new();
		_roverDriveControllerPreset =
			RoverDriveControllerSelector.GetController(
				(RoverDriveControllerSelector.Controller)LocalSettings.Singleton.Joystick.RoverDriveController
			);
		_roverManipulatorControllerPreset = new SingleAxisManipulatorController();
		_roverSamplerControllerPreset = new SamplerController();
	}

	private void InputOnJoyConnectionChanged(long device, bool connected)
	{
		var status = connected ? "connected" : "disconnected";
		EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Pad {status}");
		OnPadConnectionChanged?.Invoke(PadConnected);
		StopAll();
	}

	public bool HandleInputEvent(InputEvent inputEvent)
	{
		if (HandleInput_ControlMode(inputEvent))
			return true;
				
		HandleCameraInputEvent();

		switch (this.ControlMode)
		{
			case ControlMode.Rover:
				RoverMovement = _roverDriveControllerPreset.CalculateMoveVector(inputEvent, RoverMovement);
				return true;
		}

		HandleManipulatorInputEvent();
		HandleContainerInputEvent();
		HandleSamplerInputEvent();

		return true;
	}

	private bool HandleInput_ControlMode(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("ControlModeChange", exactMatch: true))
		{
			if ((int)ControlMode + 1 >= Enum.GetNames<ControlMode>().Length)
				ControlMode = ControlMode.EStop;
			else
				ControlMode++;
			StopAll();
			return true;
		}

		return false;
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

		absoluteVector4 = absoluteVector4.Clamp(new Vector4(-1f, -1f, -1f, -1f), new Vector4(1f, 1f, 1f, 1f));
		CameraMoveVector = absoluteVector4;
	}



	private void HandleManipulatorInputEvent()
	{
		if (ControlMode != ControlMode.Manipulator) return;

		ManipulatorControl manipulatorControl = _roverManipulatorControllerPreset.CalculateMoveVector();
		if (_roverManipulatorControllerPreset.IsMoveVectorChanged(manipulatorControl, ManipulatorMovement))
			ManipulatorMovement = manipulatorControl;
	}

	private void HandleSamplerInputEvent()
	{
		if (ControlMode != ControlMode.Sampler) return;

		SamplerControl samplerControl = _roverSamplerControllerPreset.CalculateMoveVector(SamplerMovement);
		if (_roverSamplerControllerPreset.IsMoveVectorChanged(samplerControl, SamplerMovement))
			SamplerMovement = samplerControl;

	}

	private void HandleContainerInputEvent()
	{
		if (ControlMode != ControlMode.Manipulator) return;
		var axis = Input.GetAxis("camera_focus_out", "camera_focus_in");
		if (Mathf.IsEqualApprox(axis, ContainerMovement.Axis1, 0.01f)) return;
		ContainerMovement = new RoverContainer { Axis1 = axis };
	}


	private void StopAll()
	{
		EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, "Stopping all movement");
		RoverMovement = new RoverControl() { Vel = 0, XAxis = 0, YAxis = 0, Mode = _kinematicMode };
		ContainerMovement = new RoverContainer { Axis1 = 0f };
		ManipulatorMovement = new ManipulatorControl();

		CameraMoveVector = Vector4.Zero;
	}

	#endregion Methods

	#region IDisposable

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue) return;

		if (disposing)
		{
			LocalSettings.Singleton.CategoryChanged -= OnSettingsCategoryChanged;
			LocalSettings.Singleton.PropagatedPropertyChanged -= OnSettingsPropertyChanged;
		}

		_disposedValue = true;
	}

	#endregion IDisposable
}
