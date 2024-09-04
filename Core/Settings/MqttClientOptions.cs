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
		BrokerIp = "192.168.1.20";
		BrokerPort = 1883;
		PingInterval = 2.5;
		TopicMain = "RappTORS";
	}

	public MqttClientOptions(string brokerIp, int brokerPort, double pingInterval, string topicMain)
	{
		BrokerIp = brokerIp;
		BrokerPort = brokerPort;
		PingInterval = pingInterval;
		TopicMain = topicMain;
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
}
