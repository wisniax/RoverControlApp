using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using RoverControlApp.Core.Settings;

namespace RoverControlApp.Core.JSONConverters;

public class CalibrationMotorsConverter : JsonConverter<CalibrationMotor>
{
	private static readonly CalibrationMotor Default = new();

	public override CalibrationMotor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		float? minSpeed = null;
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
				case nameof(CalibrationMotor.MinSpeed):
					minSpeed = reader.GetSingle();
					break;
				case nameof(CalibrationMotor.MaxSpeed):
					maxSpeed = reader.GetSingle();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new CalibrationMotor
		(
			minSpeed ?? Default.MinSpeed,
			maxSpeed ?? Default.MaxSpeed
		);
	}

	public override void Write(Utf8JsonWriter writer, CalibrationMotor value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber(nameof(CalibrationMotor.MinSpeed), value.MinSpeed);
		writer.WriteNumber(nameof(CalibrationMotor.MaxSpeed), value.MaxSpeed);
		writer.WriteEndObject();
	}
}
