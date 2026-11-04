using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class CanIdsConverter : JsonConverter<CanIds>
{
	public override CanIds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		DriveMotorCanIds? driveMotors = null;
		TurnMotorCanIds? turnMotors = null;
		ImuBoardCanIds? imuBoards = null;
		ManipulatorCanIds? manipulator = null;
		SamplerCanIds? sampler = null;

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
				case nameof(CanIds.DriveMotors):
					driveMotors = JsonSerializer.Deserialize<DriveMotorCanIds>(ref reader, options);
					break;
				case nameof(CanIds.TurnMotors):
					turnMotors = JsonSerializer.Deserialize<TurnMotorCanIds>(ref reader, options);
					break;
				case nameof(CanIds.ImuBoards):
					imuBoards = JsonSerializer.Deserialize<ImuBoardCanIds>(ref reader, options);
					break;
				case nameof(CanIds.Manipulator):
					manipulator = JsonSerializer.Deserialize<ManipulatorCanIds>(ref reader, options);
					break;
				case nameof(CanIds.Sampler):
					sampler = JsonSerializer.Deserialize<SamplerCanIds>(ref reader, options);
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new CanIds(driveMotors ?? new(), turnMotors ?? new(), imuBoards ?? new(), manipulator ?? new(), sampler ?? new());
	}

	public override void Write(Utf8JsonWriter writer, CanIds value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(nameof(CanIds.DriveMotors));
		JsonSerializer.Serialize(writer, value.DriveMotors, options);
		writer.WritePropertyName(nameof(CanIds.TurnMotors));
		JsonSerializer.Serialize(writer, value.TurnMotors, options);
		writer.WritePropertyName(nameof(CanIds.ImuBoards));
		JsonSerializer.Serialize(writer, value.ImuBoards, options);
		writer.WritePropertyName(nameof(CanIds.Manipulator));
		JsonSerializer.Serialize(writer, value.Manipulator, options);
		writer.WritePropertyName(nameof(CanIds.Sampler));
		JsonSerializer.Serialize(writer, value.Sampler, options);
		writer.WriteEndObject();
	}
}
