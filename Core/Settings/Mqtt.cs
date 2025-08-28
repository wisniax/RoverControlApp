using System;
using System.Text.Json.Serialization;

using Godot;

using RoverControlApp.Core.JSONConverters;

namespace RoverControlApp.Core.Settings;

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
		_topicMissionStatus = "MissionStatus";
		_topicKmlSetPoint = "KMLNode/SetPoint";
		_topicWheelFeedback = "VescStatus";
		_topicEStopStatus = "button_stop";
		_topicZedImuData = "TopicZedImuData";
		_topicKmlListOfActiveObj = "KMLNode/ActiveKMLObjects";
		_topicSamplerControlControl = "SamplerControl";
		_topicBatteryInfo = "BatteryInfo";
		_topicBatteryControl = "BatteryControl";
		_topicMissionPlanner = "MissionPlanner";
		_topicMissionPlannerFeedback = "MissionPlannerFeedback";
		_topicSamplerFeedback = "SamplerFeedback";
	}

	public Mqtt
	(
		MqttClientOptions clientSettings,
		string topicRoverControl,
		string topicManipulatorControl,
		string topicRoverFeedback,
		string topicRoverStatus,
		string topicMissionStatus,
		string topicKmlSetPoint,
		string topicWheelFeedback,
		string topicEStopStatus,
		string topicZedImuData,
		string topicKmlListOfActiveObj,
		string topicSamplerControlControl,
		string topicBatteryInfo,
		string topicBatteryControl,
		string topicMissionPlanner,
		string topicMissionPlannerFeedback,
		string topicSamplerFeedback
	)
	{
		_clientSettings = clientSettings;
		_topicRoverControl = topicRoverControl;
		_topicManipulatorControl = topicManipulatorControl;
		_topicRoverFeedback = topicRoverFeedback;
		_topicRoverStatus = topicRoverStatus;
		_topicMissionStatus = topicMissionStatus;
		_topicKmlSetPoint = topicKmlSetPoint;
		_topicWheelFeedback = topicWheelFeedback;
		_topicEStopStatus = topicEStopStatus;
		_topicZedImuData = topicZedImuData;
		_topicKmlListOfActiveObj = topicKmlListOfActiveObj;
		_topicSamplerControlControl = topicSamplerControlControl;
		_topicBatteryInfo = topicBatteryInfo;
		_topicBatteryControl = topicBatteryControl;
		_topicMissionPlanner = topicMissionPlanner;
		_topicMissionPlannerFeedback = topicMissionPlannerFeedback;
		_topicSamplerFeedback = topicSamplerFeedback;
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
			TopicMissionStatus = _topicMissionStatus,
			TopicKmlSetPoint = _topicKmlSetPoint,
			TopicWheelFeedback = _topicWheelFeedback,
			TopicZedImuData = _topicZedImuData,
			TopicEStopStatus = _topicEStopStatus,
			TopicKmlListOfActiveObj = _topicKmlListOfActiveObj,
			TopicSamplerControl = _topicSamplerControlControl,
			TopicBatteryInfo = _topicBatteryInfo,
			TopicBatteryControl = _topicBatteryControl,
			TopicMissionPlanner = _topicMissionPlanner,
			TopicMissionPlannerFeedback = _topicMissionPlannerFeedback,
			TopicSamplerFeedback = _topicSamplerFeedback,
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public MqttClientOptions ClientSettings
	{
		get => _clientSettings;
		set => EmitSignal_SectionChanged(ref _clientSettings, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverControl
	{
		get => _topicRoverControl; 
		set => EmitSignal_SettingChanged(ref _topicRoverControl, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicManipulatorControl
	{
		get => _topicManipulatorControl;
		set => EmitSignal_SettingChanged(ref _topicManipulatorControl, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverFeedback
	{
		get => _topicRoverFeedback;
		set => EmitSignal_SettingChanged(ref _topicRoverFeedback, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverStatus
	{
		get => _topicRoverStatus;
		set => EmitSignal_SettingChanged(ref _topicRoverStatus, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicMissionStatus
	{
		get => _topicMissionStatus;
		set => EmitSignal_SettingChanged(ref _topicMissionStatus, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicKmlSetPoint
	{
		get => _topicKmlSetPoint;
		set => EmitSignal_SettingChanged(ref _topicKmlSetPoint, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicWheelFeedback
	{
		get => _topicWheelFeedback;
		set => EmitSignal_SettingChanged(ref _topicWheelFeedback, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicZedImuData
	{
		get => _topicZedImuData;
		set => EmitSignal_SettingChanged(ref _topicZedImuData, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicEStopStatus
	{
		get => _topicEStopStatus;
		set => EmitSignal_SettingChanged(ref _topicEStopStatus, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicKmlListOfActiveObj
	{
		get => _topicKmlListOfActiveObj;
		set => EmitSignal_SettingChanged(ref _topicKmlListOfActiveObj, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicSamplerControl
	{
		get => _topicSamplerControlControl;
		set => EmitSignal_SettingChanged(ref _topicSamplerControlControl, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicBatteryInfo
	{
		get => _topicBatteryInfo;
		set => EmitSignal_SettingChanged(ref _topicBatteryInfo, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicBatteryControl
	{
		get => _topicBatteryControl; 
		set => EmitSignal_SettingChanged(ref _topicBatteryControl, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicMissionPlanner
	{
		get => _topicMissionPlanner;
		set => EmitSignal_SettingChanged(ref _topicMissionPlanner, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicMissionPlannerFeedback
	{
		get => _topicMissionPlannerFeedback;
		set => EmitSignal_SettingChanged(ref _topicMissionPlannerFeedback, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicSamplerFeedback
	{
		get => _topicSamplerFeedback;
		set => EmitSignal_SettingChanged(ref _topicSamplerFeedback, value);
	}

	MqttClientOptions _clientSettings;

	string _topicRoverControl;
	string _topicManipulatorControl;
	string _topicRoverFeedback;
	string _topicRoverStatus;
	string _topicMissionStatus;
	string _topicKmlSetPoint;
	string _topicWheelFeedback;
	string _topicEStopStatus;
	string _topicZedImuData;
	string _topicKmlListOfActiveObj;
	string _topicSamplerControlControl;
	string _topicBatteryInfo;
	string _topicBatteryControl;
	string _topicMissionPlanner;
	string _topicMissionPlannerFeedback;
	string _topicSamplerFeedback;
}


