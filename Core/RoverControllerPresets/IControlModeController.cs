using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IControlModeController : IActionAwareController
{
	/// <summary>
	/// Returns true when ControlMode should change to Estop.
	/// Called every frame, for safety.
	/// </summary>
	/// <returns></returns>
	public bool EstopReq();

	/// <summary>
	/// Checks InputEvent and returns ControlMode
	/// </summary>
	public ControlModeFlags GetControlMode(
		in InputEvent inputEvent,
		DualSeatEvent.InputDevice targetInputDevice,
		in ControlModeFlags lastState);

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(
		in InputEvent inputEvent,
		DualSeatEvent.InputDevice targetInputDevice,
		ControlModeFlags lastState,
		out ControlModeFlags newState)
	{
		newState = GetControlMode(inputEvent, targetInputDevice, lastState);
		return IsMoveVectorChanged(newState, lastState);
	}

	/// <summary>
	/// Compares two ControlMode states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(in ControlModeFlags currentState, in ControlModeFlags lastState) =>
		!currentState.Equals(lastState);
}
