using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using RoverControlApp.MVVM;

public partial class StatusPanelController : VBoxContainer
{
	[Export] EStopPanel _eStopPanel = null!;
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

		return Task.CompletedTask;
	}
}
