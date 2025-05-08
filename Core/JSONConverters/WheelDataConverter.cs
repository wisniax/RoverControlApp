using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class WheelDataConverter : JsonConverter<WheelData>
{
	private static readonly WheelData Default = new();

	public override WheelData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		string? frontLeftDrive = null;
		string? frontRightDrive = null;
		string? backRightDrive = null;
		string? backLeftDrive = null;

		string? frontLeftTurn = null;
		string? frontRightTurn = null;
		string? backRightTurn = null;
		string? backLeftTurn = null;

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
				case nameof(WheelData.FrontLeftTurn):
					frontLeftDrive = reader.GetString();
					break;
				case nameof(WheelData.FrontRightTurn):
					frontRightDrive = reader.GetString();
					break;
				case nameof(WheelData.BackRightTurn):
					backRightDrive = reader.GetString();
					break;
				case nameof(WheelData.BackLeftTurn):
					backLeftDrive = reader.GetString();
					break;
				case nameof(WheelData.FrontLeftDrive):
					frontLeftTurn = reader.GetString();
					break;
				case nameof(WheelData.FrontRightDrive):
					frontRightTurn = reader.GetString();
					break;
				case nameof(WheelData.BackRightDrive):
					backRightTurn = reader.GetString();
					break;
				case nameof(WheelData.BackLeftDrive):
					backLeftTurn = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new WheelData
		(
			frontLeftDrive ?? Default.FrontLeftDrive,
			frontRightDrive ?? Default.FrontRightDrive,
			backRightDrive ?? Default.BackRightDrive,
			backLeftDrive ?? Default.BackLeftDrive,

			frontLeftTurn ?? Default.FrontLeftTurn,
			frontRightTurn ?? Default.FrontRightTurn,
			backRightTurn ?? Default.BackRightTurn,
			backLeftTurn ?? Default.BackLeftTurn
		);
	}

	public override void Write(Utf8JsonWriter writer, WheelData value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(WheelData.FrontLeftDrive), value.FrontLeftDrive);
		writer.WriteString(nameof(WheelData.FrontLeftTurn), value.FrontLeftTurn);
		writer.WriteString(nameof(WheelData.FrontRightDrive), value.FrontRightDrive);
		writer.WriteString(nameof(WheelData.FrontRightTurn), value.FrontRightTurn);
		
		writer.WriteString(nameof(WheelData.BackLeftDrive), value.BackLeftDrive);
		writer.WriteString(nameof(WheelData.BackLeftTurn), value.BackLeftTurn);
		writer.WriteString(nameof(WheelData.BackRightDrive), value.BackRightDrive);
		writer.WriteString(nameof(WheelData.BackRightTurn), value.BackRightTurn);
		writer.WriteEndObject();
	}
}