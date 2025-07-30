using System.Text.Json;
using System.Text.Json.Serialization;

using Godot;

using RoverControlApp.Core.JSONConverters;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(MqttClientOptionsConverter))]
public partial class MqttClientOptions : RefCounted
{

	public MqttClientOptions()
	{
		BrokerIp = "broker.hivemq.com";
		BrokerPort = 1883;
		PingInterval = 2.5;
		Username = "";
		Password = "";
		UseTls = false;
		SkipCAVerification = false;
		CertificateAuthorityCertPath = "";
		TopicMain = "RappTORS";
	}

	public MqttClientOptions(string brokerIp, int brokerPort, double pingInterval, string username, string password, bool useTls, bool skipCAVerificationstring, string certificateAuthorityCertPath, string topicMain)
	{
		BrokerIp = brokerIp;
		BrokerPort = brokerPort;
		PingInterval = pingInterval;
		Username = username;
		Password = password;
		UseTls = useTls;
		SkipCAVerification = skipCAVerificationstring;
		CertificateAuthorityCertPath = certificateAuthorityCertPath;
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

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, customTooltip: "leave empty to use anonymous login")]
	public string Username { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, customTooltip: "Yes this is saved as plaintext.")]
	public string Password { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool UseTls { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool SkipCAVerification { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string CertificateAuthorityCertPath { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string TopicMain { get; init; }
}
