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
	public ControlMode GetControlMode(in string actionSurffix, in InputEvent inputEvent, in ControlMode lastState);

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(in string actionSurffix, in InputEvent inputEvent, ControlMode lastState, out ControlMode newState)
	{
		newState = GetControlMode(actionSurffix, inputEvent, lastState);
		return IsMoveVectorChanged(newState, lastState);
	}

	/// <summary>
	/// Compares two ControlMode states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(in ControlMode currentState, in ControlMode lastState) =>
		!currentState.Equals(lastState);
}
