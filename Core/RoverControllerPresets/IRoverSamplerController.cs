using static RoverControlApp.Core.MqttClasses;
using Godot;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverSamplerController
{
	/// <summary>
	/// Probes Godot.Input and returns SamplerControl
	/// </summary>
	public SamplerControl CalculateMoveVector(SamplerControl lastState);

	/// <summary>
	/// Compares two SamplerControls states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(SamplerControl currentState, SamplerControl lastState) =>
		!Mathf.IsEqualApprox(currentState.DrillMovement, lastState.DrillMovement) ||
		!Mathf.IsEqualApprox(currentState.PlatformMovement, lastState.PlatformMovement) ||
		!Mathf.IsEqualApprox(currentState.DrillAction, lastState.DrillAction) ||
		currentState.ExtendContainer1 != lastState.ExtendContainer1 ||
		currentState.ExtendContainer2 != lastState.ExtendContainer2 ||
		currentState.OpenSkibidi != lastState.OpenSkibidi;
}
