using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class SpeedLimiterConverter : JsonConverter<SpeedLimiter>
{
	static readonly SpeedLimiter @default = new();

	public override SpeedLimiter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		bool? enabled = null;
		float? maxSpeed = null;

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
				case nameof(SpeedLimiter.Enabled):
					enabled = reader.GetBoolean();
					break;
				case nameof(SpeedLimiter.MaxSpeed):
					maxSpeed = reader.GetSingle();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new SpeedLimiter
		(
			enabled ?? @default.Enabled,
			maxSpeed ?? @default.MaxSpeed
		);
	}

	public override void Write(Utf8JsonWriter writer, SpeedLimiter value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteBoolean(nameof(SpeedLimiter.Enabled), value.Enabled);
		writer.WriteNumber(nameof(SpeedLimiter.MaxSpeed), value.MaxSpeed);
		writer.WriteEndObject();
	}
}