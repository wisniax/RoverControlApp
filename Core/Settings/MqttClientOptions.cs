using Godot;
using RoverControlApp.Core.JSONConverters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(MqttClientOptionsConverter))]
public partial class MqttClientOptions : RefCounted
{

	public MqttClientOptions()
	{
		BrokerIp = "broker.hivemq.com";
		BrokerPort = 1883;
		PingInterval = 2.5;
		TopicMain = "RappTORS";
		TopicWill = "RoverStatus";
		WillPayloadType = "RoverControlApp.Core.MqttClasses.RoverStatus";
		WillPayloadSerializedJson = "{ CommunicationState: 5 }";
	}

	public MqttClientOptions(string brokerIp, int brokerPort, double pingInterval, string topicMain, string topicWill, string willPayloadType, string willPayloadSerializedJson)
	{
		BrokerIp = brokerIp;
		BrokerPort = brokerPort;
		PingInterval = pingInterval;
		TopicMain = topicMain;
		TopicWill = topicWill;
		WillPayloadType = willPayloadType;
		WillPayloadSerializedJson = willPayloadSerializedJson;
	}

	public override string ToString()
	{
		return JsonSerializer.Serialize(this);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string BrokerIp { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;65535;1;f;i")]
	public int BrokerPort { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0.1;60;0.1;t;d")]
	public double PingInterval { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicMain { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicWill { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, allowEdit: false)]
	public string WillPayloadType { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, allowEdit: false)]
	public string WillPayloadSerializedJson { get; init; }
}
