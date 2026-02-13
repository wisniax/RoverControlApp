using RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;
using System;

namespace RoverControlApp.Core.RoverControllerPresets;

public static class RoverManipulatorControllerSelector
{
	public enum Controller
	{
		MultiAxis = 0,
		SingleAxis = 1
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
			default:
				throw new NotImplementedException();
		}
	}
}
