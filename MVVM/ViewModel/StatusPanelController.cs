using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using RoverControlApp.MVVM;

public partial class StatusPanelController : VBoxContainer
{
	[Export] EStopPanel _eStopPanel = null!;
	[Export] SamplerFeedbackPanel _samplerPanel = null!;
	[Export] ArmAutonomyPanel _armAutonomyPanel = null!;
	[Export] ManipulatorPanel _manipulatorPanel = null!;
	public override void _Ready()
	{
		PressedKeys.Singleton.OnControlModeChanged += ControlModeChangedSubscriber;
		ControlModeChangedSubscriber(PressedKeys.Singleton.GetCurrentControlMode());
	}

	public override void _ExitTree()
	{
		PressedKeys.Singleton.OnControlModeChanged -= ControlModeChangedSubscriber;
	}

	public Task ControlModeChangedSubscriber(MqttClasses.ControlModeFlags newMode)
	{
		_eStopPanel.SetVisible(newMode.HasFlag(MqttClasses.ControlModeFlags.EStop));
		_armAutonomyPanel.SetVisible(newMode.HasFlag(MqttClasses.ControlModeFlags.RoboticArmAutonomy));
		_manipulatorPanel.SetVisible(newMode.HasFlag(MqttClasses.ControlModeFlags.RoboticArm) || newMode.HasFlag(MqttClasses.ControlModeFlags.RoboticArmAutonomy));

		var totalFlag = MqttClasses.ControlModeFlags.DeepSampler | MqttClasses.ControlModeFlags.SurfaceSampler | MqttClasses.ControlModeFlags.DeepSamplerAutonomy | MqttClasses.ControlModeFlags.SurfaceSamplerAutonomy;
		_samplerPanel.SetVisible((newMode & totalFlag) != 0);
		
		return Task.CompletedTask;
	}
}
