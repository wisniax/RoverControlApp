using static RoverControlApp.Core.MqttClasses;

using Godot;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverSamplerController
{
	/// <summary>
	/// Probes Godot.Input and returns SamplerControl
	/// </summary>
	public SamplerControl CalculateMoveVector(in InputEvent inputEvent, in SamplerControl lastState);

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(in InputEvent inputEvent, SamplerControl lastState, out SamplerControl newState)
	{
		newState = CalculateMoveVector(inputEvent, lastState);
		return IsMoveVectorChanged(newState, lastState);
	}

	/// <summary>
	/// Compares two SamplerControls states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(in SamplerControl currentState, in SamplerControl lastState) =>
		!Mathf.IsEqualApprox(currentState.DrillMovement, lastState.DrillMovement) ||
		!Mathf.IsEqualApprox(currentState.PlatformMovement, lastState.PlatformMovement) ||
		!Mathf.IsEqualApprox(currentState.DrillAction, lastState.DrillAction) ||
		!Mathf.IsEqualApprox(currentState.ContainerDegrees0, lastState.ContainerDegrees0) ||
		!Mathf.IsEqualApprox(currentState.ContainerDegrees1, lastState.ContainerDegrees1) ||
		!Mathf.IsEqualApprox(currentState.ContainerDegrees2, lastState.ContainerDegrees2);
}
