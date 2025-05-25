using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverDriveController
{
	/// <summary>
	/// Checks InputEvent and returns RoverControl
	/// </summary>
	public RoverControl CalculateMoveVector(InputEvent inputEvent, in RoverControl lastState);

	/// <summary>
	/// Checks InputEvent and returns active KinematicMode
	/// </summary>
	public KinematicMode OperateKinematicMode(InputEvent inputEvent, in RoverControl lastState);

	/// <summary>
	/// Compares two RoverControl states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(in RoverControl currentState, in RoverControl lastState) =>
		!currentState.ToVector3().IsEqualApprox(lastState.ToVector3());

	public bool IsKinematicModeChanged(KinematicMode currentState, KinematicMode lastState) =>
		currentState != lastState;
}
