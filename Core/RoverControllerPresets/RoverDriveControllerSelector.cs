using System;
using RoverControlApp.Core.RoverControllerPresets.DriveControllers;

namespace RoverControlApp.Core.RoverControllerPresets;

public static class RoverDriveControllerSelector
{
	public enum Controller
	{
		GoodOldGamesLike = 0,
		EricSOn = 1,
		ForzaLike = 2,
		DirectDrive = 3
	}

	public const Controller DEFAULT = Controller.DirectDrive;

	public static IRoverDriveController GetController(Controller controller)
	{
		switch (controller)
		{
			case Controller.GoodOldGamesLike:
				return new GoodOldGamesLikeController();
			case Controller.EricSOn:
				return new EricSOnController();
			case Controller.ForzaLike:
				return new ForzaLikeController();
			case Controller.DirectDrive:
				return new DirectDriveController();
			default:
				throw new NotImplementedException();
		}
	}
}
