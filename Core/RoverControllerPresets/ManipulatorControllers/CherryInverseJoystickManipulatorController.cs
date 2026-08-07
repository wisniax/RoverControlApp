using System;
using System.Collections.Generic;
using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class CherryInverseJoystickManipulatorController : IRoverManipulatorController
{
	private readonly StringName[] _usedActions =
	[
		RcaInEvName.ManipulatorCherryInvJoystickPosXPlus,
		RcaInEvName.ManipulatorCherryInvJoystickPosXMinus,
		RcaInEvName.ManipulatorCherryInvJoystickPosYPlus,
		RcaInEvName.ManipulatorCherryInvJoystickPosYMinus,
		RcaInEvName.ManipulatorCherryInvJoystickPosZPlus,
		RcaInEvName.ManipulatorCherryInvJoystickPosZMinus,
		RcaInEvName.ManipulatorCherryInvJoystickRotXPlus,
		RcaInEvName.ManipulatorCherryInvJoystickRotXMinus,
		RcaInEvName.ManipulatorCherryInvJoystickRotYPlus,
		RcaInEvName.ManipulatorCherryInvJoystickRotYMinus,
		RcaInEvName.ManipulatorCherryInvJoystickRotZPlus,
		RcaInEvName.ManipulatorCherryInvJoystickRotZMinus,
		RcaInEvName.ManipulatorCherryInvChangeRef
	];

	private bool _useToolReference = false;

	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in ManipulatorControl lastState)
	{
		ManipulatorControl manipulatorControl = new();
		manipulatorControl.ActionType = ActionType.InvKinJoystick;
		manipulatorControl.InvJoystick = new();

		if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvChangeRef, targetInputDevice), exactMatch: true))
		{
			_useToolReference = !_useToolReference;
		}

		if (_useToolReference)
		{
			manipulatorControl.Reference = "tool";
		}

		Vec3 linearSpeed = new();
		Vec3 angularSpeed = new();

		
		linearSpeed.X = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxLinearSpeed / 100f * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickPosXMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickPosXPlus, targetInputDevice));
		linearSpeed.Y = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxLinearSpeed / 100f * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickPosYMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickPosYPlus, targetInputDevice));
		linearSpeed.Z = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxLinearSpeed / 100f * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickPosZMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickPosZPlus, targetInputDevice));
		
		angularSpeed.X = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxAngularSpeed * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickRotXMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickRotXPlus, targetInputDevice));
		angularSpeed.Y = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxAngularSpeed * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickRotYMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickRotYPlus, targetInputDevice));
		angularSpeed.Z = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxAngularSpeed * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickRotZMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorCherryInvJoystickRotZPlus, targetInputDevice));
		

		manipulatorControl.InvJoystick.LinearSpeed = linearSpeed;
		manipulatorControl.InvJoystick.RotationSpeed = angularSpeed;

		manipulatorControl.Gripper = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperBackward, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperForward, targetInputDevice));

		return manipulatorControl;
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"Use left joystick, Y and A buttons to control manipulator movement. Use right joystick, X and B buttons to control manipulator rotation. Gripper is controlled with triggers.";

	public string[] GetControlledAxes()
	{
		return new string[0];
	}

}
