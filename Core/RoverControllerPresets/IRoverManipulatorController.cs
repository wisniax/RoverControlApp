using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverManipulatorController
{
	/// <summary>
	/// Probes Godot.Input and returns ManipulatorControl
	/// </summary>
	public ManipulatorControl CalculateMoveVector();

	/// <summary>
	/// Compares two ManipulatorControl states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(ManipulatorControl currentState, ManipulatorControl lastState) =>
		!currentState.Equals(lastState);

}
