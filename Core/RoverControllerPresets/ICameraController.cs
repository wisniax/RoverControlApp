using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface ICameraController : IActionAwareController
{
	/// <summary>
	/// Checks InputEvent and returns new state
	/// </summary>
	public Vector4 CalculateMoveVector(
		in InputEvent inputEvent,
		DualSeatEvent.InputDevice targetInputDevice,
		in Vector4 lastState);

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(
		in InputEvent inputEvent,
		DualSeatEvent.InputDevice targetInputDevice,
		Vector4 lastState,
		out Vector4 newState)
	{
		newState = CalculateMoveVector(inputEvent, targetInputDevice, lastState);
		return IsMoveVectorChanged(newState, lastState);
	}

	/// <summary>
	/// Compares two states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(in Vector4 currentState, in Vector4 lastState) =>
		!currentState.IsEqualApprox(lastState);
}
