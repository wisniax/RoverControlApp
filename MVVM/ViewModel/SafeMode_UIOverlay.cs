using System.Collections.Generic;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SafeMode_UIOverlay : UIOverlay
{
	//position under rovermode slave
	const float POSITION_LEFT = -369.0f;
	//position under rovermode master
	const float POSITION_RIGHT = -181.0f;

	private int _inputMode;
	private int _inputModeSlave;

	private static float _speedLimit => LocalSettings.Singleton.SpeedLimiter.MaxSpeed;

	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.Blue, Colors.LightBlue, $"SpeedLimiter: ON {_speedLimit:P0}", "SpeedLimiter: ") },
		{ 1, new(Colors.DarkRed, Colors.Orange, "SpeedLimiter: OFF", "SpeedLimiter: ") }

	};

	public Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		_inputMode = (int)newMode;
		CallDeferred(MethodName.UpdateSafeModeIndicator);
		return Task.CompletedTask;
	}

	public Task SlaveControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		_inputModeSlave = (int)newMode;
		CallDeferred(MethodName.UpdateSafeModeIndicator);
		return Task.CompletedTask;
	}

	public override void _Ready()
	{
		base._Ready();

		ControlMode = LocalSettings.Singleton.SpeedLimiter.Enabled ? 0 : 1;

		Connect(SignalName.VisibilityChanged, Callable.From(OnVisibleChange));

		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedPropertyChanged,
			Callable.From<StringName, StringName, Variant, Variant>(OnSettingsPropertyChanged));
		UpdateDictionary();
	}

	void OnSettingsPropertyChanged(StringName category, StringName name, Variant _, Variant newValue)
	{
		if (category != nameof(LocalSettings.SpeedLimiter))
			return;

		if (name == nameof(LocalSettings.SpeedLimiter.Enabled))
			ControlMode = ((bool)newValue) ? 0 : 1;
		else if (name == nameof(LocalSettings.SpeedLimiter.MaxSpeed))
			UpdateDictionary();
	}

	void UpdateDictionary()
	{
		Presets[0] = new(Colors.Blue, Colors.LightBlue, $"SpeedLimiter: ON {_speedLimit:P0}", "SpeedLimiter: ");
		OnSetControlMode();
	}

	void UpdateSafeModeIndicator()
	{
		this.Visible = true;

		switch ((MqttClasses.ControlMode)_inputMode)
		{
			case MqttClasses.ControlMode.Rover:
				//reanimate on position change
				if(!Mathf.IsEqualApprox(OffsetRight, POSITION_RIGHT))
					OnSetControlMode();
				OffsetRight = POSITION_RIGHT;
				break;

			case not MqttClasses.ControlMode.Rover
			when (MqttClasses.ControlMode)_inputModeSlave == MqttClasses.ControlMode.Rover:
				//reanimate on position change
				if(!Mathf.IsEqualApprox(OffsetRight, POSITION_LEFT))
					OnSetControlMode();
				OffsetRight = POSITION_LEFT;
				break;

			default:
				this.Visible = false;
				break;
		}
	}

	void OnVisibleChange()
    {
		OnSetControlMode();
    }

}
