using System;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverCalibrateController : IActionAwareController
{

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice)
	{
		EventLogger.LogMessage("IRoverCalibrateControll", EventLogger.LogLevel.Info, $"inputEvent: last: {inputEvent.ToString()} new: {targetInputDevice.ToString()}");
		return true;
	}

	/// <summary>
	/// Checks InputEvent and returns if wants to operate Calibrate changes
	/// </summary>
	public bool OperateMode(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice);

}
