using RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;
using System;

namespace RoverControlApp.Core.RoverControllerPresets;

public static class RoverManipulatorControllerSelector
{
	public enum Controller
	{
		MultiAxis = 0,
		SingleAxis = 1,
		InverseJoystick = 2,
		MultiMode = 3,
		SimplerInverseJoystick = 4,
	}

	public const Controller DEFAULT = Controller.MultiAxis;

	public static IRoverManipulatorController GetController(Controller controller)
	{
		switch (controller)
		{
			case Controller.MultiAxis:
				return new MultiAxisManipulatorController();
			case Controller.SingleAxis:
				return new SingleAxisManipulatorController();
			case Controller.InverseJoystick:
				return new InverseJoystickManipulatorController();
			case Controller.SimplerInverseJoystick:
				return new SimplerInverseJoystickManipulatorController();
			case Controller.MultiMode:
				return new MultiModeManipulatorController();
			default:
				throw new NotImplementedException();
		}
	}
}
