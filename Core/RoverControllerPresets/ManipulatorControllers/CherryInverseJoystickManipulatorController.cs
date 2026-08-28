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
		RcaInEvName.ManipulatorGtrChangeRefPlus,
		RcaInEvName.ManipulatorGtrChangeRefMinus,
		RcaInEvName.ManipulatorGtrAccept,
		RcaInEvName.ManipulatorGtrCancel,
		RcaInEvName.ManipulatorModeChange,
	];

	private MultiAxisManipulatorController multiAxisManipulatorController = new();

	private bool _useToolReference = false;
	private int _GTRReference = 0; // "Go to reference" reference
	private ActionType actionType = ActionType.InvKinJoystick;
	private bool _movingToReference = false;

	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in ManipulatorControl lastState)
	{
		if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorModeChange, targetInputDevice), exactMatch: true))
		{
			switch (actionType)
			{
				case ActionType.InvKinJoystick:
					actionType = ActionType.GoToReference;
					break;
				case ActionType.GoToReference:
					actionType = ActionType.UseMoveITPlanning;
					_movingToReference = false; // reset moving to reference when changing mode
					break;
				case ActionType.UseMoveITPlanning:
					actionType = ActionType.ForwardKin;
					break;
				case ActionType.ForwardKin:
					actionType = ActionType.InvKinJoystick;
					break;
				default:
					break;
			}
		}

		ManipulatorControl manipulatorControl = new();
		manipulatorControl.ActionType = actionType;

		switch (actionType)
		{
			case ActionType.InvKinJoystick:
			{
				manipulatorControl.InvJoystick = new();

				if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorGtrChangeRefPlus, targetInputDevice), exactMatch: true))
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

			case ActionType.GoToReference:
				{
					if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorGtrChangeRefPlus, targetInputDevice), exactMatch: true))
					{
						_GTRReference++;
						_GTRReference = _GTRReference % 10; // wrap around to 0 after 9
						_movingToReference = false; // reset moving to reference when changing reference
					}

					if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorGtrChangeRefMinus, targetInputDevice), exactMatch: true))
					{
						_GTRReference--;
						if (_GTRReference < 0) // wrap around to 0 after 9
						{
							_GTRReference = 9;
						}
						_movingToReference = false; // reset moving to reference when changing reference
					}

					manipulatorControl.Reference = $"Ref{_GTRReference}";

					if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorGtrAccept, targetInputDevice), exactMatch: true))
					{
						_movingToReference = true;
					}

					if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorGtrCancel, targetInputDevice), exactMatch: true))
					{
						_movingToReference = false;
					}

					manipulatorControl.ActionType = _movingToReference ? ActionType.GoToReference : ActionType.Stop;
					return manipulatorControl;
				}
			case ActionType.UseMoveITPlanning:
				{
					manipulatorControl.ActionType = ActionType.UseMoveITPlanning;
					return manipulatorControl;
				}
			case ActionType.ForwardKin:
				{
					manipulatorControl = multiAxisManipulatorController.CalculateMoveVector(inputEvent, targetInputDevice, lastState);
					return manipulatorControl;
				}
			default:
				return manipulatorControl;
		}
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"D-pad UP: cycle modes (InvKinJoystick → GoToReference → UseMoveITPlanning).\n" +
		"D-pad DOWN: in GoToReference mode, sends the GoToReference command.\n\n" +
		"InvKinJoystick: Left stick = posX/posY, Y/A = posZ±, Right stick = rotY/rotZ, B/X = rotX±, Triggers = gripper. ChangeRef toggles 'tool' reference.\n\n" +
		"GoToReference: ChangeRef cycles Ref0–Ref4, D-pad DOWN sends GoToReference, otherwise sends Stop.\n\n" +
		"MoveITPlanning: Hands off — sends control to MoveIT.";

	public string[] GetControlledAxes()
	{
		switch (actionType)
		{
			case ActionType.InvKinJoystick:
				return new string[] { "posX", "posY", "posZ", "rotX", "rotY", "rotZ", "gripper" };
			case ActionType.GoToReference:
				return new string[0];
			case ActionType.UseMoveITPlanning:
				return new string[0];
			case ActionType.ForwardKin:
				return multiAxisManipulatorController.GetControlledAxes();
			default:
				return new string[0];
		}
	}

}
