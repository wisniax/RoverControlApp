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
		RcaInEvName.ManipulatorMultiChangeAxes,
	];

	private bool _axesChanged = false;

	public RoboticArmControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice tagetInputDevice, in RoboticArmControl lastState)
	{
		if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiChangeAxes, tagetInputDevice), allowEcho: false))
		{
			_axesChanged = !_axesChanged;
		}

		RoboticArmControl manipulatorControl = new();

		manipulatorControl.ActionType = ActionType.ForwardKin;
		manipulatorControl.ForwardKin = new();

		float gripper = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperBackward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperForward, tagetInputDevice));

		if (!_axesChanged)
		{
			float axis1 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis1Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis1Forward, tagetInputDevice));
			float axis2 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis2Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis2Forward, tagetInputDevice));
			float axis3 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis3Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis3Forward, tagetInputDevice));


			manipulatorControl.ForwardKin.Axis1 = axis1;
			manipulatorControl.ForwardKin.Axis2 = axis2;
			manipulatorControl.ForwardKin.Axis3 = axis3;
			manipulatorControl.ForwardKin.Axis4 = 0;
			manipulatorControl.ForwardKin.Axis5 = 0;
			manipulatorControl.ForwardKin.Axis6 = 0;
		}
		else
		{
			float axis4 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis4Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis4Forward, tagetInputDevice));
			float axis5 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis5Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis5Forward, tagetInputDevice));
			float axis6 = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis6Backward, tagetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis6Forward, tagetInputDevice));

			manipulatorControl.ForwardKin.Axis1 = 0;
			manipulatorControl.ForwardKin.Axis2 = 0;
			manipulatorControl.ForwardKin.Axis3 = 0;
			manipulatorControl.ForwardKin.Axis4 = axis4;
			manipulatorControl.ForwardKin.Axis5 = axis5;
			manipulatorControl.ForwardKin.Axis6 = axis6;
		}

		manipulatorControl.Gripper = gripper;

		return manipulatorControl;
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"Use joysticks to control the axes of the manipulator. Click the right bumper to toggle between axes 1-3 and axes 4-6. Gripper is controlled with triggers.";

	public string[] GetControlledAxes()
	{
		return _axesChanged ? new string[] { "Axis4", "Axis5", "Axis6", "Gripper" } : new string[] { "Axis1", "Axis2", "Axis3", "Gripper" };
	}

}
