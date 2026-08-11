using System;

using Godot;
using Godot.Collections;
using RoverControlApp.MVVM.Model;
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
		RcaInEvName.JoystickLeftPress,
		RcaInEvName.JoystickRightPress,
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


	public ControlModeFlags GetControlMode(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice,
		in ControlModeFlags lastState)
	{
		ControlModeFlags newState = lastState;

		if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeEstop, targetInputDevice)))
		{
			if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeDrive, targetInputDevice), exactMatch: true))
			{
				estopStart = null;
				newState = ControlModeFlags.Drive;
			}
			else if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeManipulator, targetInputDevice), exactMatch: true))
			{
				estopStart = null;
				newState = ControlModeFlags.RoboticArm;
			}
			else if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeSampler, targetInputDevice), exactMatch: true))
			{
				estopStart = null;
				newState = ControlModeFlags.DeepSampler | ControlModeFlags.SurfaceSampler;
			}
			else if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeAutonomy, targetInputDevice), exactMatch: true))
			{
				estopStart = null;

				if (IsDriveGroup(lastState))
					newState = ControlModeFlags.DriveAutonomy;
				else if (IsRoboticArmGroup(lastState))
					newState = ControlModeFlags.RoboticArmAutonomy;
				else if (IsSamplerGroup(lastState))
					newState = ControlModeFlags.DeepSamplerAutonomy | ControlModeFlags.SurfaceSamplerAutonomy;
				else
					newState = lastState;
			}
		}
		else if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ControlModeChange, targetInputDevice), exactMatch: true))
		{
			if (IsDriveGroup(lastState))
				newState = ControlModeFlags.RoboticArm;
			else if (IsRoboticArmGroup(lastState))
				newState = ControlModeFlags.DeepSampler | ControlModeFlags.SurfaceSampler;
			else if (IsSamplerGroup(lastState))
				newState = ControlModeFlags.Drive;
			else
				newState = ControlModeFlags.Drive;
		}

		if ((Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.JoystickLeftPress, targetInputDevice), exactMatch: true) &&
			(Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.JoystickRightPress, targetInputDevice), exactMatch: true))) &&
			(inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.JoystickLeftPress, targetInputDevice), exactMatch: true) ||
			(inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.JoystickRightPress, targetInputDevice), exactMatch: true))))
		{
			if (newState.HasFlag(ControlModeFlags.Stop))
				newState &= ~ControlModeFlags.Stop;
			else
				newState |= ControlModeFlags.Stop;
		}

		return newState;
	}

	private static bool IsDriveGroup(ControlModeFlags mode) =>
		(mode & (ControlModeFlags.Drive | ControlModeFlags.DriveAutonomy)) != 0;

	private static bool IsRoboticArmGroup(ControlModeFlags mode) =>
		(mode & (ControlModeFlags.RoboticArm | ControlModeFlags.RoboticArmAutonomy)) != 0;

	private static bool IsSamplerGroup(ControlModeFlags mode) =>
		(mode & (ControlModeFlags.DeepSampler | ControlModeFlags.DeepSamplerAutonomy
			| ControlModeFlags.SurfaceSampler | ControlModeFlags.SurfaceSamplerAutonomy)) != 0;

	public System.Collections.Generic.Dictionary<StringName, Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
	"""
	To quick select control mode on the controller:
	 HOLD 'controlmode_estop' and PRESS desired mode.
	 Use 'controlmode_change' to cycle: Drive -> RoboticArm -> Sampler.
	 Autonomy is toggled per-mode by the active controller or UI.
	""";
}
