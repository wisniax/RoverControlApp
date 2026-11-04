using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class DriveMotorCanIdsConverter : JsonConverter<DriveMotorCanIds>
{
	private static readonly DriveMotorCanIds Default = new();

	public override DriveMotorCanIds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
				case nameof(DriveMotorCanIds.FrontLeft):
					frontLeft = reader.GetString();
					break;
				case nameof(DriveMotorCanIds.FrontRight):
					frontRight = reader.GetString();
					break;
				case nameof(DriveMotorCanIds.RearRight):
					rearRight = reader.GetString();
					break;
				case nameof(DriveMotorCanIds.RearLeft):
					rearLeft = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new DriveMotorCanIds(
			frontLeft ?? Default.FrontLeft,
			frontRight ?? Default.FrontRight,
			rearRight ?? Default.RearRight,
			rearLeft ?? Default.RearLeft);
	}

	public override void Write(Utf8JsonWriter writer, DriveMotorCanIds value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(DriveMotorCanIds.FrontLeft), value.FrontLeft);
		writer.WriteString(nameof(DriveMotorCanIds.FrontRight), value.FrontRight);
		writer.WriteString(nameof(DriveMotorCanIds.RearRight), value.RearRight);
		writer.WriteString(nameof(DriveMotorCanIds.RearLeft), value.RearLeft);
		writer.WriteEndObject();
	}
}
