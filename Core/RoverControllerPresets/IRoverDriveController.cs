using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverDriveController
{
	/// <summary>
	/// Probes Godot.Input and returns RoverControl
	/// </summary>
	public RoverControl CalculateMoveVector();

	public KinematicMode Mode { get; set; }

	/// <summary>
	/// Compares two RoverControl states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(RoverControl currentState, RoverControl lastState) =>
		!currentState.FromRoverControl().IsEqualApprox(lastState.FromRoverControl());
}
