using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SafeMode_UIOverlay : UIOverlay
{
	[Export]
	PanelContainer _panelContainer;
	[Export]
	Label _label;

	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		
	};

	public Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		if (newMode != MqttClasses.ControlMode.Rover) return Task.CompletedTask;
		
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
			_panelContainer.Visible = true;
			_label.Text = $"Safe Mode ON - {LocalSettings.Singleton.SpeedLimiter.MaxSpeed:P0}";
		}
		else
		{
			_panelContainer.Visible = false;
		}
	}

}