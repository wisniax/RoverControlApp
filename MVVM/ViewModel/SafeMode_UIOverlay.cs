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

	private MqttClasses.ControlModeFlags _inputModeMaster;
	private MqttClasses.ControlModeFlags _inputModeSlave;

	private static float _speedLimit => LocalSettings.Singleton.SpeedLimiter.MaxSpeed;

	public override Dictionary<int, Setting> Presets { get; } = new()
	{
		{ 0, new(Colors.Blue, Colors.LightBlue, $"SpeedLimiter: ON {_speedLimit:P0}", "SpeedLimiter: ") },
		{ 1, new(Colors.DarkRed, Colors.Orange, "SpeedLimiter: OFF", "SpeedLimiter: ") }

	};

	public Task MasterControlModeChangedSubscriber(MqttClasses.ControlModeFlags newMode)
	{
		_inputModeMaster = newMode;
		CallDeferred(MethodName.UpdateSafeModeIndicator);
		return Task.CompletedTask;
	}

	public Task SlaveControlModeChangedSubscriber(MqttClasses.ControlModeFlags newMode)
	{
		_inputModeSlave = newMode;
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

		if (_inputModeMaster.HasFlag(MqttClasses.ControlModeFlags.Drive))
		{
			if (!Mathf.IsEqualApprox(OffsetRight, POSITION_RIGHT))
				OnSetControlMode();
			OffsetRight = POSITION_RIGHT;
			return;
		}

		if (_inputModeSlave.HasFlag(MqttClasses.ControlModeFlags.Drive))
		{
			if (!Mathf.IsEqualApprox(OffsetRight, POSITION_LEFT))
				OnSetControlMode();
			OffsetRight = POSITION_LEFT;
			return;
		}
		
		this.Visible = false;
	}

	void OnVisibleChange()
    {
		OnSetControlMode();
    }

}
