using System;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverManipulatorController : IActionAwareController
{
	/// <summary>
	/// Probes Godot.Input and returns ManipulatorControl
	/// </summary>
	public RoboticArmControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in RoboticArmControl lastState);

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, RoboticArmControl lastState, out RoboticArmControl newState)
	{
		newState = CalculateMoveVector(inputEvent, targetInputDevice, lastState);
		bool changed = IsMoveVectorChanged(newState, lastState);
		newState.Timestamp = changed ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : lastState.Timestamp;
		return changed;
	}

	/// <summary>
	/// Compares two ManipulatorControl states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(in RoboticArmControl currentState, in RoboticArmControl lastState) =>
		!currentState.Equals(lastState);

	/// <summary>
	/// Returns currently controlled axes
	/// </summary>
	public string[] GetControlledAxes();
}
