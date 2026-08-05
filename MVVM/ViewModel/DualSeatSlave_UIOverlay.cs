using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

public partial class DualSeatSlave_UIOverlay : UIOverlay
{
	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.DarkRed, Colors.Orange, "Slave: Off", "Slave: ") },
		{ 1, new(Colors.DarkGreen, Colors.LightGreen, "Slave: Driving","Slave: ") },
		{ 2, new(Colors.DarkOliveGreen, Colors.LightGreen, "Slave: Manipulator","Slave: ") },
		{ 3, new (Colors.LightGreen, Colors.DarkGreen, "Slave: Sampler", "Slave: ")},
		{ 4, new(Colors.DarkRed, Colors.Orange, "Slave: Off","Slave: ") }
	};

	public Task ControlModeChangedSubscriber(MqttClasses.ControlModeFlags newMode)
	{
		if (newMode.HasFlag(MqttClasses.ControlModeFlags.Drive)) ControlMode = 1;
		else if (newMode.HasFlag(MqttClasses.ControlModeFlags.RoboticArm)) ControlMode = 2;
		else if (newMode.HasFlag(MqttClasses.ControlModeFlags.DeepSampler) ||
				 newMode.HasFlag(MqttClasses.ControlModeFlags.SurfaceSampler)) ControlMode = 3;
		else ControlMode = 0;

		return Task.CompletedTask;
	}
}
