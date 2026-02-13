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

	private bool _useSecondaryAxes = false;

	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in ManipulatorControl lastState)
	{
		float gripper = Input.GetAxis(
			DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperBackward, targetInputDevice),
			DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiGripperForward, targetInputDevice)
		);

		bool holdToChangeAxes = LocalSettings.Singleton.Manipulator.HoldToChangeManipulatorAxes;
		bool changeAxesHeld = Input.IsActionPressed(
			DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiChangeAxes, targetInputDevice)
		);

		if (holdToChangeAxes)
		{
			_useSecondaryAxes = changeAxesHeld;
		}
		else
		{
			if (inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiChangeAxes, targetInputDevice)))
				_useSecondaryAxes = !_useSecondaryAxes;
		}

		if (!_useSecondaryAxes)
		{
			float axis1 = Input.GetAxis(
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis1Backward, targetInputDevice),
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis1Forward, targetInputDevice)
			);
			float axis2 = Input.GetAxis(
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis2Backward, targetInputDevice),
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis2Forward, targetInputDevice)
			);
			float axis3 = Input.GetAxis(
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis3Backward, targetInputDevice),
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis3Forward, targetInputDevice)
			);

			return new ManipulatorControl
			{
				Axis1 = axis1,
				Axis2 = axis2,
				Axis3 = axis3,
				Gripper = gripper
			};
		}
		else
		{
			float axis4 = Input.GetAxis(
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis4Backward, targetInputDevice),
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis4Forward, targetInputDevice)
			);
			float axis5 = Input.GetAxis(
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis5Backward, targetInputDevice),
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis5Forward, targetInputDevice)
			);
			float axis6 = Input.GetAxis(
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis6Backward, targetInputDevice),
				DualSeatEvent.GetName(RcaInEvName.ManipulatorMultiAxis6Forward, targetInputDevice)
			);

			return new ManipulatorControl
			{
				Axis4 = axis4,
				Axis5 = axis5,
				Axis6 = axis6,
				Gripper = gripper
			};
		}
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"Use joysticks to control the axes of the manipulator. Click the right bumper to toggle between axes 1-3 and axes 4-6. Gripper is controlled with triggers.";

	public string[] GetControlledAxes()
	{
		return _useSecondaryAxes ? new string[] { "Axis4", "Axis5", "Axis6", "Gripper" } : new string[] { "Axis1", "Axis2", "Axis3", "Gripper" };
	}

}
