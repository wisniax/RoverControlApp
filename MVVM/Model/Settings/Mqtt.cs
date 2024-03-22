using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.MVVM.Model.Settings;

[JsonConverter(typeof(MqttConverter))]
public partial class Mqtt : SettingBase, ICloneable
{

	public Mqtt()
	{
		_clientSettings = new();

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

	public Mqtt
	(
		MqttClientOptions clientSettings,
		string topicRoverControl,
		string topicManipulatorControl,
		string topicRoverFeedback,
		string topicRoverStatus,
		string topicRoverContainer,
		string topicMissionStatus,
		string topicKmlSetPoint,
		string topicWheelFeedback,
		string topicEStopStatus,
		string topicKmlListOfActiveObj
	)
	{
		_clientSettings = clientSettings;
		_topicRoverControl = topicRoverControl;
		_topicManipulatorControl = topicManipulatorControl;
		_topicRoverFeedback = topicRoverFeedback;
		_topicRoverStatus = topicRoverStatus;
		_topicRoverContainer = topicRoverContainer;
		_topicMissionStatus = topicMissionStatus;
		_topicKmlSetPoint = topicKmlSetPoint;
		_topicWheelFeedback = topicWheelFeedback;
		_topicEStopStatus = topicEStopStatus;
		_topicKmlListOfActiveObj = topicKmlListOfActiveObj;
	}

	public object Clone()
	{
		return new Mqtt()
		{
			ClientSettings = _clientSettings,

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

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom)]
	public MqttClientOptions ClientSettings
	{
		get => _clientSettings;
		set
		{
			EmitSignal(SignalName.SectionChanged, PropertyName.ClientSettings, _clientSettings, value);
			_clientSettings = value;
		}
	}

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

	MqttClientOptions _clientSettings;

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


