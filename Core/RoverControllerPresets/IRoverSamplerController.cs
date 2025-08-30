using System;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverSamplerController : IActionAwareController
{
	/// <summary>
	/// Probes Godot.Input and returns SamplerControl
	/// </summary>
	public SamplerControl CalculateMoveVector(in string actionSurffix, in InputEvent inputEvent, in SamplerControl lastState);

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(in string actionSurffix, in InputEvent inputEvent, SamplerControl lastState, out SamplerControl newState)
	{
		newState = CalculateMoveVector(actionSurffix, inputEvent, lastState);
		bool changed = IsMoveVectorChanged(newState, lastState);
		newState.Timestamp = changed ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : lastState.Timestamp;
		return changed;
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
		!Mathf.IsEqualApprox(currentState.VacuumSuction, lastState.VacuumSuction) ||
		!Mathf.IsEqualApprox(currentState.VacuumA, lastState.VacuumA) ||
		!Mathf.IsEqualApprox(currentState.VacuumB, lastState.VacuumB);
}
