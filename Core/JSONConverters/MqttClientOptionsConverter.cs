using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

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
		string? topicMain = null;
		string? topicWill = null;
		string? willPayloadType = null;
		string? willPayloadSerializedJson = null;

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
				case nameof(MqttClientOptions.TopicMain):
					topicMain = reader.GetString();
					break;
				case nameof(MqttClientOptions.TopicWill):
					topicWill = reader.GetString();
					break;
				case nameof(MqttClientOptions.WillPayloadType):
					willPayloadType = reader.GetString();
					break;
				case nameof(MqttClientOptions.WillPayloadSerializedJson):
					willPayloadSerializedJson = reader.GetString();
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
			topicMain ?? Default.TopicMain,
			topicWill ?? Default.TopicWill,
			willPayloadType ?? Default.WillPayloadType,
			willPayloadSerializedJson ?? Default.WillPayloadSerializedJson
		);
	}

	public override void Write(Utf8JsonWriter writer, MqttClientOptions value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(MqttClientOptions.BrokerIp), value.BrokerIp);
		writer.WriteNumber(nameof(MqttClientOptions.BrokerPort), value.BrokerPort);
		writer.WriteNumber(nameof(MqttClientOptions.PingInterval), value.PingInterval);
		writer.WriteString(nameof(MqttClientOptions.TopicMain), value.TopicMain);
		writer.WriteString(nameof(MqttClientOptions.TopicWill), value.TopicWill);
		writer.WriteString(nameof(MqttClientOptions.WillPayloadType), value.WillPayloadType);
		writer.WriteString(nameof(MqttClientOptions.WillPayloadSerializedJson), value.WillPayloadSerializedJson);
		writer.WriteEndObject();
	}
}