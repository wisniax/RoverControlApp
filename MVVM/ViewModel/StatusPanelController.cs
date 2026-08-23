using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using RoverControlApp.MVVM;

public partial class StatusPanelController : VBoxContainer
{
	[Export] EStopPanel _eStopPanel = null!;
	[Export] SamplerFeedbackPanel _samplerPanel = null!;
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

		var totalFlag = MqttClasses.ControlModeFlags.DeepSampler | MqttClasses.ControlModeFlags.SurfaceSampler | MqttClasses.ControlModeFlags.DeepSamplerAutonomy | MqttClasses.ControlModeFlags.SurfaceSamplerAutonomy;
		_samplerPanel.SetVisible((newMode & totalFlag) != 0);
		
		return Task.CompletedTask;
	}
}
