using RoverControlApp.Core.RoverControllerPresets.ManipulatorControllers;

namespace RoverControlApp.Core.RoverControllerPresets;

public static class RoverManipulatorControllerSelector
{

	public static IRoverManipulatorController GetController(bool controlMode)
	{
		return controlMode ? new MultiAxisManipulatorController() : new SingleAxisManipulatorController();
	}
}
