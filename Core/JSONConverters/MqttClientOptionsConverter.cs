using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using RoverControlApp.Core.Settings;

namespace RoverControlApp.Core.JSONConverters;

public class MqttClientOptionsConverter : JsonConverter<MqttClientOptions>
{
	private static readonly MqttClientOptions Default = new();

	public override MqttClientOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		string? brokerIp = null;
		int? brokerPort = null;
		double? pingInterval = null;
		string? username = null;
		string? password = null;
		bool? useTls = null;
		bool? skipCAVerification = null;
		string? certificateAuthorityCertPath = null;
		string? topicMain = null;

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
				break;

			if (reader.TokenType != JsonTokenType.PropertyName)
				throw new JsonException("Expected property name.");

			string propertyName = reader.GetString()!;
			reader.Read();

			switch (propertyName)
			{
				case nameof(MqttClientOptions.BrokerIp):
					brokerIp = reader.GetString();
					break;
				case nameof(MqttClientOptions.BrokerPort):
					brokerPort = reader.GetInt32();
					break;
				case nameof(MqttClientOptions.PingInterval):
					pingInterval = reader.GetDouble();
					break;
				case nameof(MqttClientOptions.Username):
					username = reader.GetString();
					break;
				case nameof(MqttClientOptions.Password):
					password = reader.GetString();
					break;
				case nameof(MqttClientOptions.UseTls):
					useTls = reader.GetBoolean();
					break;
				case nameof(MqttClientOptions.SkipCAVerification):
					skipCAVerification = reader.GetBoolean();
					break;
				case nameof(MqttClientOptions.CertificateAuthorityCertPath):
					certificateAuthorityCertPath = reader.GetString();
					break;
				case nameof(MqttClientOptions.TopicMain):
					topicMain = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new MqttClientOptions
		(
			brokerIp ?? Default.BrokerIp,
			brokerPort ?? Default.BrokerPort,
			pingInterval ?? Default.PingInterval,
			username ?? Default.Username,
			password ?? Default.Password,
			useTls ?? Default.UseTls,
			skipCAVerification ?? Default.SkipCAVerification,
			certificateAuthorityCertPath ?? Default.CertificateAuthorityCertPath,
			topicMain ?? Default.TopicMain
		);
	}

	public override void Write(Utf8JsonWriter writer, MqttClientOptions value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(MqttClientOptions.BrokerIp), value.BrokerIp);
		writer.WriteNumber(nameof(MqttClientOptions.BrokerPort), value.BrokerPort);
		writer.WriteNumber(nameof(MqttClientOptions.PingInterval), value.PingInterval);
		writer.WriteString(nameof(MqttClientOptions.Username), value.Username);
		writer.WriteString(nameof(MqttClientOptions.Password), value.Password);
		writer.WriteBoolean(nameof(MqttClientOptions.UseTls), value.UseTls);
		writer.WriteBoolean(nameof(MqttClientOptions.SkipCAVerification), value.SkipCAVerification);
		writer.WriteString(nameof(MqttClientOptions.CertificateAuthorityCertPath), value.CertificateAuthorityCertPath);
		writer.WriteString(nameof(MqttClientOptions.TopicMain), value.TopicMain);
		writer.WriteEndObject();
	}
}
