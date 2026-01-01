using System;
using System.Collections.Generic;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

public class DirectDriveController : IRoverDriveController
{
	private readonly StringName[] _usedActions =
	[
		RcaInEvName.CrabMode,
		RcaInEvName.SpinnerMode,
		RcaInEvName.EbrakeMode,
		RcaInEvName.AckermannMode,
		RcaInEvName.RoverMoveBackward,
		RcaInEvName.RoverMoveForward,
		RcaInEvName.RoverMoveRight,
		RcaInEvName.RoverMoveLeft,
		RcaInEvName.RoverMoveDown,
		RcaInEvName.RoverMoveUp,
	];

	public static float SpeedModifier => LocalSettings.Singleton.SpeedLimiter.Enabled ? LocalSettings.Singleton.SpeedLimiter.MaxSpeed : 1f;

	public RoverControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in RoverControl lastState)
	{
		//deadzone have to be non zero for IsEqualApprox
		var joyDeadZone = Mathf.Max(
			0.001f,
			Convert.ToSingle(LocalSettings.Singleton.Joystick.MinimalInput)
		);

		KinematicMode kinematicMode = OperateKinematicMode(inputEvent, targetInputDevice, lastState);

		Vector3 vec;

		switch (kinematicMode)
		{
			case KinematicMode.Ackermann:
				vec = new(
					Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.RoverMoveBackward, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveForward, targetInputDevice)),
					Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.RoverMoveRight, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveLeft, targetInputDevice)),
					0);
				break;
			case KinematicMode.Crab:
				vec = new(
					Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.RoverMoveBackward, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveForward, targetInputDevice)),
					Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.RoverMoveRight, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveLeft, targetInputDevice)),
					Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.RoverMoveDown, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveUp, targetInputDevice))
				);
				break;
			case KinematicMode.Spinner:
				vec = new(
					Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.RoverMoveBackward, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveForward, targetInputDevice)),
					0f,
					0f
				);
				break;
			case KinematicMode.EBrake:
				vec = new(0, 0, 0); //todo wheels in X pattern?
				break;
			default:
				vec = new(0, 0, 0);
				break;
		}

		vec.X *= SpeedModifier;

		vec.X = Mathf.IsEqualApprox(vec.X, 0f, joyDeadZone * SpeedModifier) ? 0 : vec.X;
		vec.Y = Mathf.IsEqualApprox(vec.Y, 0f, joyDeadZone) ? 0 : vec.Y;
		vec.Z = Mathf.IsEqualApprox(vec.Z, 0f, joyDeadZone) ? 0 : vec.Z;

		var ret = vec.ToRoverControl();
		ret.Mode = kinematicMode;

		return ret;
	}

	public KinematicMode OperateKinematicMode(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in RoverControl lastState)
	{
		switch (LocalSettings.Singleton.Joystick.ToggleableKinematics)
		{
			// Toggle
			case true when inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CrabMode, targetInputDevice), allowEcho: false, exactMatch: true):
				return KinematicMode.Crab;
			case true when inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.SpinnerMode, targetInputDevice), allowEcho: false, exactMatch: true):
				return KinematicMode.Spinner;
			case true when inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.EbrakeMode, targetInputDevice), allowEcho: false, exactMatch: true):
				return KinematicMode.EBrake;
			case true when inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.AckermannMode, targetInputDevice), allowEcho: false, exactMatch: true):
				return KinematicMode.Ackermann;
			case true: // Toggle with no action
				return lastState.Mode;
			// Hold
			case false when Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CrabMode, targetInputDevice), exactMatch: true):
				return KinematicMode.Crab;
			case false when Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.SpinnerMode, targetInputDevice), exactMatch: true):
				return KinematicMode.Spinner;
			case false when Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.EbrakeMode, targetInputDevice), exactMatch: true):
				return KinematicMode.EBrake;
			case false: //default for hold
				return KinematicMode.Ackermann;
		}
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
	"""
	Action: rover_move_backward/forward - is force of movment.

	- Ackermann mode (back wheels are inverted)
	  Action: rover_move_right/left
	  TO: set front wheel direction
	- Crab mode (all wheels in same direction)
	  Action: rover_move_right/left/down/up
	  TO: set steering Vector, where Vector.UP is front of rover
	- Spinner mode (wheels try to form circle around center of rover)
	  rover_move_backward is anti-clockwise
	  rover_move_forward is clockwise
	  TO: rotate in place
	- Brake mode (wheels will brake)
	  Wheels just stop. No action required.
	""";
}

