using System;
using System.Collections.Generic;
using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class MultiAxisManipulatorController : IRoverManipulatorController
{
	private readonly StringName[] _usedActions =
	[
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

	private bool _axesChanged = false;

	private float ApplyDeadzone(float velocity)
	{
		if (Mathf.Abs(velocity) < LocalSettings.Singleton.Joystick.MinimalInput)
			return 0f;
		return velocity;
	}

	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice tagetInputDevice, in ManipulatorControl lastState)
	{
		if (inputEvent.IsActionPressed(RcaInEvName.ManipulatorMultiChangeAxes, allowEcho: false))
		{
			_axesChanged = !_axesChanged;
		}

		ManipulatorControl manipulatorControl;

		if (!_axesChanged)
		{
			float axis1 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis1Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis1Forward, tagetInputDevice));
			float axis2 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis2Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis2Forward, tagetInputDevice));
			float axis3 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis3Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis3Forward, tagetInputDevice));
			float axis4 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis4Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis4Forward, tagetInputDevice));

			manipulatorControl = new()
			{
				Axis1 = ApplyDeadzone(axis1),
				Axis2 = ApplyDeadzone(axis2),
				Axis3 = ApplyDeadzone(axis3),
				Axis4 = ApplyDeadzone(axis4),
			};
		} else
		{
			float axis5 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis5Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis5Forward, tagetInputDevice));
			float axis6 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis6Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis6Forward, tagetInputDevice));
			float gripper = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperBackward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperForward, tagetInputDevice));

			manipulatorControl = new()
			{
				Axis5 = ApplyDeadzone(axis5),
				Axis6 = ApplyDeadzone(axis6),
				Gripper = ApplyDeadzone(gripper),
			};
		}

		return manipulatorControl;
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"Use joysticks to control the axes of the manipulator. Click the right bumper to toggle between axes 1-4 and axes 5-6 + gripper.";

	public string[] GetControlledAxes()
	{
		return _axesChanged ? new string[] { "Axis5", "Axis6", "Gripper" } : new string[] { "Axis1", "Axis2", "Axis3", "Axis4" };
	}

}
