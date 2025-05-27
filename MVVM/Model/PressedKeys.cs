using System;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.Core.RoverControllerPresets;
using RoverControlApp.Core.RoverControllerPresets.CameraControllers;
using RoverControlApp.Core.RoverControllerPresets.ControlModeControllers;
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
	private IControlModeController _roverModeControllerPreset = null!;
	private IRoverDriveController _roverDriveControllerPreset = null!;
	private IRoverManipulatorController _roverManipulatorControllerPreset = null!;
	private IRoverSamplerController _roverSamplerControllerPreset = null!;
	private ICameraController _roverCameraControllerPreset = null!;
	private bool _disposedValue;

	#endregion Fields

	#region Events

	public delegate void ControllerPresetChangedEventHandler();
	public delegate void LastAcceptedInputEventHandler(InputHelpHint.HintVisibility type);

	public event Action<Vector4>? CameraMoveVectorChanged;
	public event Func<RoverControl, Task>? OnRoverMovementVector;
	public event Func<ManipulatorControl, Task>? OnManipulatorMovement;
	public event Func<SamplerControl, Task>? OnSamplerMovement;
	public event Func<bool, Task>? OnPadConnectionChanged;
	public event Func<ControlMode, Task>? OnControlModeChanged;
	public event Func<KinematicMode, Task>? OnKinematicModeChanged;
	public event ControllerPresetChangedEventHandler? ControllerPresetChanged;
	public event LastAcceptedInputEventHandler? LastAcceptedInput;

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

	public IControlModeController RoverModeControllerPreset => _roverModeControllerPreset;
	public IRoverDriveController RoverDriveControllerPreset => _roverDriveControllerPreset;
	public IRoverManipulatorController RoverManipulatorControllerPreset => _roverManipulatorControllerPreset;
	public IRoverSamplerController RoverSamplerControllerPreset => _roverSamplerControllerPreset;
	public ICameraController RoverCameraControllerPreset => _roverCameraControllerPreset;

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

	#region Methods.HandleInput

	public void HandleEstop()
	{		
		if (_roverModeControllerPreset.EstopReq())
		{
			ControlMode = ControlMode.EStop;
			EventLogger.LogMessage(nameof(PressedKeys), EventLogger.LogLevel.Info, "Entered EStop (by InputController).");
		}
	}

	public bool HandleInputEvent(InputEvent inputEvent)
	{
		if (_roverModeControllerPreset.HandleInput(inputEvent, _controlMode, out _controlMode))
		{
			OnControlModeChanged?.Invoke(_controlMode);
			StopAll();
			OnAcceptedInput(inputEvent);
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as ControlMode");
			return true;
		}

		// camera control
		switch (this.ControlMode)
		{
			case ControlMode.EStop:
			case ControlMode.Rover:
			case ControlMode.Manipulator: // was disabled originally
			case ControlMode.Sampler:
			case ControlMode.Autonomy:
				if (_roverCameraControllerPreset.HandleInput(inputEvent, _cameraMoveVector, out _cameraMoveVector))
				{
					CameraMoveVectorChanged?.Invoke(_cameraMoveVector);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as Camera");
					return true;
				}
				break;
		}

		// rover control
		switch (this.ControlMode)
		{
			case ControlMode.Rover:
				if (_roverDriveControllerPreset.HandleInput(inputEvent, _roverMovement, out _roverMovement))
				{
					OnKinematicModeChanged?.Invoke(_roverMovement.Mode);
					OnRoverMovementVector?.Invoke(_roverMovement);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as RoverDrive");
					return true;
				}
				break;
			case ControlMode.Manipulator:
				if (_roverManipulatorControllerPreset.HandleInput(inputEvent, _manipulatorMovement, out _manipulatorMovement))
				{
					OnManipulatorMovement?.Invoke(_manipulatorMovement);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as RoverManipulator");
					return true;
				}
				break;
			case ControlMode.Sampler:
				if (_roverSamplerControllerPreset.HandleInput(inputEvent, _samplerControl, out _samplerControl))
				{
					OnSamplerMovement?.Invoke(_samplerControl);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as RoverSampler");
					return true;
				}
				break;
		}

		return false;
	}

	#endregion Methods.HandleInput

	#region Methods

	private void SetupControllerPresets()
	{
		_roverModeControllerPreset = new StandardModeController();
		_roverDriveControllerPreset =
			RoverDriveControllerSelector.GetController(
				(RoverDriveControllerSelector.Controller)LocalSettings.Singleton.Joystick.RoverDriveController
			);
		_roverManipulatorControllerPreset = new SingleAxisManipulatorController();
		_roverSamplerControllerPreset = new SamplerController();
		_roverCameraControllerPreset = new OriginalCameraController();

		ControllerPresetChanged?.Invoke();
	}

	private void InputOnJoyConnectionChanged(long device, bool connected)
	{
		var status = connected ? "connected" : "disconnected";
		EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Pad {status}");
		OnPadConnectionChanged?.Invoke(PadConnected);
		StopAll();
	}

	private void StopAll()
	{
		EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, "Stopping all movement");
		RoverMovement = new RoverControl() { Vel = 0, XAxis = 0, YAxis = 0, Mode = KinematicMode.Ackermann };
		ManipulatorMovement = new ManipulatorControl();
		SamplerMovement = new SamplerControl();

		CameraMoveVector = Vector4.Zero;
	}

	private void OnAcceptedInput(InputEvent inputEvent)
	{
		bool inputIsJoystick = inputEvent is InputEventJoypadButton or InputEventJoypadButton;
		LastAcceptedInput?.Invoke(inputIsJoystick ? InputHelpHint.HintVisibility.Joy : InputHelpHint.HintVisibility.Kb);
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
