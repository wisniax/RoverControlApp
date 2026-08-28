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
		RcaInEvName.ManipulatorSimInvChangeRef,
		RcaInEvName.ManipulatorModeChange,
		RcaInEvName.ManipulatorGtrChangeRefPlus,
		RcaInEvName.ManipulatorGtrChangeRefMinus,
		RcaInEvName.ManipulatorGtrAccept,
		RcaInEvName.ManipulatorGtrCancel,
		RcaInEvName.ManipulatorMultiGripperBackward,
		RcaInEvName.ManipulatorMultiGripperForward,
		RcaInEvName.ManipulatorMultiAxis1Backward,
		RcaInEvName.ManipulatorMultiAxis2Backward,
		RcaInEvName.ManipulatorMultiAxis3Backward,
		RcaInEvName.ManipulatorMultiAxis4Backward,
		RcaInEvName.ManipulatorMultiAxis5Backward,
		RcaInEvName.ManipulatorMultiAxis6Backward,
		RcaInEvName.ManipulatorMultiAxis1Forward,
		RcaInEvName.ManipulatorMultiAxis2Forward,
		RcaInEvName.ManipulatorMultiAxis3Forward,
		RcaInEvName.ManipulatorMultiAxis4Forward,
		RcaInEvName.ManipulatorMultiAxis5Forward,
		RcaInEvName.ManipulatorMultiAxis6Forward,
		RcaInEvName.ManipulatorMultiChangeAxes
	];

	MultiAxisManipulatorController multiAxisManipulatorController = new();

	private bool _useToolReference = false;
	private int _GTRReference = 0; // "Go to reference" reference
	private ActionType actionType = ActionType.InvKinJoystick;
	private bool _movingToReference = false;


	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in ManipulatorControl lastState)
	{
		ManipulatorControl manipulatorControl = new();
		manipulatorControl.InvJoystick = new();

		if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorModeChange, targetInputDevice), exactMatch: true))
		{
			switch (actionType)
			{
				case ActionType.InvKinJoystick:
					actionType = ActionType.GoToReference;
					break;
				case ActionType.GoToReference:
					actionType = ActionType.UseMoveITPlanning;
					_movingToReference = false; // reset moving to reference when changing action type
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

		manipulatorControl.ActionType = actionType;

		switch (actionType)
		{
			case ActionType.InvKinJoystick:
			{
				manipulatorControl.ActionType = ActionType.InvKinJoystick;

				if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorSimInvChangeRef, targetInputDevice), exactMatch: true))
				{
					_useToolReference = !_useToolReference;
				}

				if (_useToolReference)
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
		"D-pad UP: cycle modes (InvKinJoystick → GoToReference → UseMoveITPlanning → ForwardKin).\n\n" +
		"InvKinJoystick: Left stick = posX/posY, Right stick = posZ, Left/right bumper = rotX, A/Y = rotY, X/B = rotZ, Triggers = gripper. D-pad right toggles 'tool' reference.\n\n" +
		"GoToReference: Y/X cycles Ref0–Ref9, A sends GoToReference, B cancels (Stop).\n\n" +
		"UseMoveITPlanning: Hands off — sends control to MoveIT.\n\n" +
		"ForwardKin: Joysticks control axes 1–3, right bumper toggles axes 4–6, Triggers = gripper.";

	public string[] GetControlledAxes()
	{
		switch (actionType)
		{
			case ActionType.InvKinJoystick:
				return new string[0];
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
