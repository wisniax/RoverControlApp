using System;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverDriveController
{
	/// <summary>
	/// Checks InputEvent and returns RoverControl
	/// </summary>
	public RoverControl CalculateMoveVector(in InputEvent inputEvent, in RoverControl lastState);

	/// <summary>
	/// Checks InputEvent and returns active KinematicMode
	/// </summary>
	public KinematicMode OperateKinematicMode(in InputEvent inputEvent, in RoverControl lastState);

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(in InputEvent inputEvent, RoverControl lastState, out RoverControl newState)
	{
		newState = CalculateMoveVector(inputEvent, lastState);
		bool changed = IsMoveVectorChanged(newState, lastState) || IsKinematicModeChanged(newState.Mode, lastState.Mode);
		newState.Timestamp = changed ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : lastState.Timestamp;
		return changed;
	}

	/// <summary>
	/// Compares two RoverControl states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(in RoverControl currentState, in RoverControl lastState) =>
		!currentState.ToVector3().IsEqualApprox(lastState.ToVector3());

	public bool IsKinematicModeChanged(KinematicMode currentState, KinematicMode lastState) =>
		currentState != lastState;
}
