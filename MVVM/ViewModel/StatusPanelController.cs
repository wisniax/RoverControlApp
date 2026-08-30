using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using RoverControlApp.MVVM;

public partial class StatusPanelController : VBoxContainer
{
	[Export] EStopPanel _eStopPanel = null!;
	[Export] ManipulatorPanel _manipulatorPanel = null!;
	public override void _Ready()
	{
		PressedKeys.Singleton.OnControlModeChanged += ControlModeChangedSubscriber;
	}

	public override void _ExitTree()
	{
		PressedKeys.Singleton.OnControlModeChanged -= ControlModeChangedSubscriber;
	}

	public Task ControlModeChangedSubscriber(MqttClasses.ControlModeFlags newMode)
	{
		_eStopPanel.SetVisible(newMode.HasFlag(MqttClasses.ControlModeFlags.EStop));
		_manipulatorPanel.SetVisible(newMode.HasFlag(MqttClasses.ControlModeFlags.RoboticArm) || newMode.HasFlag(MqttClasses.ControlModeFlags.RoboticArmAutonomy));

		return Task.CompletedTask;
	}
}
