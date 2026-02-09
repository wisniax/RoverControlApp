using System.Collections.Generic;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class SingleAxisManipulatorController : IRoverManipulatorController
{
	private readonly StringName[] _usedActions =
	[
		RcaInEvName.ManipulatorSpeedBackward,
		RcaInEvName.ManipulatorSpeedForward,
		RcaInEvName.ManipulatorAxis1,
		RcaInEvName.ManipulatorAxis2,
		RcaInEvName.ManipulatorAxis3,
		RcaInEvName.ManipulatorAxis4,
		RcaInEvName.ManipulatorAxis5,
		RcaInEvName.ManipulatorAxis6,
	];

	public RoboticArmControl CalculateMoveVector(
		in InputEvent inputEvent,
		DualSeatEvent.InputDevice targetInputDevice,
		in RoboticArmControl lastState)
	{
		float velocity = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.ManipulatorSpeedBackward, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.ManipulatorSpeedForward, targetInputDevice));
		if (Mathf.Abs(velocity) < LocalSettings.Singleton.Joystick.MinimalInput)
			velocity = 0f;

		RoboticArmControl manipulatorControl = new();

		manipulatorControl.ActionType = ActionType.ForwardKin;
		manipulatorControl.ForwardKin = new();

		if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis5, targetInputDevice)) && Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis6, targetInputDevice)))
		{
			// No gripper control yet

			//manipulatorControl = new()
			//{
			//	Gripper = velocity
			//};
		}
		else
		{
			manipulatorControl.ForwardKin.Axis1 = Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis1, targetInputDevice)) ? velocity : 0f;
			manipulatorControl.ForwardKin.Axis2 = Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis2, targetInputDevice)) ? velocity : 0f;
			manipulatorControl.ForwardKin.Axis3 = Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis3, targetInputDevice)) ? velocity : 0f;
			manipulatorControl.ForwardKin.Axis4 = Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis4, targetInputDevice)) ? velocity : 0f;
			manipulatorControl.ForwardKin.Axis5 = Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis5, targetInputDevice)) ? velocity : 0f;
			manipulatorControl.ForwardKin.Axis6 = Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis6, targetInputDevice)) ? velocity : 0f;
		}
		

		return manipulatorControl;
	}

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"manipulator_axis_5 + manipulator_axis_6 = gripper";

	public string[] GetControlledAxes()
	{
		var activeAxes = new List<string>();

		if (Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis5)) && Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.ManipulatorAxis6)))
		{
			activeAxes.Add("Gripper");
			return activeAxes.ToArray();
		}

		var axisMap = new (StringName action, string axis)[]
		{
			(RcaInEvName.ManipulatorAxis1, "Axis1"),
			(RcaInEvName.ManipulatorAxis2, "Axis2"),
			(RcaInEvName.ManipulatorAxis3, "Axis3"),
			(RcaInEvName.ManipulatorAxis4, "Axis4"),
			(RcaInEvName.ManipulatorAxis5, "Axis5"),
			(RcaInEvName.ManipulatorAxis6, "Axis6")
		};

		foreach (var (action, axisName) in axisMap)
		{
			if (Input.IsActionPressed(DualSeatEvent.GetName(action)))
				activeAxes.Add(axisName);
		}

		return activeAxes.ToArray();
	}
}
