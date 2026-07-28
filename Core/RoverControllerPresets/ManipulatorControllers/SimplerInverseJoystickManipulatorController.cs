using System;
using System.Collections.Generic;
using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class SimplerInverseJoystickManipulatorController : IRoverManipulatorController
{
	private readonly StringName[] _usedActions =
	[
		RcaInEvName.ManipulatorSimInvJoystickPosXPlus,
		RcaInEvName.ManipulatorSimInvJoystickPosXMinus,
		RcaInEvName.ManipulatorSimInvJoystickPosYPlus,
		RcaInEvName.ManipulatorSimInvJoystickPosYMinus,
		RcaInEvName.ManipulatorSimInvJoystickPosZPlus,
		RcaInEvName.ManipulatorSimInvJoystickPosZMinus,
		RcaInEvName.ManipulatorSimInvJoystickRotXPlus,
		RcaInEvName.ManipulatorSimInvJoystickRotXMinus,
		RcaInEvName.ManipulatorSimInvJoystickRotYPlus,
		RcaInEvName.ManipulatorSimInvJoystickRotYMinus,
		RcaInEvName.ManipulatorSimInvJoystickRotZPlus,
		RcaInEvName.ManipulatorSimInvJoystickRotZMinus,
		RcaInEvName.ManipulatorSimInvChangeRef
	];

	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in ManipulatorControl lastState)
	{
		ManipulatorControl manipulatorControl = new();
		manipulatorControl.ActionType = ActionType.InvKinJoystick;
		manipulatorControl.InvJoystick = new();

		if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvChangeRef, targetInputDevice), exactMatch: true))
		{
			manipulatorControl.Reference = "tool";
		}

		Vec3 linearSpeed = new();
		Vec3 angularSpeed = new();

		
		linearSpeed.X = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxLinearSpeed / 100f * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickPosXMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickPosXPlus, targetInputDevice));
		linearSpeed.Y = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxLinearSpeed / 100f * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickPosYMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickPosYPlus, targetInputDevice));
		linearSpeed.Z = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxLinearSpeed / 100f * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickPosZMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickPosZPlus, targetInputDevice));
		
		angularSpeed.X = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxAngularSpeed * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickRotXMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickRotXPlus, targetInputDevice));
		angularSpeed.Y = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxAngularSpeed * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickRotYMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickRotYPlus, targetInputDevice));
		angularSpeed.Z = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxAngularSpeed * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickRotZMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvJoystickRotZPlus, targetInputDevice));
		

		manipulatorControl.InvJoystick.LinearSpeed = linearSpeed;
		manipulatorControl.InvJoystick.RotationSpeed = angularSpeed;

		manipulatorControl.Gripper = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperBackward, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperForward, targetInputDevice));

		return manipulatorControl;
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"Use joysticks to control the axes of the manipulator. Click the right bumper to toggle between position and rotation. Hold Y (xbox) to change reference to 'tool' Gripper is controlled with triggers.";

	public string[] GetControlledAxes()
	{
		return new string[0];
	}

}
