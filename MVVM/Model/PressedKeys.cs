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
	private ControlModeFlags _controlMode;
	private ControlModeFlags _masterControlMode;
	private ControlModeFlags _slaveControlMode;
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

	#endregion Fields

	#region Events

	public delegate void ControllerPresetChangedEventHandler();
	public delegate void LastAcceptedInputEventHandler(InputHelpHint.HintVisibility type);

	public event Action<Vector4>? CameraMoveVectorChanged;
	public event Func<RoverControl, Task>? OnRoverMovementVector;
	public event Func<ManipulatorControl, Task>? OnManipulatorMovement;
	public event Func<SamplerControl, Task>? OnSamplerMovement;
	public event Func<bool, Task>? OnPadConnectionChanged;
	public event Func<KinematicMode, Task>? OnKinematicModeChanged;
	public event ControllerPresetChangedEventHandler? ControllerPresetChanged;
	public event LastAcceptedInputEventHandler? LastAcceptedInput;

	public event Func<ControlModeFlags, Task>? OnMasterControlModeChanged;
	public event Func<ControlModeFlags, Task>? OnSlaveControlModeChanged;
	public event Func<ControlModeFlags, Task>? OnControlModeChanged;

	#endregion Events

	#region Properties

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public static PressedKeys Singleton { get; private set; }
#pragma warning restore CS8618

	public ControlModeFlags ControlMode
	{
		get => _controlMode;
		set
		{
			_controlMode = value;
			OnControlModeChanged?.Invoke(value);
		}
	} 

	public ControlModeFlags MasterControlMode
	{
		get => _masterControlMode;
		private set
		{
			_masterControlMode = value;
			ControlMode = CombineFlags(_masterControlMode, _slaveControlMode);
			EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Master Control Mode changed {value}");
			OnMasterControlModeChanged?.Invoke(value);
		}
	}

	public ControlModeFlags SlaveControlMode
	{
		get => _slaveControlMode;
		private set			
		{
			_slaveControlMode = value;
			ControlMode = CombineFlags(_masterControlMode, _slaveControlMode);
			EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Slave Control Mode changed {value}");
			OnSlaveControlModeChanged?.Invoke(value);
		}
	}

	public bool PadConnected => Input.GetConnectedJoypads().Count > 0;

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

		if (HandleInputEventAsMaster(@event))
			GetViewport().SetInputAsHandled();

		if (HandleInputEventAsSlave(@event))
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
		if (property != nameof(LocalSettings.Joystick) && property != nameof(LocalSettings.Manipulator)) return;

		SetupControllerPresets();
	}

	void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(LocalSettings.Joystick) && category != nameof(LocalSettings.Manipulator)) return;

		switch (name)
		{
			case nameof(LocalSettings.Joystick.RoverDriveController):
				SetupControllerPresets();
				break;
			case nameof(LocalSettings.Manipulator.RoverManipulatorController):
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
			!MasterControlMode.HasFlag(ControlModeFlags.EStop) // Not in EStop already
		)
		{
			MasterControlMode = ControlModeFlags.EStop;
			SlaveControlMode = ControlModeFlags.EStop;
			EventLogger.LogMessage(nameof(PressedKeys), EventLogger.LogLevel.Info, "Entered EStop (by Auto-EStop).");
			StopAll();
		}

		if (_roverModeControllerPreset.EstopReq())
		{
			_autoEstop_lastInput = Time.GetTicksMsec(); //or else will not vibrate when already in Auto E-Stop
			MasterControlMode = ControlModeFlags.EStop;
			SlaveControlMode = ControlModeFlags.EStop;
			EventLogger.LogMessage(nameof(PressedKeys), EventLogger.LogLevel.Info, "Entered EStop (by InputController).");
			StopAll();
		}
	}

	public bool HandleInputEventAsMaster(InputEvent inputEvent)
	{
		//GD.Print($"IsKB:{IsInputFromKeyboard(inputEvent)} IsMasterJoy:{_masterJoyConnected && IsInputFromController(inputEvent, _masterJoy)}");

		if (!IsInputFromKeyboard(inputEvent) && (!IsInputFromController(inputEvent, 0)))
		{
			return false;
		}


		if (_roverModeControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Master, _masterControlMode, out var newMasterMode))
		{
			MasterControlMode = newMasterMode;
			StopAll();
			OnAcceptedInput(inputEvent);
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) ControlMode");
			return true;
		}

		if (LocalSettings.Singleton.General.PedanticEstop && MasterControlMode.HasFlag(ControlModeFlags.EStop))
		{
			//print only if some controller is happy to take input
			bool isInputHandled =
				_roverCameraControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Master, _cameraMoveVector, out _);

			if (isInputHandled)
				EventLogger.LogMessage(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "PedanticEstop is enabled. Input rejected.");
			return false;
		}

		// rover control
		if(this.MasterControlMode.HasFlag(ControlModeFlags.Drive))
		{
			if (_roverDriveControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Master, _roverMovement, out _roverMovement))
			{
				OnKinematicModeChanged?.Invoke(_roverMovement.Mode);
				OnRoverMovementVector?.Invoke(_roverMovement);
				OnAcceptedInput(inputEvent);
				EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) RoverDrive");
				return true;
			}
		}

		if (this.MasterControlMode.HasFlag(ControlModeFlags.RoboticArm))
		{
			if (_roverManipulatorControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Master, _manipulatorMovement, out _manipulatorMovement))
			{
				OnManipulatorMovement?.Invoke(_manipulatorMovement);
				OnAcceptedInput(inputEvent);
				EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) RoverManipulator");
				return true;
			}
		}

		if (this.MasterControlMode.HasFlag(ControlModeFlags.DeepSampler) || this.MasterControlMode.HasFlag(ControlModeFlags.SurfaceSampler))
		{
			if (_roverSamplerControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Master, _samplerControl, out _samplerControl))
			{
				OnSamplerMovement?.Invoke(_samplerControl);
				OnAcceptedInput(inputEvent);
				EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) RoverSampler");
				return true;
			}
		}

		// camera control
		if (_roverCameraControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Master, _cameraMoveVector, out _cameraMoveVector))
		{
			CameraMoveVectorChanged?.Invoke(_cameraMoveVector);
			OnAcceptedInput(inputEvent);
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Master) Camera");
			return true;
		}
		
		return false;
	}

	public bool HandleInputEventAsSlave(InputEvent inputEvent)
	{
		if (!IsInputFromController(inputEvent, 1))
		{
			return false;
		}

		if (_masterControlMode.HasFlag(ControlModeFlags.EStop))
		{
			return false;
		}

		if (_roverModeControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Slave, _slaveControlMode, out var newSlaveMode))
		{
			SlaveControlMode = newSlaveMode;
			OnAcceptedInput(inputEvent);
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) ControlMode");
			return true;
		}

		if (LocalSettings.Singleton.General.PedanticEstop && _slaveControlMode.HasFlag(ControlModeFlags.EStop))
		{
			//print only if some controller is happy to take input
			bool isInputHandled =
				_roverCameraControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Slave, _cameraMoveVector, out _);

			if (isInputHandled)
				EventLogger.LogMessage(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "PedanticEstop is enabled. Input rejected.");
			return false;
		}

		// rover control
		if (_masterControlMode == _slaveControlMode)
		{
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input rejected (Slave). Master is in same mode.");
			return false;
		}

		if (_slaveControlMode.HasFlag(ControlModeFlags.Drive))
		{
			if (_roverDriveControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Slave, _roverMovement, out _roverMovement))
			{
				OnKinematicModeChanged?.Invoke(_roverMovement.Mode);
				OnRoverMovementVector?.Invoke(_roverMovement);
				OnAcceptedInput(inputEvent);
				EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) RoverDrive");
				return true;
			}
		}

		if (_slaveControlMode.HasFlag(ControlModeFlags.RoboticArm))
		{
			if (_roverManipulatorControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Slave, _manipulatorMovement, out _manipulatorMovement))
			{
				OnManipulatorMovement?.Invoke(_manipulatorMovement);
				OnAcceptedInput(inputEvent);
				EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) RoverManipulator");
				return true;
			}
		}

		if (_slaveControlMode.HasFlag(ControlModeFlags.DeepSampler) || _slaveControlMode.HasFlag(ControlModeFlags.SurfaceSampler))
		{
			if (_roverSamplerControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Slave, _samplerControl, out _samplerControl))
			{
				OnSamplerMovement?.Invoke(_samplerControl);
				OnAcceptedInput(inputEvent);
				EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) RoverSampler");
				return true;
			}
		}

		// camera control
		if (_roverCameraControllerPreset.HandleInput(inputEvent, DualSeatEvent.InputDevice.Slave, _cameraMoveVector, out _cameraMoveVector))
		{
			CameraMoveVectorChanged?.Invoke(_cameraMoveVector);
			OnAcceptedInput(inputEvent);
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, "Input handled as (Slave) Camera");
			return true;
		}
		
		return false;
	}

	public static bool IsInputFromController(InputEvent input, long device)
	{
		if (IsInputFromKeyboard(input))
		{
			return false;
		}

		return input.Device == device;
	}

	public static bool IsInputFromKeyboard(InputEvent input) 
	=> input.Device == InputEvent.DeviceIdKeyboard;


	#endregion Methods.HandleInput

	#region Methods

	private void SetupControllerPresets()
	{
		_roverModeControllerPreset = new StandardModeController();
		_roverDriveControllerPreset =
			RoverDriveControllerSelector.GetController(
				(RoverDriveControllerSelector.Controller)LocalSettings.Singleton.Joystick.RoverDriveController
			);
		_roverManipulatorControllerPreset =
			RoverManipulatorControllerSelector.GetController(
				(RoverManipulatorControllerSelector.Controller)LocalSettings.Singleton.Manipulator.RoverManipulatorController
			);
		_roverSamplerControllerPreset = new SamplerController();
		_roverCameraControllerPreset = new OriginalCameraController();

		ControllerPresetChanged?.Invoke();
	}

	private void InputOnJoyConnectionChanged(long device, bool connected)
	{

		switch (device)
		{
			case 0 when connected == true:
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller (0) connected as Master");
				break;
			case 0 when connected == false:
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller (0) disconnected - Master lost.");
				break;
			case 1 when connected == true:
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller (1) connected as Slave");
				break;
			case 1 when connected == false:
				EventLogger.LogMessage("PressedKeys", EventLogger.LogLevel.Info, $"Controller (1) disconnected - Slave lost.");
				break;
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

	public ControlModeFlags CombineFlags(ControlModeFlags masterControlMode, ControlModeFlags slaveControlMode)
	{
		slaveControlMode &= ~ControlModeFlags.EStop; // Slave cannot override Master EStop

		var temp = masterControlMode | slaveControlMode;

		if (ValidateRoverStatusControlMode(temp))
		{
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, $"ControlMode validation passed, master + slave: {temp}");
			return temp;
		}

		if (ValidateRoverStatusControlMode(masterControlMode)) // Slave may have goofed, but master can still be valid
		{
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, $"ControlMode validation passed, master only: {masterControlMode}");
			return masterControlMode;
		}

		masterControlMode &= ~ControlModeFlags.Stop; // Master may have goofed

		if (ValidateRoverStatusControlMode(masterControlMode))
		{
			EventLogger.LogMessageDebug(nameof(PressedKeys), EventLogger.LogLevel.Verbose, $"ControlMode validation passed, master only (Stop cleared): {masterControlMode}");
			return masterControlMode;
		}

		return ControlModeFlags.EStop; // If the developer has goofed, not much can be done. Shut it all down.
	}

	private static bool ValidateRoverStatusControlMode(ControlModeFlags controlMode)
	{
		static int HasIllegalCombination(ControlModeFlags mode, ControlModeFlags illegalMask, ControlModeFlags value)
		{
			return (int)(value & mode) != 0 ? (int)(value & illegalMask) : 0;
		}

		if (HasIllegalCombination(ControlModeFlags.EStop,
				ControlModeFlags.Stop |
				ControlModeFlags.Config |
				ControlModeFlags.Drive |
				ControlModeFlags.RoboticArm |
				ControlModeFlags.DeepSampler |
				ControlModeFlags.SurfaceSampler |
				ControlModeFlags.DriveAutonomy |
				ControlModeFlags.RoboticArmAutonomy |
				ControlModeFlags.DeepSamplerAutonomy |
				ControlModeFlags.SurfaceSamplerAutonomy, controlMode) != 0)
			return false;

		if (HasIllegalCombination(ControlModeFlags.Stop,
				ControlModeFlags.Config |
				ControlModeFlags.Drive |
				ControlModeFlags.DeepSampler |
				ControlModeFlags.SurfaceSampler |
				ControlModeFlags.DriveAutonomy |
				ControlModeFlags.DeepSamplerAutonomy |
				ControlModeFlags.SurfaceSamplerAutonomy, controlMode) != 0)
			return false;

		if (HasIllegalCombination(ControlModeFlags.Config,
				ControlModeFlags.Drive |
				ControlModeFlags.RoboticArm |
				ControlModeFlags.DeepSampler |
				ControlModeFlags.SurfaceSampler |
				ControlModeFlags.DriveAutonomy |
				ControlModeFlags.RoboticArmAutonomy |
				ControlModeFlags.DeepSamplerAutonomy |
				ControlModeFlags.SurfaceSamplerAutonomy, controlMode) != 0)
			return false;

		if (HasIllegalCombination(ControlModeFlags.Drive,
				ControlModeFlags.DeepSampler |
				ControlModeFlags.SurfaceSampler |
				ControlModeFlags.DriveAutonomy |
				ControlModeFlags.DeepSamplerAutonomy |
				ControlModeFlags.SurfaceSamplerAutonomy, controlMode) != 0)
			return false;

		if (HasIllegalCombination(ControlModeFlags.RoboticArm,
				ControlModeFlags.RoboticArmAutonomy, controlMode) != 0)
			return false;

		if (HasIllegalCombination(ControlModeFlags.DeepSampler,
				ControlModeFlags.DriveAutonomy |
				ControlModeFlags.DeepSamplerAutonomy |
				ControlModeFlags.SurfaceSamplerAutonomy, controlMode) != 0)
			return false;

		if (HasIllegalCombination(ControlModeFlags.SurfaceSampler,
				ControlModeFlags.DriveAutonomy |
				ControlModeFlags.DeepSamplerAutonomy |
				ControlModeFlags.SurfaceSamplerAutonomy, controlMode) != 0)
			return false;

		if (HasIllegalCombination(ControlModeFlags.DriveAutonomy,
				ControlModeFlags.DeepSamplerAutonomy |
				ControlModeFlags.SurfaceSamplerAutonomy, controlMode) != 0)
			return false;

		return true;
	}


	#endregion Methods
}
