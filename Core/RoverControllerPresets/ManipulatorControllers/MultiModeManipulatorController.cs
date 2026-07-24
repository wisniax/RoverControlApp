using System;
using System.Collections.Generic;
using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class MultiModeManipulatorController : IRoverManipulatorController
{
	private static readonly StringName[] _usedActions =
	[
		RcaInEvName.ManipulatorModeChange,
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
		RcaInEvName.ManipulatorMultiAxis1Backward,
		RcaInEvName.ManipulatorMultiAxis2Backward,
		RcaInEvName.ManipulatorMultiAxis3Backward,
		RcaInEvName.ManipulatorMultiAxis4Backward,
		RcaInEvName.ManipulatorMultiAxis5Backward,
		RcaInEvName.ManipulatorMultiAxis6Backward,
		RcaInEvName.ManipulatorMultiGripperBackward,
		RcaInEvName.ManipulatorMultiAxis1Forward,
		RcaInEvName.ManipulatorMultiAxis2Forward,
		RcaInEvName.ManipulatorMultiAxis3Forward,
		RcaInEvName.ManipulatorMultiAxis4Forward,
		RcaInEvName.ManipulatorMultiAxis5Forward,
		RcaInEvName.ManipulatorMultiAxis6Forward,
		RcaInEvName.ManipulatorMultiGripperForward,
		RcaInEvName.ManipulatorMultiChangeAxes
	];

	private ActionType _currentActionType = ActionType.InvKinJoystick;

	InverseJoystickManipulatorController inverseJoystickManipulatorController = new();
	MultiAxisManipulatorController multiAxisManipulatorController = new();

	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice tagetInputDevice, in ManipulatorControl lastState)
	{
		switch (_currentActionType)
		{
			case ActionType.ForwardKin:
				if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorModeChange, tagetInputDevice), allowEcho: false))
				{
					_currentActionType = ActionType.InvKinJoystick;
				}
				break;
			case ActionType.InvKinJoystick:
				if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorModeChange, tagetInputDevice), allowEcho: false))
				{
					_currentActionType = ActionType.ForwardKin;
				}
				break;
			default:
				_currentActionType = ActionType.ForwardKin;
				break;
		}

		switch(_currentActionType)
		{
			case ActionType.ForwardKin:
				return multiAxisManipulatorController.CalculateMoveVector(inputEvent, tagetInputDevice, lastState);
			case ActionType.InvKinJoystick:
				return inverseJoystickManipulatorController.CalculateMoveVector(inputEvent, tagetInputDevice, lastState);
			default:
				return new ManipulatorControl() { ActionType = ActionType.ForwardKin };
		}
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"MULTIMODE: Left bumper changes modes forward/inverse_joystick. \n\n" +
		"INVERSE_KIN:" + inverseJoystickManipulatorController.GetInputActionsAdditionalNote() + "\n\n" +
		"FORWARD_KIN:" + multiAxisManipulatorController.GetInputActionsAdditionalNote();

	public string[] GetControlledAxes()
	{
		switch (_currentActionType)
		{
			case ActionType.ForwardKin:
				return multiAxisManipulatorController.GetControlledAxes();
			case ActionType.InvKinJoystick:
				return inverseJoystickManipulatorController.GetControlledAxes();
			default:
				return Array.Empty<string>();
		}
	}

}
