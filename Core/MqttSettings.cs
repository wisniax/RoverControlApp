using Godot;

namespace RoverControlApp.Core;

public class MqttSettings
{
	public static readonly MqttSettings DEFAULT = new()
	{
		BrokerIp = "broker.hivemq.com",
		BrokerPort = 1883,
		PingInterval = 2.5,
		TopicMain = "RappTORS",
		TopicRoverControl = "RoverControl",
		TopicManipulatorControl = "ManipulatorControl",
		TopicRoverFeedback = "RoverFeedback",
		TopicRoverStatus = "RoverStatus",
		TopicRoverContainer = "RoverContainer",
		TopicMissionStatus = "MissionStatus",
		TopicKmlSetPoint = "KMLNode/SetPoint",
		TopicWheelFeedback = "wheel_feedback",
		TopicEStopStatus = "button_stop",
		TopicKmlListOfActiveObj = "KMLNode/ActiveKMLObjects"
	};

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BrokerIp { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int BrokerPort { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0.1;60;0.1;t;d")]
	public double PingInterval { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicMain { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverControl { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicManipulatorControl { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverFeedback { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverStatus { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicRoverContainer { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicMissionStatus { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicKmlSetPoint { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicWheelFeedback { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicEStopStatus { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicKmlListOfActiveObj { get; set; }

}


