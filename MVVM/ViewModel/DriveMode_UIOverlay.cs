using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class DriveMode_UIOverlay : UIOverlay
{
	[Export] 
	PanelContainer _panelContainer = null!;

	private int _inputMode; 

	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.DarkGray, Colors.LightGray, "Drive: Compatibility", "Drive: ") },
		{ 1, new(Colors.LightGreen, Colors.DarkGreen, "Drive: Ackermann","Drive: ") },
		{ 2, new(Colors.LightSalmon, Colors.DarkRed, "Drive: Crab","Drive: ") },
		{ 3, new(Colors.Yellow, Colors.Black, "Drive: Spinner","Drive: ") },
		{ 4, new(Colors.DarkBlue, Colors.LightBlue, "Drive: E-Brake","Drive: ") }
	};

	public Task KinematicModeChangedSubscriber(MqttClasses.KinematicMode newMode)
	{
		ControlMode = (int)newMode;

		return Task.CompletedTask;
	}

	public Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		_inputMode = (int)newMode;
		UpdateIndicatorVisibility();

		return Task.CompletedTask;
	}

	public override void _Ready()
	{
		base._Ready();
		ControlMode = (int)MqttClasses.KinematicMode.Ackermann;
	}

	void UpdateIndicatorVisibility()
	{
		if (_inputMode != (int)MqttClasses.ControlMode.Rover)
		{
			this.Visible = false; 
			return;
		}

		this.Visible = true;
	}
}