using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class RoverMode_UIOverlay : UIOverlay
{
	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.DarkRed, Colors.Orange, "Rover: E-STOP", "Rover: ") },
		{ 1, new(Colors.DarkGreen, Colors.LightGreen, "Rover: Driving","Rover: ") },
		{ 2, new(Colors.DarkOliveGreen, Colors.LightGreen, "Rover: Manipulator","Rover: ") },
		{ 3, new (Colors.LightGreen, Colors.DarkGreen, "Rover: Sampler", "Rover:") },
		{ 4, new(Colors.DarkBlue, Colors.LightBlue, "Rover: DriveAutonomy","Rover: ") },
		{ 5, new(Colors.DarkBlue, Colors.LightBlue, "Rover: ManipulatorAu","Rover: ") },
		{ 6, new(Colors.DarkBlue, Colors.LightBlue, "Rover: SamplerAutono","Rover: ") }
	};

	public Task ControlModeChangedSubscriber(MqttClasses.ControlModeFlags newMode)
	{
		if (newMode.HasFlag(MqttClasses.ControlModeFlags.Drive)) ControlMode = 1;
		else if (newMode.HasFlag(MqttClasses.ControlModeFlags.RoboticArm)) ControlMode = 2;
		else if (newMode.HasFlag(MqttClasses.ControlModeFlags.DeepSampler) ||
				 newMode.HasFlag(MqttClasses.ControlModeFlags.SurfaceSampler)) ControlMode = 3;
		else if (newMode.HasFlag(MqttClasses.ControlModeFlags.DriveAutonomy)) ControlMode = 4;
		else if (newMode.HasFlag(MqttClasses.ControlModeFlags.RoboticArmAutonomy)) ControlMode = 5;
		else if (newMode.HasFlag(MqttClasses.ControlModeFlags.DeepSamplerAutonomy) ||
				 newMode.HasFlag(MqttClasses.ControlModeFlags.SurfaceSamplerAutonomy)) ControlMode = 6;
		else ControlMode = 0;

		return Task.CompletedTask;
	}
}
