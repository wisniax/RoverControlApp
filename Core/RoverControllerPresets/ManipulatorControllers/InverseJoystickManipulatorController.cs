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
		RcaInEvName.ManipulatorMultiGripperForward,
		RcaInEvName.ManipulatorMultiGripperBackward,
		RcaInEvName.ManipulatorMultiChangeAxes,
		RcaInEvName.ManipulatorModeChange,
		RcaInEvName.ManipulatorInvChangeRef
	];

	private bool _useSecondaryAxes = false;

	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in ManipulatorControl lastState)
	{
		ManipulatorControl manipulatorControl = new();
		manipulatorControl.ActionType = ActionType.InvKinJoystick;
		manipulatorControl.InvJoystick = new();

		if (LocalSettings.Singleton.Manipulator.HoldToChangeManipulatorAxes == true)
		{
			_useSecondaryAxes = !Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiChangeAxes, targetInputDevice));
		}
		else
		{
			if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiChangeAxes, targetInputDevice), allowEcho: false))
			{
				_useSecondaryAxes = !_useSecondaryAxes;
			}
		}

		if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvChangeRef, targetInputDevice), exactMatch: true))
		{
			manipulatorControl.Reference = "tool";
		}

		Vec3 linearSpeed = new();
		Vec3 angularSpeed = new();

		if (_useSecondaryAxes)
		{
			linearSpeed.X = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxLinearSpeed / 100f * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosXMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosXPlus, targetInputDevice));
			linearSpeed.Y = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxLinearSpeed / 100f * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosYMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosYPlus, targetInputDevice));
			linearSpeed.Z = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxLinearSpeed / 100f * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosZMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickPosZPlus, targetInputDevice));
		}
		else
		{
			angularSpeed.X = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxAngularSpeed * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotXMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotXPlus, targetInputDevice));
			angularSpeed.Y = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxAngularSpeed * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotYMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotYPlus, targetInputDevice));
			angularSpeed.Z = LocalSettings.Singleton.Manipulator.InvKinScaler.MaxAngularSpeed * Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotZMinus, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorInvJoystickRotZPlus, targetInputDevice));
		}

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
		return _useSecondaryAxes ? new string[] { "PosX", "PosY", "PosZ" } : new string[] { "RotX", "RotY", "RotZ" };
	}

}
