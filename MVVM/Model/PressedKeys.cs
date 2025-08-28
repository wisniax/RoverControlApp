using System;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.Core.RoverControllerPresets;
using RoverControlApp.Core.RoverControllerPresets.CameraControllers;
using RoverControlApp.Core.RoverControllerPresets.ControlModeControllers;
using RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;
using RoverControlApp.Core.RoverControllerPresets.SamplerControllers;
using RoverControlApp.MVVM.ViewModel;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.MVVM.Model;

public partial class PressedKeys : Node
{
	#region Fields
	private ControlMode _controlMode;
	private ControlMode _slaveControlMode;
	private Vector4 _cameraMoveVector;
	private RoverControl _roverMovement;
	private ManipulatorControl _manipulatorMovement;
	private SamplerControl _samplerControl = null!;
	private IControlModeController _roverModeControllerPreset = null!;
	private IRoverDriveController _roverDriveControllerPreset = null!;
	private IRoverManipulatorController _roverManipulatorControllerPreset = null!;
	private IRoverSamplerController _roverSamplerControllerPreset = null!;
	private ICameraController _roverCameraControllerPreset = null!;
	private ulong _autoEstop_lastInput = 0;

	private bool _masterJoyConnected = false;
	private long _masterJoy = -1;
	private bool _slaveJoyConnected = false;
	private long _slaveJoy = -1;

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

	public event Func<ControlMode, Task>? OnSlaveControlModeChanged;

	#endregion Events

	#region Properties

	#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public static PressedKeys Singleton { get; private set; }
	#pragma warning restore CS8618

	public ControlMode ControlMode
	{
		get => _controlMode;
		private set
		{
			_controlMode = value;
			EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Master Control Mode changed {value}");
			OnControlModeChanged?.Invoke(value);
		}
	}

	public ControlMode SlaveControlMode
	{
		get => _slaveControlMode;
		private set
		{
			_slaveControlMode = value;
			EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Slave Control Mode changed {value}");
			OnSlaveControlModeChanged?.Invoke(value);
		}
	}

	public bool PadConnected => _masterJoyConnected;

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

	public long MasterJoyDevice { get => _masterJoy; }
	public long SlaveJoyDevice { get => _slaveJoy; }

	public IControlModeController RoverModeControllerPreset => _roverModeControllerPreset;
	public IRoverDriveController RoverDriveControllerPreset => _roverDriveControllerPreset;
	public IRoverManipulatorController RoverManipulatorControllerPreset => _roverManipulatorControllerPreset;
	public IRoverSamplerController RoverSamplerControllerPreset => _roverSamplerControllerPreset;
	public ICameraController RoverCameraControllerPreset => _roverCameraControllerPreset;

	/// <summary>
	/// Time left to Auto-EStop.
	/// 0 means Auto-EStop inactive.
	/// </summary>
	public long TimeToAutoEStopMsec
	{
		get
		{
			if (LocalSettings.Singleton.General.NoInputSecondsToEstop == 0)
				return 0;
			var lastInput = Time.GetTicksMsec() - _autoEstop_lastInput;
			return (long)LocalSettings.Singleton.General.NoInputMsecToEstop - (long)lastInput;
		}
	}

	#endregion Properties

	#region Ctor

	public PressedKeys()
	{
		_cameraMoveVector = Vector4.Zero;
		_roverMovement = new();
		_manipulatorMovement = new();
		_samplerControl = new();
	}

	#endregion Ctor

	#region GodotOverride

	public override void _Ready()
	{
		base._Ready();
		Singleton ??= this;

		Input.JoyConnectionChanged += InputOnJoyConnectionChanged;
		LocalSettings.Singleton.CategoryChanged += OnSettingsCategoryChanged;
		LocalSettings.Singleton.PropagatedPropertyChanged += OnSettingsPropertyChanged;

		_cameraMoveVector = Vector4.Zero;
		_roverMovement = new();
		_manipulatorMovement = new();
		_samplerControl = new();
		SetupControllerPresets();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not (InputEventKey or InputEventJoypadButton or InputEventJoypadMotion))
			return;

		if (HandleInputEvent(@event))
			GetViewport().SetInputAsHandled();

		if (HandleSlaveInputEvent(@event))
			GetViewport().SetInputAsHandled();
	}

	public override void _Process(double delta)
	{
		HandleEstop();
	}

	public override void _ExitTree()
	{
		LocalSettings.Singleton.CategoryChanged -= OnSettingsCategoryChanged;
		LocalSettings.Singleton.PropagatedPropertyChanged -= OnSettingsPropertyChanged;
		Singleton = null!;
	}

	#endregion GodotOverride

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
		var lastInput = Time.GetTicksMsec() - _autoEstop_lastInput;

		if (
			LocalSettings.Singleton.General.NoInputSecondsToEstop > 0 && // Must be enabled
			lastInput > LocalSettings.Singleton.General.NoInputMsecToEstop && // Last input longer than expected
			ControlMode != ControlMode.EStop // Not in EStop already
		)
		{
			ControlMode = ControlMode.EStop;
			EventLogger.LogMessage(nameof(PressedKeys), EventLogger.LogLevel.Info, "Entered EStop (by Auto-EStop).");
			StopAll();
		}

		if (_roverModeControllerPreset.EstopReq())
		{
			_autoEstop_lastInput = Time.GetTicksMsec(); //or else will not vibrate when already in Auto E-Stop
			ControlMode = ControlMode.EStop;
			SlaveControlMode = ControlMode.EStop;
			EventLogger.LogMessage(nameof(PressedKeys), EventLogger.LogLevel.Info, "Entered EStop (by InputController).");
			StopAll();
		}
	}

	public bool HandleInputEvent(InputEvent inputEvent)
	{
		//GD.Print($"IsKB:{IsInputFromKeyboard(inputEvent)} IsMasterJoy:{_masterJoyConnected && IsInputFromController(inputEvent, _masterJoy)}");

		if (!IsInputFromKeyboard(inputEvent) && (!_masterJoyConnected || !IsInputFromController(inputEvent, _masterJoy)))
		{
			return false;
		}


		if (_roverModeControllerPreset.HandleInput(inputEvent, _controlMode, out _controlMode))
		{
			OnControlModeChanged?.Invoke(_controlMode);
			StopAll();
			OnAcceptedInput(inputEvent);
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) ControlMode");
			return true;
		}

		if (LocalSettings.Singleton.General.PedanticEstop && ControlMode == ControlMode.EStop)
		{
			//print only if some controller is happy to take input
			bool isInputHandled =
				_roverCameraControllerPreset.HandleInput(inputEvent, _cameraMoveVector, out _);

			if(isInputHandled)
				EventLogger.LogMessage(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "PedanticEstop is enabled. Input rejected.");
			return false;
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
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) RoverDrive");
					return true;
				}
				break;
			case ControlMode.Manipulator:
				if (_roverManipulatorControllerPreset.HandleInput(inputEvent, _manipulatorMovement, out _manipulatorMovement))
				{
					OnManipulatorMovement?.Invoke(_manipulatorMovement);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) RoverManipulator");
					return true;
				}
				break;
			case ControlMode.Sampler:
				if (_roverSamplerControllerPreset.HandleInput(inputEvent, _samplerControl, out _samplerControl))
				{
					OnSamplerMovement?.Invoke(_samplerControl);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) RoverSampler");
					return true;
				}
				break;
		}

		// camera control
		switch (this.ControlMode)
		{
			case ControlMode.EStop:
			case ControlMode.Rover:
			case ControlMode.Manipulator: // was disabled originally
			case ControlMode.Sampler:
			case ControlMode.Autonomy:
			default:
				if (_roverCameraControllerPreset.HandleInput(inputEvent, _cameraMoveVector, out _cameraMoveVector))
				{
					CameraMoveVectorChanged?.Invoke(_cameraMoveVector);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) Camera");
					return true;
				}
				break;
		}

		return false;
	}

	public bool HandleSlaveInputEvent(InputEvent inputEvent)
	{
		if (!_slaveJoyConnected || !IsInputFromController(inputEvent, _slaveJoy))
		{
			return false;
		}

		if (_controlMode == ControlMode.EStop)
		{
			return false;
		}

		if (_roverModeControllerPreset.HandleInput(inputEvent, _slaveControlMode, out _slaveControlMode))
		{
			OnSlaveControlModeChanged?.Invoke(_slaveControlMode);
			StopAll();
			OnAcceptedInput(inputEvent);
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) ControlMode");
			return true;
		}

		if (LocalSettings.Singleton.General.PedanticEstop && _slaveControlMode == ControlMode.EStop)
		{
			//print only if some controller is happy to take input
			bool isInputHandled =
				_roverCameraControllerPreset.HandleInput(inputEvent, _cameraMoveVector, out _);

			if(isInputHandled)
				EventLogger.LogMessage(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "PedanticEstop is enabled. Input rejected.");
			return false;
		}

		// rover control
		if (_controlMode == _slaveControlMode)
		{
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input rejected (Slave). Master is in same mode.");
			return false;
		}

		switch (_slaveControlMode)
		{
			case ControlMode.Rover:
				if (_roverDriveControllerPreset.HandleInput(inputEvent, _roverMovement, out _roverMovement))
				{
					OnKinematicModeChanged?.Invoke(_roverMovement.Mode);
					OnRoverMovementVector?.Invoke(_roverMovement);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) RoverDrive");
					return true;
				}
				break;
			case ControlMode.Manipulator:
				if (_roverManipulatorControllerPreset.HandleInput(inputEvent, _manipulatorMovement, out _manipulatorMovement))
				{
					OnManipulatorMovement?.Invoke(_manipulatorMovement);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) RoverManipulator");
					return true;
				}
				break;
			case ControlMode.Sampler:
				if (_roverSamplerControllerPreset.HandleInput(inputEvent, _samplerControl, out _samplerControl))
				{
					OnSamplerMovement?.Invoke(_samplerControl);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) RoverSampler");
					return true;
				}
				break;
		}

		// camera control
		switch (_slaveControlMode)
		{
			case ControlMode.EStop:
			case ControlMode.Rover:
			case ControlMode.Manipulator: // was disabled originally
			case ControlMode.Sampler:
			case ControlMode.Autonomy:
			default:
				if (_roverCameraControllerPreset.HandleInput(inputEvent, _cameraMoveVector, out _cameraMoveVector))
				{
					CameraMoveVectorChanged?.Invoke(_cameraMoveVector);
					OnAcceptedInput(inputEvent);
					EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) Camera");
					return true;
				}
				break;
		}

		return false;
	}

	public static bool IsInputFromController(InputEvent input, long device)
	{
		if (input is not InputEventJoypadButton && input is not InputEventJoypadMotion)
		{
			return false;
		}

		return input.Device == device;
	}

	public static bool IsInputFromKeyboard(InputEvent input)
	{
		if (input is InputEventJoypadButton || input is InputEventJoypadMotion)
		{
			return false;
		}

		return input.Device == 0;
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
		if (connected)
		{
			if (!_masterJoyConnected)
			{
				_masterJoy = device;
				_masterJoyConnected = true;
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller ({device}) connected as Master");
			}
			else if (_masterJoyConnected && !_slaveJoyConnected)
			{
				_slaveJoy = device;
				_slaveJoyConnected = true;
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller ({device}) connected as Slave");
			}
		}
		else
		{
			EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller ({device}) disconnected.");
			if (_masterJoy == device && _slaveJoyConnected)
			{
				_masterJoy = _slaveJoy;
				_slaveJoy = -1;
				SlaveControlMode = ControlMode.EStop;
				_slaveJoyConnected = false;
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller ({_masterJoy}) promoted to Master");
			}
			else if (_masterJoy == device && !_slaveJoyConnected)
			{
				_masterJoy = -1;
				_masterJoyConnected = false;
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller ({device}) - Master lost.");
			}
			else if (_slaveJoy == device)
			{
				_slaveJoy = -1;
				_slaveJoyConnected = false;
				SlaveControlMode = ControlMode.EStop;
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller ({device}) - Slave lost.");
			}
		}

		OnPadConnectionChanged?.Invoke(PadConnected);
		StopAll();
	}

	private void StopAll()
	{
		EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Verbose, "Stopping all movement");
		RoverMovement = new RoverControl() { Vel = 0, XAxis = 0, YAxis = 0, Mode = KinematicMode.Ackermann };
		ManipulatorMovement = new ManipulatorControl();
		SamplerMovement = new SamplerControl();

		CameraMoveVector = Vector4.Zero;
	}

	private void OnAcceptedInput(InputEvent inputEvent)
	{
		_autoEstop_lastInput = Time.GetTicksMsec();
		bool inputIsJoystick = inputEvent is InputEventJoypadButton or InputEventJoypadMotion;
		LastAcceptedInput?.Invoke(inputIsJoystick ? InputHelpHint.HintVisibility.Joy : InputHelpHint.HintVisibility.Kb);
	}

	#endregion Methods
}
