using System;
using System.Collections.Generic;
using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class InverseJoystickManipulatorController : IRoverManipulatorController
{
	private readonly StringName[] _usedActions =
	[
		RcaInEvName.ManipulatorInvJoystickPosXPlus,
		RcaInEvName.ManipulatorInvJoystickPosXMinus,
		RcaInEvName.ManipulatorInvJoystickPosYPlus,
		RcaInEvName.ManipulatorInvJoystickPosYMinus,
		RcaInEvName.ManipulatorInvJoystickPosZPlus,
		RcaInEvName.ManipulatorInvJoystickPosZMinus,
		RcaInEvName.ManipulatorInvJoystickRotXPlus,
		RcaInEvName.ManipulatorInvJoystickRotXMinus,
		RcaInEvName.ManipulatorInvJoystickRotYPlus,
		RcaInEvName.ManipulatorInvJoystickRotYMinus,
		RcaInEvName.ManipulatorInvJoystickRotZPlus,
		RcaInEvName.ManipulatorInvJoystickRotZMinus,
		RcaInEvName.ManipulatorMultiChangeAxes,
	];

	private bool _axesChanged = true;

	public RoboticArmControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice tagetInputDevice, in RoboticArmControl lastState)
	{
		if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiChangeAxes, tagetInputDevice), allowEcho: false))
		{
			_axesChanged = !_axesChanged;
		}

		RoboticArmControl manipulatorControl = new();
		manipulatorControl.ActionType = ActionType.InvKinJoystick;
		manipulatorControl.InvJoystick = new();

		Vec3 linearSpeed = new();
		Vec3 angularSpeed = new();

		if (_axesChanged)
		{
			linearSpeed.X = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosXMinus, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosXPlus, tagetInputDevice));
			linearSpeed.Y = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosYMinus, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosYPlus, tagetInputDevice));
			linearSpeed.Z = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosZMinus, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosZPlus, tagetInputDevice));
		}
		else
		{
			angularSpeed.X = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotXMinus, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotXPlus, tagetInputDevice));
			angularSpeed.Y = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotYMinus, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotYPlus, tagetInputDevice));
			angularSpeed.Z = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotZMinus, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotZPlus, tagetInputDevice));
		}

		manipulatorControl.InvJoystick.LinearSpeed = linearSpeed;
		manipulatorControl.InvJoystick.RotationSpeed = angularSpeed;

		return manipulatorControl;
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"Use joysticks to control the axes of the manipulator. Click the right bumper to toggle between position and rotation. Gripper is not controlled with triggers.";

	public string[] GetControlledAxes()
	{
		return _axesChanged ? new string[] { "PosX", "PosY", "PosZ" } : new string[] { "RotX", "RotY", "RotZ" };
	}

}
