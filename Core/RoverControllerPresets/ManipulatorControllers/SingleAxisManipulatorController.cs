using System.Collections.Generic;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class SingleAxisManipulatorController : IRoverManipulatorController
{
	private readonly string[] _usedActions =
	[
		"manipulator_speed_backward",
		"manipulator_speed_forward",
		"manipulator_axis_1",
		"manipulator_axis_2",
		"manipulator_axis_3",
		"manipulator_axis_4",
		"manipulator_axis_5",
		"manipulator_axis_6",
	];

	public ManipulatorControl CalculateMoveVector(in string actionSurffix, in InputEvent inputEvent, in ManipulatorControl lastState)
	{
		float velocity = Input.GetAxis("manipulator_speed_backward" + actionSurffix, "manipulator_speed_forward" + actionSurffix);
		if (Mathf.Abs(velocity) < LocalSettings.Singleton.Joystick.MinimalInput)
			velocity = 0f;

		ManipulatorControl manipulatorControl;
		if (Input.IsActionPressed("manipulator_axis_5" + actionSurffix) && Input.IsActionPressed("manipulator_axis_6" + actionSurffix))
		{
			manipulatorControl = new()
			{
				Gripper = velocity
			};
		}
		else
			manipulatorControl = new()
			{
				Axis1 = Input.IsActionPressed("manipulator_axis_1" + actionSurffix) ? velocity : 0f,
				Axis2 = Input.IsActionPressed("manipulator_axis_2" + actionSurffix) ? velocity : 0f,
				Axis3 = Input.IsActionPressed("manipulator_axis_3" + actionSurffix) ? velocity : 0f,
				Axis4 = Input.IsActionPressed("manipulator_axis_4" + actionSurffix) ? velocity : 0f,
				Axis5 = Input.IsActionPressed("manipulator_axis_5" + actionSurffix) ? velocity : 0f,
				Axis6 = Input.IsActionPressed("manipulator_axis_6" + actionSurffix) ? velocity : 0f
			};

		return manipulatorControl;
	}

	public Dictionary<string, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
		"manipulator_axis_5 + manipulator_axis_6 = gripper";
}
