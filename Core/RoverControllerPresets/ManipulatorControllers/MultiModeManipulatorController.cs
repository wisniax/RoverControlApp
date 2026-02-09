using System;
using System.Collections.Generic;
using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class MultiModeManipulatorController : IRoverManipulatorController
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
		RcaInEvName.ManipulatorMultiChangeAxes
	];

	private bool _axesChanged = true;
	private ActionType _currentActionType = ActionType.Stop;

	InverseJoystickManipulatorController inverseJoystickManipulatorController = new();
	MultiAxisManipulatorController multiAxisManipulatorController = new();

	public RoboticArmControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice tagetInputDevice, in RoboticArmControl lastState)
	{
		if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiChangeAxes, tagetInputDevice), allowEcho: false))
		{
			_axesChanged = !_axesChanged;
		}
		switch (_currentActionType)
		{
			case ActionType.Stop:
				if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorModeChange, tagetInputDevice), allowEcho: false))
				{
					_currentActionType = ActionType.ForwardKin;
				}
				break;
			case ActionType.ForwardKin:
				if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorModeChange, tagetInputDevice), allowEcho: false))
				{
					_currentActionType = ActionType.InvKinJoystick;
				}
				break;
			case ActionType.InvKinJoystick:
				if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorModeChange, tagetInputDevice), allowEcho: false))
				{
					_currentActionType = ActionType.Stop;
				}
				break;
			default:
				_currentActionType = ActionType.Stop;
				break;
		}

		switch(_currentActionType)
		{
			case ActionType.Stop:
				return new RoboticArmControl() { ActionType = ActionType.Stop };
			case ActionType.ForwardKin:
				return multiAxisManipulatorController.CalculateMoveVector(inputEvent, tagetInputDevice, lastState);
			case ActionType.InvKinJoystick:
				return inverseJoystickManipulatorController.CalculateMoveVector(inputEvent, tagetInputDevice, lastState);
			default:
				return new RoboticArmControl() { ActionType = ActionType.Stop };
		}
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"Use joysticks to control the axes of the manipulator. Left bumper changes modes forward/inverse_joystick/stop. More info in MultiAxis and InverseJoy F1 hints";

	public string[] GetControlledAxes()
	{
		switch (_currentActionType)
		{
			case ActionType.Stop:
				break;
			case ActionType.ForwardKin:
				return multiAxisManipulatorController.GetControlledAxes();
				break;
			case ActionType.InvKinJoystick:
				return inverseJoystickManipulatorController.GetControlledAxes();
				break;
			default:
				break;
		}
		return Array.Empty<string>();
	}

}
