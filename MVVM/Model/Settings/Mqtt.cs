using RoverControlApp.Core;
using Godot;
using Newtonsoft.Json;
using System;

namespace RoverControlApp.MVVM.Model.Settings;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public partial class Mqtt : GodotObject, ICloneable
{
    [Signal]
	public delegate void SettingChangedEventHandler(StringName name, Variant oldValue, Variant newValue);

	public Mqtt()
	{
		_brokerIp = "broker.hivemq.com";
        _brokerPort = 1883;
        _pingInterval = 2.5;
        _topicMain = "RappTORS";
        _topicRoverControl = "RoverControl";
        _topicManipulatorControl = "ManipulatorControl";
        _topicRoverFeedback = "RoverFeedback";
        _topicRoverStatus = "RoverStatus";
        _topicRoverContainer = "RoverContainer";
        _topicMissionStatus = "MissionStatus";
        _topicKmlSetPoint = "KMLNode/SetPoint";
        _topicWheelFeedback = "wheel_feedback";
        _topicEStopStatus = "button_stop";
        _topicKmlListOfActiveObj = "KMLNode/ActiveKMLObjects";
	}

	public object Clone()
	{
		return new Mqtt()
		{
			BrokerIp = _brokerIp,
			BrokerPort = _brokerPort,
			PingInterval = _pingInterval,
			TopicMain = _topicMain,
			TopicRoverControl = _topicRoverControl,
			TopicManipulatorControl = _topicManipulatorControl,
			TopicRoverFeedback = _topicRoverFeedback,
			TopicRoverStatus = _topicRoverStatus,
			TopicRoverContainer = _topicRoverContainer,
			TopicMissionStatus = _topicMissionStatus,
			TopicKmlSetPoint = _topicKmlSetPoint,
			TopicWheelFeedback = _topicWheelFeedback,
			TopicEStopStatus = _topicEStopStatus,
			TopicKmlListOfActiveObj = _topicKmlListOfActiveObj
		};
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BrokerIp
	{
		get => _brokerIp; 
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.BrokerIp, _brokerIp, value);
			_brokerIp = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int BrokerPort
	{
		get => _brokerPort; 
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.BrokerPort, _brokerPort, value);
			_brokerPort = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0.1;60;0.1;t;d")]
	public double PingInterval
	{
		get => _pingInterval; 
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.PingInterval, _pingInterval, value);
			_pingInterval = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicMain
	{
		get => _topicMain; 
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicMain, _topicMain, value);
			_topicMain = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverControl
	{
		get => _topicRoverControl; 
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicRoverControl, _topicRoverControl, value);
			_topicRoverControl = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicManipulatorControl
	{
		get => _topicManipulatorControl;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicManipulatorControl, _topicManipulatorControl, value);
			_topicManipulatorControl = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverFeedback
	{
		get => _topicRoverFeedback;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicRoverFeedback, _topicRoverFeedback, value);
			_topicRoverFeedback = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverStatus
	{
		get => _topicRoverStatus;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicRoverStatus, _topicRoverStatus, value);
			_topicRoverStatus = value;
		}
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverContainer
	{
		get => _topicRoverContainer;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicRoverContainer, _topicRoverContainer, value);
			_topicRoverContainer = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicMissionStatus
	{
		get => _topicMissionStatus;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicMissionStatus, _topicMissionStatus, value);
			_topicMissionStatus = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicKmlSetPoint
	{
		get => _topicKmlSetPoint;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicKmlSetPoint, _topicKmlSetPoint, value);
			_topicKmlSetPoint = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicWheelFeedback
	{
		get => _topicWheelFeedback;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicWheelFeedback, _topicWheelFeedback, value);
			_topicWheelFeedback = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicEStopStatus
	{
		get => _topicEStopStatus;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicEStopStatus, _topicEStopStatus, value);
			_topicEStopStatus = value;
		}
	}

	[JsonProperty]
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicKmlListOfActiveObj
	{
		get => _topicKmlListOfActiveObj;
		set
		{
			EmitSignal(SignalName.SettingChanged, PropertyName.TopicKmlListOfActiveObj, _topicKmlListOfActiveObj, value);
			_topicKmlListOfActiveObj = value;
		}
	}

	

	string _brokerIp;
	int _brokerPort;
	double _pingInterval;
	string _topicMain;
	string _topicRoverControl;
	string _topicManipulatorControl;
	string _topicRoverFeedback;
	string _topicRoverStatus;
	string _topicRoverContainer;
	string _topicMissionStatus;
	string _topicKmlSetPoint;
	string _topicWheelFeedback;
	string _topicEStopStatus;
	string _topicKmlListOfActiveObj;
}


