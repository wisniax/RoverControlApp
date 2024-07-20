using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class RoverMode_UIOverlay : UIOverlay
{
	[Export]
	Label SafeModeIndicator;
	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.DarkRed, Colors.Orange, "Rover: E-STOP", "Rover: ") },
		{ 1, new(Colors.DarkGreen, Colors.LightGreen, "Rover: Driving","Rover: ") },
		{ 2, new(Colors.DarkOliveGreen, Colors.LightGreen, "Rover: Manipulator","Rover: ") },
		{ 3, new(Colors.DarkBlue, Colors.LightBlue, "Rover: Autonomy","Rover: ") }
	};

	public Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		ControlMode = (int)newMode;
		CallDeferred(MethodName.UpdateSafeModeIndicatator);
		return Task.CompletedTask;
	}

	public Task SettingsAppliedSubscriber()
	{
		CallDeferred(MethodName.UpdateSafeModeIndicatator);
		return Task.CompletedTask;
	}

	void UpdateSafeModeIndicatator()
	{
		if (ControlMode == 1 && LocalSettings.Singleton.SpeedLimiter.Enabled == true && LocalSettings.Singleton.SpeedLimiter.MaxSpeed < 1)
		{
			SafeModeIndicator.Visible = true;
			SafeModeIndicator.Text = $"Safe Mode ON - {Mathf.Round(LocalSettings.Singleton.SpeedLimiter.MaxSpeed * 100.0)}%";//Rounding may seem unnecessary, but without it numbers higher than 80 would be displayed as 79.99999999999999
		}
		else
		{
			SafeModeIndicator.Visible = false;
		}
	}

}
