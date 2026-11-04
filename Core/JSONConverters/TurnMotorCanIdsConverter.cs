using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class TurnMotorCanIdsConverter : JsonConverter<TurnMotorCanIds>
{
	private static readonly TurnMotorCanIds Default = new();

	public override TurnMotorCanIds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		string? frontLeft = null;
		string? frontRight = null;
		string? rearRight = null;
		string? rearLeft = null;

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
				case nameof(TurnMotorCanIds.FrontLeft):
					frontLeft = reader.GetString();
					break;
				case nameof(TurnMotorCanIds.FrontRight):
					frontRight = reader.GetString();
					break;
				case nameof(TurnMotorCanIds.RearRight):
					rearRight = reader.GetString();
					break;
				case nameof(TurnMotorCanIds.RearLeft):
					rearLeft = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new TurnMotorCanIds(
			frontLeft ?? Default.FrontLeft,
			frontRight ?? Default.FrontRight,
			rearRight ?? Default.RearRight,
			rearLeft ?? Default.RearLeft);
	}

	public override void Write(Utf8JsonWriter writer, TurnMotorCanIds value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(TurnMotorCanIds.FrontLeft), value.FrontLeft);
		writer.WriteString(nameof(TurnMotorCanIds.FrontRight), value.FrontRight);
		writer.WriteString(nameof(TurnMotorCanIds.RearRight), value.RearRight);
		writer.WriteString(nameof(TurnMotorCanIds.RearLeft), value.RearLeft);
		writer.WriteEndObject();
	}
}
