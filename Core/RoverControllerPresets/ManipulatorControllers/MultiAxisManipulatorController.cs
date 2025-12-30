using System.Collections.Generic;
using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class MultiAxisManipulatorController : IRoverManipulatorController
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

	public static float Deadzone(float value)
	{
		return Mathf.Abs(value) < LocalSettings.Singleton.Joystick.MinimalInput ? 0f : value;
	}

	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, in ManipulatorControl lastState)
	{
		//float velocity = Input.GetAxis("manipulator_speed_backward", "manipulator_speed_forward");
		//if (Mathf.Abs(velocity) < LocalSettings.Singleton.Joystick.MinimalInput)
		//	velocity = 0f;
		int deviceId = 0;
		float lx = Deadzone(Input.GetJoyAxis(deviceId, JoyAxis.LeftX));
		float ly = Deadzone(Input.GetJoyAxis(deviceId, JoyAxis.LeftY));
		float rx = Deadzone(Input.GetJoyAxis(deviceId, JoyAxis.RightX));
		float ry = Deadzone(Input.GetJoyAxis(deviceId, JoyAxis.RightY));


		ManipulatorControl manipulatorControl = new()
		{

			Axis1 = lx,
			Axis2 = ly,
			Axis3 = rx,
			Axis4 = ry
			//Axis5 = Input.IsActionPressed("manipulator_axis_5") ? velocity : 0f,
			//Axis6 = Input.IsActionPressed("manipulator_axis_6") ? velocity : 0f
		};


		return manipulatorControl;
	}

	public Dictionary<string, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	//public string GetInputActionsAdditionalNote() =>
	//	"manipulator_axis_5 + manipulator_axis_6 = gripper";
}
