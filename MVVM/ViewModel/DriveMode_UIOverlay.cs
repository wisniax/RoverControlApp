using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class DriveMode_UIOverlay : UIOverlay
{
	[Export] 
	PanelContainer _panelContainer;
	[Export]
	Label _label;

	public enum KinematicMode
	{
		Compatibility = 0,
		Ackermann = 1,
		Crab = 2,
		Spinner = 3,
		EBrake = 4
	}

	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.DarkGray, Colors.LightGray, "Drive: Compatibility", "Drive: ") },
		{ 1, new(Colors.DarkGreen, Colors.LightGreen, "Drive: Ackermann","Drive: ") },
		{ 2, new(Colors.DarkRed, Colors.Red, "Drive: Crab","Drive: ") },
		{ 3, new(Colors.Yellow, Colors.LightYellow, "Drive: Spinner","Drive: ") },
		{ 4, new(Colors.DarkBlue, Colors.LightBlue, "Drive: E-Brake","Drive: ") }
	};

	public Task KinematicModeChangedSubscriber(MqttClasses.KinematicMode newMode)
	{
		ControlMode = (int)newMode;
		return Task.CompletedTask;
	}

	public override void _Ready()
	{
	
	}
}