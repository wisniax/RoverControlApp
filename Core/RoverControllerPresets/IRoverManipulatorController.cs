using System;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverManipulatorController : IActionAwareController
{
	/// <summary>
	/// Probes Godot.Input and returns ManipulatorControl
	/// </summary>
	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, in ManipulatorControl lastState);

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(in InputEvent inputEvent, ManipulatorControl lastState, out ManipulatorControl newState)
	{
		newState = CalculateMoveVector(inputEvent, lastState);
		bool changed = IsMoveVectorChanged(newState, lastState);
		newState.Timestamp = changed ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : lastState.Timestamp;
		return changed;
	}

	/// <summary>
	/// Compares two ManipulatorControl states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(in ManipulatorControl currentState, in ManipulatorControl lastState) =>
		!currentState.Equals(lastState);

}
