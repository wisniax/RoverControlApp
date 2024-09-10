using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class RoverMode_UIOverlay : UIOverlay
{
	[Export]
	Label _safeModeIndicatorLabel;
	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.DarkRed, Colors.Orange, "Rover: E-STOP", "Rover: ") },
		{ 1, new(Colors.DarkGreen, Colors.LightGreen, "Rover: Driving","Rover: ") },
		{ 2, new(Colors.DarkOliveGreen, Colors.LightGreen, "Rover: Manipulator","Rover: ") },
		{ 3, new (Colors.LightGreen, Colors.DarkGreen, "Rover: Sampler", "Rover:")},
		{ 4, new(Colors.DarkBlue, Colors.LightBlue, "Rover: Autonomy","Rover: ") }
	};

	public Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		ControlMode = (int)newMode;
		CallDeferred(MethodName.UpdateSafeModeIndicatator);
		return Task.CompletedTask;
	}

	public override void _Ready()
	{
		base._Ready();
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged,
			Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
	}

	void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(LocalSettings.SpeedLimiter))
			return;

		UpdateSafeModeIndicatator();
	}

	void UpdateSafeModeIndicatator()
	{
		if (ControlMode == 1 && LocalSettings.Singleton.SpeedLimiter.Enabled)
		{
			_safeModeIndicatorLabel.Visible = true;
			_safeModeIndicatorLabel.Text = $"Safe Mode ON - {LocalSettings.Singleton.SpeedLimiter.MaxSpeed:P0}";
		}
		else
		{
			_safeModeIndicatorLabel.Visible = false;
		}
	}

}
