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
	[Export]
	ColorRect _background;

	//animation skip todo - implement animations
	private int InternalControlMode;

	public MqttClasses.KinematicMode DriveMode = MqttClasses.KinematicMode.Ackermann;

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
		DriveMode = newMode;
		UpdateDriveModeIndicator(DriveMode);

		return Task.CompletedTask;
	}

	public Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		InternalControlMode = (int)newMode;
		UpdateDriveModeIndicator(DriveMode);
		return Task.CompletedTask;
	}

	public override void _Ready()
	{
	
	}

	void UpdateDriveModeIndicator(MqttClasses.KinematicMode newMode)
	{
		if (InternalControlMode != 1) { _panelContainer.Visible = false; return; }
		_panelContainer.Visible = true;
		switch (newMode)
		{
			case MqttClasses.KinematicMode.Ackermann:
				_label.Text = "Drive: Ackermann";
				_label.AddThemeColorOverride("font_color", Colors.LightGreen);
				_background.Color = Colors.DarkGreen;
				break;
			case MqttClasses.KinematicMode.Crab:
				_label.Text = "Drive: Crab";
				_label.AddThemeColorOverride("font_color", Colors.Red);
				_background.Color = Colors.DarkRed;
				break;
			case MqttClasses.KinematicMode.Spinner:
				_label.Text = "Drive: Spinner";
				_label.AddThemeColorOverride("font_color", Colors.Black);
				_background.Color = Colors.Yellow;
				break;
			case MqttClasses.KinematicMode.EBrake:
				_label.Text = "Drive: E-Brake";
				_label.AddThemeColorOverride("font_color", Colors.LightBlue);
				_background.Color = Colors.DarkBlue;
				break;
		}
	}
}