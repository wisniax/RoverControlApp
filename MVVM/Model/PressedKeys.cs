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
	private Vector4 _cameraMoveVector;
	private RoverControl _roverMovement;
	private ManipulatorControl _manipulatorMovement;
	private SamplerControl _samplerControl = null!;
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
			_roverMovement = value;
				OnKinematicModeChanged?.Invoke(value.Mode);
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

	#endregion Properties

	#region Ctor

	public PressedKeys()
	{
		Input.JoyConnectionChanged += InputOnJoyConnectionChanged;
		_cameraMoveVector = Vector4.Zero;
		_roverMovement = new();
		_manipulatorMovement = new();
		_samplerControl = new();
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
				if (_roverDriveControllerPreset.HandleInput(inputEvent, _roverMovement, out _roverMovement))
				{
					OnKinematicModeChanged?.Invoke(_roverMovement.Mode);
					OnRoverMovementVector?.Invoke(_roverMovement);
					return true;
				}
				break;
			case ControlMode.Manipulator:
				if (_roverManipulatorControllerPreset.HandleInput(inputEvent, _manipulatorMovement, out _manipulatorMovement))
				{
					OnManipulatorMovement?.Invoke(_manipulatorMovement);
					return true;
				}
				break;
			case ControlMode.Sampler:
				if (_roverSamplerControllerPreset.HandleInput(inputEvent, _samplerControl, out _samplerControl))
				{
					OnSamplerMovement?.Invoke(_samplerControl);
					return true;
				}
				break;
		}


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

	private void StopAll()
	{
		EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, "Stopping all movement");
		RoverMovement = new RoverControl() { Vel = 0, XAxis = 0, YAxis = 0, Mode = KinematicMode.Ackermann };
		ManipulatorMovement = new ManipulatorControl();
		SamplerMovement = new SamplerControl();

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
