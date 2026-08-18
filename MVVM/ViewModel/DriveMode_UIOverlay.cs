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

	private MqttClasses.ControlModeFlags _inputModeMaster;
	private MqttClasses.ControlModeFlags _inputModeSlave;

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

	public Task ControlModeChangedSubscriber(MqttClasses.ControlModeFlags newMasterMode)
	{
		_inputModeMaster = newMasterMode;
		UpdateIndicatorVisibility();

		return Task.CompletedTask;
	}

	public Task SlaveControlModeChangedSubscriber(MqttClasses.ControlModeFlags newSlaveMode)
	{
		_inputModeSlave = newSlaveMode;
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

		if (_inputModeMaster.HasFlag(MqttClasses.ControlModeFlags.Drive))
		{
			//reanimate on position change
			if (!Mathf.IsEqualApprox(OffsetRight, POSITION_RIGHT))
				OnSetControlMode();
			OffsetRight = POSITION_RIGHT;

			return;
		}

		//reanimate on position change
		if (_inputModeSlave.HasFlag(MqttClasses.ControlModeFlags.Drive))
		{
			if (!Mathf.IsEqualApprox(OffsetRight, POSITION_LEFT))
				OnSetControlMode();
			OffsetRight = POSITION_LEFT;
			return;
		}

		this.Visible = false;
		return;
	}

	void OnVisibleChange()
    {
		OnSetControlMode();
    }
}
