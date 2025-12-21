using System.Collections.Generic;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

public partial class DriveMode_UIOverlay : UIOverlay
{
	//position under rovermode slave
	const float POSITION_LEFT = -369.0f;
	//position under rovermode master
	const float POSITION_RIGHT = -181.0f;

	[Export]
	PanelContainer _panelContainer = null!;

	private int _inputMode;
	private int _inputModeSlave;

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
		if (ControlMode == (int)newMode)
			return Task.CompletedTask;
		ControlMode = (int)newMode;

		return Task.CompletedTask;
	}

	public Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		_inputMode = (int)newMode;
		UpdateIndicatorVisibility();

		return Task.CompletedTask;
	}

	public Task SlaveControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		_inputModeSlave = (int)newMode;
		UpdateIndicatorVisibility();

		return Task.CompletedTask;
	}

	public override void _Ready()
	{
		base._Ready();

		Connect(SignalName.VisibilityChanged, Callable.From(OnVisibleChange));

		ControlMode = (int)MqttClasses.KinematicMode.Ackermann;
	}

	void UpdateIndicatorVisibility()
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
