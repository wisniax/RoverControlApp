using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using RoverControlApp.Core.Settings;

namespace RoverControlApp.Core.JSONConverters;

public class MqttConverter : JsonConverter<Mqtt>
{
	private static readonly Mqtt Default = new();

	public override Mqtt Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		MqttClientOptions? clientSettings = null;
		string? topicRoverControl = null;
		string? topicManipulatorControl = null;
		string? topicRoverFeedback = null;
		string? topicRoverStatus = null;
		string? topicMissionStatus = null;
		string? topicKmlSetPoint = null;
		string? topicWheelFeedback = null;
		string? topicZedImuData = null;
		string? topicEStopStatus = null;
		string? topicKmlListOfActiveObj = null;
		string? topicSampler = null;
		string? topicBatteryInfo = null;
		string? topicBatteryControl = null;

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
				case nameof(Mqtt.ClientSettings):
					clientSettings = JsonSerializer.Deserialize<MqttClientOptions>(ref reader, options);
					break;
				case nameof(Mqtt.TopicRoverControl):
					topicRoverControl = reader.GetString();
					break;
				case nameof(Mqtt.TopicManipulatorControl):
					topicManipulatorControl = reader.GetString();
					break;
				case nameof(Mqtt.TopicRoverFeedback):
					topicRoverFeedback = reader.GetString();
					break;
				case nameof(Mqtt.TopicRoverStatus):
					topicRoverStatus = reader.GetString();
					break;
				case nameof(Mqtt.TopicMissionStatus):
					topicMissionStatus = reader.GetString();
					break;
				case nameof(Mqtt.TopicKmlSetPoint):
					topicKmlSetPoint = reader.GetString();
					break;
				case nameof(Mqtt.TopicWheelFeedback):
					topicWheelFeedback = reader.GetString();
					break;
				case nameof(Mqtt.TopicEStopStatus):
					topicEStopStatus = reader.GetString();
					break;
				case nameof(Mqtt.TopicZedImuData):
					topicZedImuData = reader.GetString();
					break;
				case nameof(Mqtt.TopicKmlListOfActiveObj):
					topicKmlListOfActiveObj = reader.GetString();
					break;
				case nameof(Mqtt.TopicSamplerControl):
					topicSampler = reader.GetString();
					break;
				case nameof(Mqtt.TopicBatteryInfo):
					topicBatteryInfo = reader.GetString();
					break;
				case nameof(Mqtt.TopicBatteryControl):
					topicBatteryControl = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new Mqtt
		(
			clientSettings ?? Default.ClientSettings,
			topicRoverControl ?? Default.TopicRoverControl,
			topicManipulatorControl ?? Default.TopicManipulatorControl,
			topicRoverFeedback ?? Default.TopicRoverFeedback,
			topicRoverStatus ?? Default.TopicRoverStatus,
			topicMissionStatus ?? Default.TopicMissionStatus,
			topicKmlSetPoint ?? Default.TopicKmlSetPoint,
			topicWheelFeedback ?? Default.TopicWheelFeedback,
			topicEStopStatus ?? Default.TopicEStopStatus,
			topicZedImuData ?? Default.TopicZedImuData,
			topicKmlListOfActiveObj ?? Default.TopicKmlListOfActiveObj,
			topicSampler ?? Default.TopicSamplerControl,
			topicBatteryInfo ?? Default.TopicBatteryInfo,
			topicBatteryControl ?? Default.TopicBatteryControl
		);
	}

	public override void Write(Utf8JsonWriter writer, Mqtt value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(nameof(Mqtt.ClientSettings));
		JsonSerializer.Serialize(writer, value.ClientSettings, options);
		writer.WriteString(nameof(Mqtt.TopicRoverControl), value.TopicRoverControl);
		writer.WriteString(nameof(Mqtt.TopicManipulatorControl), value.TopicManipulatorControl);
		writer.WriteString(nameof(Mqtt.TopicRoverFeedback), value.TopicRoverFeedback);
		writer.WriteString(nameof(Mqtt.TopicRoverStatus), value.TopicRoverStatus);
		writer.WriteString(nameof(Mqtt.TopicMissionStatus), value.TopicMissionStatus);
		writer.WriteString(nameof(Mqtt.TopicKmlSetPoint), value.TopicKmlSetPoint);
		writer.WriteString(nameof(Mqtt.TopicWheelFeedback), value.TopicWheelFeedback);
		writer.WriteString(nameof(Mqtt.TopicEStopStatus), value.TopicEStopStatus);
		writer.WriteString(nameof(Mqtt.TopicZedImuData), value.TopicZedImuData);
		writer.WriteString(nameof(Mqtt.TopicKmlListOfActiveObj), value.TopicKmlListOfActiveObj);
		writer.WriteString(nameof(Mqtt.TopicSamplerControl), value.TopicSamplerControl);
		writer.WriteString(nameof(Mqtt.TopicBatteryInfo), value.TopicBatteryInfo);
		writer.WriteString(nameof(Mqtt.TopicBatteryControl), value.TopicBatteryControl);
		writer.WriteEndObject();
	}
}
