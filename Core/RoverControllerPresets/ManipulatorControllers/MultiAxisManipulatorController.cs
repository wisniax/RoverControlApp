using System.Collections.Generic;
using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

public class MultiAxisManipulatorController : IRoverManipulatorController
{
	private readonly string[] _usedActions =
	[
		"manipulator_change_axes"
	];

	private bool _axesChanged = false;

	public static float Deadzone(float value)
	{
		return Mathf.Abs(value) < LocalSettings.Singleton.Joystick.MinimalInput ? 0f : value;
	}

	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, in ManipulatorControl lastState)
	{
		int deviceId = 0;
		float lx = Deadzone(Input.GetJoyAxis(deviceId, JoyAxis.LeftX));
		float ly = Deadzone(Input.GetJoyAxis(deviceId, JoyAxis.LeftY));
		float rx = Deadzone(Input.GetJoyAxis(deviceId, JoyAxis.RightX));
		float ry = Deadzone(Input.GetJoyAxis(deviceId, JoyAxis.RightY));

		if (inputEvent.IsActionPressed("manipulator_change_axes", allowEcho: false))
		{
			_axesChanged = !_axesChanged;
		}

		ManipulatorControl manipulatorControl;

		if (!_axesChanged)
		{
			manipulatorControl = new()
			{

				Axis1 = lx,
				Axis2 = ly,
				Axis3 = rx,
				Axis4 = ry
			};
		} else
		{
			manipulatorControl = new()
			{
				Axis5 = lx,
				Axis6 = ly,
				Gripper = rx
			};
		}

		return manipulatorControl;
	}

	public Dictionary<string, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

}
