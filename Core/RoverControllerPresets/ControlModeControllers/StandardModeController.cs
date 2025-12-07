using System;

using Godot;
using Godot.Collections;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ControlModeControllers;

public class StandardModeController : IControlModeController
{
	private static readonly StringName[] _usedActions =
	[
		RcaInEvName.ControlModeEstop,
		RcaInEvName.ControlModeChange,
		RcaInEvName.ControlModeDrive,
		RcaInEvName.ControlModeManipulator,
		RcaInEvName.ControlModeSampler,
		RcaInEvName.ControlModeAutonomy,
	];

	TimeSpan? estopStart;

	public bool EstopReq()
	{
		if (Input.IsActionJustPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeEstop), exactMatch: true))
		{
			estopStart = System.DateTime.Now.TimeOfDay;
			return false;
		}

		if (Input.IsActionJustReleased(DualSeatEvent.GetName(RcaInEvName.ControlModeEstop), exactMatch: true) && estopStart is not null)
		{
			if ((System.DateTime.Now.TimeOfDay - estopStart).Value.TotalSeconds < 5)
				return true;
		}

		return false;
	}


	public ControlMode GetControlMode(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice,
		in ControlMode lastState)
	{
		ControlMode newState = lastState;

		if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeEstop, targetInputDevice)))
		{
			if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeDrive, targetInputDevice), exactMatch: true))
			{
				estopStart = null;
				newState = ControlMode.Rover;
			}
			else if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeManipulator, targetInputDevice), exactMatch: true))
			{
				estopStart = null;
				newState = ControlMode.Manipulator;
			}
			else if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeSampler, targetInputDevice), exactMatch: true))
			{
				estopStart = null;
				newState = ControlMode.Sampler;
			}
			else if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeAutonomy, targetInputDevice), exactMatch: true))
			{
				estopStart = null;
				newState = ControlMode.Autonomy;
			}
		}
		else if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeChange, targetInputDevice), exactMatch: true))
		{
			if ((int)lastState + 1 >= Enum.GetNames<ControlMode>().Length)
				newState = ControlMode.Rover;
			else
				newState++;
		}
		return newState;
	}

	public System.Collections.Generic.Dictionary<StringName, Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
	"""
	To quick select control mode on the controller:
	 HOLD 'controlmode_estop' and PRESS desired mode.
	""";
}
