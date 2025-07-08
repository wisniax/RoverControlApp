using System.ServiceModel;
using System.Threading.Tasks;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel.MainView_Panel;

public abstract partial class TopPanelBase : Node
{
	protected bool _panelVisible = true;
	protected bool _alertAnimation = true;

	protected MqttClasses.ControlMode _roverOverlay_controlMode;
	protected MqttClasses.KinematicMode _roverOverlay_kinematicMode;
	protected float _roverOverlay_speedLimiter;
	protected UIOverlay2.AnimationAlert _roverOverlay_alertMode;

	protected UIOverlay2.AnimationAlert _phisEstopOverlay_alertMode;

	protected CommunicationState _rtspOverlay_connection;
	protected float _rtspOverlay_delay;
	protected UIOverlay2.AnimationAlert _rtspOverlay_alertMode;

	protected CommunicationState _ptzOverlay_connection;
	protected float _ptzOverlay_delay;
	protected UIOverlay2.AnimationAlert _ptzOverlay_alertMode;

	protected CommunicationState _mqttOverlay_connection;
	protected UIOverlay2.AnimationAlert _mqttOverlay_alertMode;

	[ExportGroup(".internal", "_")]
	[Export]
	protected Control _panelRoot = null!;

	[Export]
	protected UIOverlay2 _roverOverlay = null!;

	[Export]
	protected UIOverlay2 _phisEstopOverlay = null!;

	[Export]
	protected UIOverlay2 _rtspOverlay = null!;

	[Export]
	protected UIOverlay2 _ptzOverlay = null!;

	[Export]
	protected UIOverlay2 _mqttOverlay = null!;

	[Export]
	protected Button _batteryButton = null!;

	[Export]
	protected Label _batteryLabel = null!;

	protected static string BattLevel0 => " \xe92e";
	protected static string BattLevel1 => " \xe92c";
	protected static string BattLevel2 => " \xe92d";
	protected static string BattLevel3 => " \xe92b";
	protected static string BattLevelCharge => " \xe92a";
	protected static string BattLevelAltView => "\xe92e\xe90a";

	protected UIOverlay2.AnimationAlert AnimationSlow => UseSoftAnimation ? UIOverlay2.AnimationAlert.AlertSoft_Slow : UIOverlay2.AnimationAlert.AlertHard_Slow;
	protected UIOverlay2.AnimationAlert AnimationNormal => UseSoftAnimation ? UIOverlay2.AnimationAlert.AlertSoft_Normal : UIOverlay2.AnimationAlert.AlertHard_Normal;
	protected UIOverlay2.AnimationAlert AnimationFast => UseSoftAnimation ? UIOverlay2.AnimationAlert.AlertSoft_Fast : UIOverlay2.AnimationAlert.AlertHard_Fast;

	[Export]
	private BatteryMonitor? batteryMonitorNode;

	[Signal]
	public delegate void LayoutChangePressedEventHandler();

	[Export]
	public bool PanelVisible
	{
		get => _panelVisible;
		set
		{
			_panelVisible = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.PanelVisibleInternal);
			}
		}
	}

	[Export]
	public bool AlertAnimation
	{
		get => _alertAnimation;
		set
		{
			_alertAnimation = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.UpdateAlert);
			}
		}
	}

	[Export]
	public bool AnimateInSync { get; set; } = true;

	[Export]
	public bool UseSoftAnimation { get; set; } = false;

	public override void _Ready()
	{
		PressedKeys.Singleton.OnControlModeChanged += OnControlModeChanged;
		PressedKeys.Singleton.OnKinematicModeChanged += OnKinematicModeChanged;
		MqttNode.Singleton.ConnectionChanged += OnMqttConnectionChanged;
		LocalSettings.Singleton.PropagatedPropertyChanged += OnSettingsPropertyChanged;

		batteryMonitorNode?.Connect(CanvasItem.SignalName.VisibilityChanged, new Callable(this, MethodName.OnBatteryMonitorVisiblilityChange));

		OnBatteryInfo();

		if (batteryMonitorNode is null)
			EventLogger.LogMessage(nameof(TopPanelBase), EventLogger.LogLevel.Warning, "Can't access battery info!");
		else
			batteryMonitorNode.OnBatteryDataChanged += OnBatteryInfo;

		_roverOverlay_controlMode = PressedKeys.Singleton.ControlMode;
		_roverOverlay_kinematicMode = PressedKeys.Singleton.RoverMovement.Mode;
		_roverOverlay_speedLimiter = LocalSettings.Singleton.SpeedLimiter.Enabled ? LocalSettings.Singleton.SpeedLimiter.MaxSpeed : -1.0f;
		_mqttOverlay_connection = MqttNode.Singleton.ConnectionState;

		CallDeferred(MethodName.UpdateRoverOverlay);
		CallDeferred(MethodName.UpdatePhisEStopOverlay);
		CallDeferred(MethodName.UpdateRtspOverlay);
		CallDeferred(MethodName.UpdatePtzOverlay);
		CallDeferred(MethodName.UpdateMqttOverlay);
	}

	public override void _ExitTree()
	{
		batteryMonitorNode?.Disconnect(CanvasItem.SignalName.VisibilityChanged, new Callable(this, MethodName.OnBatteryMonitorVisiblilityChange));
	}

	protected override void Dispose(bool disposing)
	{
		PressedKeys.Singleton.OnControlModeChanged -= OnControlModeChanged;
		MqttNode.Singleton.ConnectionChanged -= OnMqttConnectionChanged;
		LocalSettings.Singleton.PropagatedPropertyChanged -= OnSettingsPropertyChanged;

		if (batteryMonitorNode is not null)
			batteryMonitorNode.OnBatteryDataChanged -= OnBatteryInfo;

		base.Dispose(disposing);
	}

	protected Task OnControlModeChanged(MqttClasses.ControlMode newMode)
	{
		_roverOverlay_controlMode = newMode;
		CallDeferred(MethodName.UpdateRoverOverlay);
		return Task.CompletedTask;
	}

	protected Task OnKinematicModeChanged(MqttClasses.KinematicMode newMode)
	{
		_roverOverlay_kinematicMode = newMode;
		CallDeferred(MethodName.UpdateRoverOverlay);
		return Task.CompletedTask;
	}

	protected void OnRtspConnectionChanged(CommunicationState state)
	{
		_rtspOverlay_connection = state;
		CallDeferred(MethodName.UpdateRtspOverlay);
	}

	protected void OnPtzConnectionChanged(CommunicationState state)
	{
		_ptzOverlay_connection = state;
		CallDeferred(MethodName.UpdatePtzOverlay);
	}

	protected void OnCameraDataPulse(float rtspDelay, float ptzDelay)
	{
		_rtspOverlay_delay = rtspDelay;
		_ptzOverlay_delay = ptzDelay;

		CallDeferred(MethodName.UpdateRtspOverlay);
		CallDeferred(MethodName.UpdatePtzOverlay);
	}

	protected void OnMqttConnectionChanged(CommunicationState state)
	{
		_mqttOverlay_connection = state;
		CallDeferred(MethodName.UpdateMqttOverlay);
	}

	protected void OnSettingsPropertyChanged(StringName category, StringName property, Variant oldValue, Variant newValue)
	{
		switch (category)
		{
			case nameof(LocalSettings.SpeedLimiter):
				_roverOverlay_speedLimiter = LocalSettings.Singleton.SpeedLimiter.Enabled ? LocalSettings.Singleton.SpeedLimiter.MaxSpeed : -1.0f;
				CallDeferred(MethodName.UpdateRoverOverlay);
				break;
		}
	}

	private void OnBatteryMonitorVisiblilityChange()
	{
		if(batteryMonitorNode is not null)
			_batteryButton.SetPressedNoSignal(batteryMonitorNode.Visible);
	}

	protected void OnBatteryInfo() => OnBatteryInfo(0, 0, Colors.Red);
	protected void OnBatteryInfo(int enabledBatts, int percentagesOrVolts, Color requestedColor)
	{
		bool voltsMode = enabledBatts == 0;
		string text;

		if (voltsMode)
		{
			percentagesOrVolts /= 10;
			text = string.Format("{0:0.0}V", percentagesOrVolts);

			_batteryLabel.Text = BattLevelAltView;
		}
		else
		{
			int percentDiv;
			if (LocalSettings.Singleton.Battery.AverageAll)
			{
				text = string.Format("{0:0}%|{1:0}", percentagesOrVolts, enabledBatts);
				percentDiv = 1;
			}
			else
			{
				text = string.Format("{0:0}%/{1:0}", percentagesOrVolts, enabledBatts);
				percentDiv = enabledBatts;
			}

			int percentages = percentagesOrVolts / percentDiv;
			//percent to icon
			if (percentages < 15)
				_batteryLabel.Text = BattLevel0;
			else if (percentages < 30)
				_batteryLabel.Text = BattLevel1;
			else if (percentages < 60)
				_batteryLabel.Text = BattLevel2;
			else
				_batteryLabel.Text = BattLevel3;
		}

		_batteryButton.Text = text;
		_batteryButton.AddThemeColorOverride("font_color", requestedColor);
		_batteryButton.AddThemeColorOverride("font_focus_color", requestedColor);
		_batteryButton.AddThemeColorOverride("font_pressed_color", requestedColor);
		_batteryLabel.Modulate = requestedColor;
	}

	protected void UpdateAlert()
	{
		if (!AlertAnimation)
		{
			_roverOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
			_phisEstopOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
			_rtspOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
			_ptzOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
			_mqttOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
			return;
		}

		bool changes = _roverOverlay.AlertMode != _roverOverlay_alertMode
			|| _phisEstopOverlay.AlertMode != _phisEstopOverlay_alertMode
			|| _rtspOverlay.AlertMode != _rtspOverlay_alertMode
			|| _ptzOverlay.AlertMode != _ptzOverlay_alertMode
			|| _mqttOverlay.AlertMode != _mqttOverlay_alertMode;

		if (changes && AnimateInSync)
		{
			_roverOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
			_phisEstopOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
			_rtspOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
			_ptzOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
			_mqttOverlay.AlertMode = UIOverlay2.AnimationAlert.Off;
		}

		if (changes)
		{
			_roverOverlay.AlertMode = _roverOverlay_alertMode;
			_phisEstopOverlay.AlertMode = _phisEstopOverlay_alertMode;
			_rtspOverlay.AlertMode = _rtspOverlay_alertMode;
			_ptzOverlay.AlertMode = _ptzOverlay_alertMode;
			_mqttOverlay.AlertMode = _mqttOverlay_alertMode;
		}
	}

	abstract protected string UpdateRoverOverlay_SpeedLimit();

	protected void UpdateRoverOverlay()
	{
		_roverOverlay_alertMode = UIOverlay2.AnimationAlert.Off;
		_roverOverlay.VariableTextSurfixEx = "";
		switch (_roverOverlay_controlMode)
		{
			case MqttClasses.ControlMode.EStop:
				_roverOverlay.ControlMode = 1;
				_roverOverlay_alertMode = AnimationSlow;
				break;
			case MqttClasses.ControlMode.Rover when _roverOverlay_kinematicMode == MqttClasses.KinematicMode.Ackermann:
				_roverOverlay.ControlMode = 3;
				_roverOverlay.VariableTextSurfixEx = UpdateRoverOverlay_SpeedLimit();
				break;
			case MqttClasses.ControlMode.Rover when _roverOverlay_kinematicMode == MqttClasses.KinematicMode.Crab:
				_roverOverlay.ControlMode = 4;
				_roverOverlay.VariableTextSurfixEx = UpdateRoverOverlay_SpeedLimit();
				break;
			case MqttClasses.ControlMode.Rover when _roverOverlay_kinematicMode == MqttClasses.KinematicMode.Spinner:
				_roverOverlay.ControlMode = 5;
				_roverOverlay.VariableTextSurfixEx = UpdateRoverOverlay_SpeedLimit();
				break;
			case MqttClasses.ControlMode.Rover when _roverOverlay_kinematicMode == MqttClasses.KinematicMode.EBrake:
				_roverOverlay.ControlMode = 6;
				_roverOverlay.VariableTextSurfixEx = UpdateRoverOverlay_SpeedLimit();
				break;
			case MqttClasses.ControlMode.Rover:
				_roverOverlay.ControlMode = 2;
				_roverOverlay.VariableTextSurfixEx = UpdateRoverOverlay_SpeedLimit();
				break;
			case MqttClasses.ControlMode.Manipulator:
				_roverOverlay.ControlMode = 7;
				break;
			case MqttClasses.ControlMode.Sampler:
				_roverOverlay.ControlMode = 8;
				break;
			case MqttClasses.ControlMode.Autonomy:
				_roverOverlay.ControlMode = 9;
				break;
			default:
				_roverOverlay.ControlMode = 0;
				_roverOverlay_alertMode = AnimationFast;
				break;
		}

		UpdateAlert();
	}

	protected void UpdatePhisEStopOverlay()
	{
		_phisEstopOverlay.ControlMode = 0;
		//_phisEstopOverlay_alertMode = AnimationSlow;

		UpdateAlert();
	}

	protected abstract string RtspPtzOverlay_Delay(float delay, out UIOverlay2.AnimationAlert suggestedAlert);

	protected void UpdateRtspOverlay()
	{
		string delayStr = "";
		_rtspOverlay_alertMode = AnimationSlow;
		switch (_rtspOverlay_connection)
		{
			case CommunicationState.Created:
				_rtspOverlay.ControlMode = 1;
				break;
			case CommunicationState.Opening:
				_rtspOverlay.ControlMode = 2;
				_rtspOverlay_alertMode = AnimationNormal;
				break;
			case CommunicationState.Opened:
				_rtspOverlay.ControlMode = 3;
				delayStr = RtspPtzOverlay_Delay(_rtspOverlay_delay, out _rtspOverlay_alertMode);
				break;
			case CommunicationState.Closing:
				_rtspOverlay.ControlMode = 4;
				break;
			case CommunicationState.Closed:
				_rtspOverlay.ControlMode = 5;
				_rtspOverlay_alertMode = UIOverlay2.AnimationAlert.Off;
				break;
			case CommunicationState.Faulted:
				_rtspOverlay.ControlMode = 6;
				_rtspOverlay_alertMode = AnimationFast;
				break;
			default:
				_rtspOverlay.ControlMode = 0;
				_rtspOverlay_alertMode = AnimationFast;
				break;
		}

		_rtspOverlay.VariableTextSurfixEx = delayStr;
		UpdateAlert();
	}

	protected void UpdatePtzOverlay()
	{
		string delayStr = "";
		_ptzOverlay_alertMode = AnimationSlow;
		switch (_ptzOverlay_connection)
		{
			case CommunicationState.Created:
				_ptzOverlay.ControlMode = 1;
				break;
			case CommunicationState.Opening:
				_ptzOverlay.ControlMode = 2;
				_ptzOverlay_alertMode = AnimationNormal;
				break;
			case CommunicationState.Opened:
				_ptzOverlay.ControlMode = 3;
				delayStr = RtspPtzOverlay_Delay(_ptzOverlay_delay, out _ptzOverlay_alertMode);
				break;
			case CommunicationState.Closing:
				_ptzOverlay.ControlMode = 4;
				break;
			case CommunicationState.Closed:
				_ptzOverlay.ControlMode = 5;
				_ptzOverlay_alertMode = UIOverlay2.AnimationAlert.Off;
				break;
			case CommunicationState.Faulted:
				_ptzOverlay.ControlMode = 6;
				_ptzOverlay_alertMode = AnimationFast;
				break;
			default:
				_ptzOverlay.ControlMode = 0;
				_ptzOverlay_alertMode = AnimationFast;
				break;
		}

		_ptzOverlay.VariableTextSurfixEx = delayStr;
		UpdateAlert();
	}

	protected void UpdateMqttOverlay()
	{
		_mqttOverlay_alertMode = AnimationSlow;
		switch (_mqttOverlay_connection)
		{
			case CommunicationState.Created:
				_mqttOverlay.ControlMode = 1;
				break;
			case CommunicationState.Opening:
				_mqttOverlay.ControlMode = 2;
				_mqttOverlay_alertMode = AnimationNormal;
				break;
			case CommunicationState.Opened:
				_mqttOverlay.ControlMode = 3;
				_mqttOverlay_alertMode = UIOverlay2.AnimationAlert.Off;
				break;
			case CommunicationState.Closing:
				_mqttOverlay.ControlMode = 4;
				break;
			case CommunicationState.Closed:
				_mqttOverlay.ControlMode = 5;
				break;
			case CommunicationState.Faulted:
				_mqttOverlay.ControlMode = 6;
				_mqttOverlay_alertMode = AnimationFast;
				break;
			default:
				_mqttOverlay.ControlMode = 0;
				_mqttOverlay_alertMode = AnimationFast;
				break;
		}

		UpdateAlert();
	}

	protected void PanelVisibleInternal()
	{
		_panelRoot.Visible = PanelVisible;
	}

	protected void OnLayoutChangePressed()
	{
		EmitSignal(SignalName.LayoutChangePressed);
	}
}
