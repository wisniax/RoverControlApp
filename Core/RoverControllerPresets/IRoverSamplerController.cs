using static RoverControlApp.Core.MqttClasses;

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
	public bool IsMoveVectorChanged(SamplerControl currentState, SamplerControl lastState);
}
