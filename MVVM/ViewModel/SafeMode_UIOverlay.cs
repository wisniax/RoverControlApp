using Godot;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SafeMode_UIOverlay : UIOverlay
{
	private int _inputMode;

	private static float _speedLimit => LocalSettings.Singleton.SpeedLimiter.MaxSpeed;

	public override Dictionary<int, Setting> Presets { get; } = new() 
	{
		{ 0, new(Colors.Blue, Colors.LightBlue, $"SpeedLimiter: ON {_speedLimit:P0}", "SpeedLimiter: ") },
		{ 1, new(Colors.DarkRed, Colors.Orange, "SpeedLimiter: OFF", "SpeedLimiter: ") }

	};

	public Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		_inputMode = (int)newMode;
		CallDeferred(MethodName.UpdateSafeModeIndicatator);
		return Task.CompletedTask;
	}

	public override void _Ready()
	{
		base._Ready();
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged,
			Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		UpdateDictionary();
	}

	void OnSettingsPropertyChanged(StringName category, StringName name, Variant oldValue, Variant newValue)
	{
		if (category != nameof(LocalSettings.SpeedLimiter))
			return;
		UpdateDictionary();

		UpdateSafeModeIndicatator();
	}

	void UpdateDictionary()
	{
		Presets[0] = new(Colors.Blue, Colors.LightBlue, $"SpeedLimiter: ON {_speedLimit:P0}", "SpeedLimiter: ");
	}

	void UpdateSafeModeIndicatator()
	{
		if (_inputMode != (int)MqttClasses.ControlMode.Rover)
		{
			this.Visible = false; 
			return;
		}
		
		this.Visible = true;
		
		ControlMode = LocalSettings.Singleton.SpeedLimiter.Enabled ? 0 : 1;
	}

}