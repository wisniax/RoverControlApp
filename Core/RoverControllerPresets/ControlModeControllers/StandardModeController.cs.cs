using System;

using Godot;
using Godot.Collections;

using RoverControlApp.MVVM.Model;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ControlModeControllers;

public class StandardModeController : IControlModeController
{
	private static readonly string[] _usedActions =
	[
		"controlmode_estop",
		"controlmode_change",
		"controlmode_drive",
		"controlmode_manipulator",
		"controlmode_sampler",
		"controlmode_autonomy",
	];

	TimeSpan? estopStart;

	public bool EstopReq()
	{
		if (Input.IsActionJustPressed("controlmode_estop", exactMatch: true))
		{
			estopStart = System.DateTime.Now.TimeOfDay;
			return false;
		}

		if (Input.IsActionJustReleased("controlmode_estop", exactMatch: true) && estopStart is not null)
		{
			if ((System.DateTime.Now.TimeOfDay - estopStart).Value.TotalSeconds < 5)
				return true;
		}

		return false;
	}


	public ControlMode GetControlMode(in InputEvent inputEvent, in ControlMode lastState)
	{
		ControlMode newState = lastState;

		if (Input.IsActionPressed("controlmode_estop") || PressedKeys.IsInputFromKeyboard(inputEvent))
		{
			if (inputEvent.IsActionPressed("controlmode_drive", exactMatch: true))
			{
				estopStart = null;
				newState = ControlMode.Rover;
			}
			else if (inputEvent.IsActionPressed("controlmode_manipulator", exactMatch: true))
			{
				estopStart = null;
				newState = ControlMode.Manipulator;
			}
			else if (inputEvent.IsActionPressed("controlmode_sampler", exactMatch: true))
			{
				estopStart = null;
				newState = ControlMode.Sampler;
			}
			else if (inputEvent.IsActionPressed("controlmode_autonomy", exactMatch: true))
			{
				estopStart = null;
				newState = ControlMode.Autonomy;
			}
		}
		else if (inputEvent.IsActionPressed("controlmode_change", exactMatch: true))
		{
			if ((int)lastState + 1 >= Enum.GetNames<ControlMode>().Length)
				newState = ControlMode.Rover;
			else
				newState++;
		}
		return newState;
	}

	public System.Collections.Generic.Dictionary<string, Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
	"""
	To quick select control mode on the controller:
	 HOLD 'controlmode_estop' and PRESS desired mode.
	""";
}
