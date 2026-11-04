using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class ManipulatorCanIdsConverter : JsonConverter<ManipulatorCanIds>
{
	private static readonly ManipulatorCanIds Default = new();

	public override ManipulatorCanIds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		string? axis1 = null;
		string? axis2 = null;
		string? axis3 = null;
		string? axis4 = null;
		string? axis5 = null;
		string? axis6 = null;
		string? gripper = null;

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
				case nameof(ManipulatorCanIds.Axis1):
					axis1 = reader.GetString();
					break;
				case nameof(ManipulatorCanIds.Axis2):
					axis2 = reader.GetString();
					break;
				case nameof(ManipulatorCanIds.Axis3):
					axis3 = reader.GetString();
					break;
				case nameof(ManipulatorCanIds.Axis4):
					axis4 = reader.GetString();
					break;
				case nameof(ManipulatorCanIds.Axis5):
					axis5 = reader.GetString();
					break;
				case nameof(ManipulatorCanIds.Axis6):
					axis6 = reader.GetString();
					break;
				case nameof(ManipulatorCanIds.Gripper):
					gripper = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new ManipulatorCanIds(
			axis1 ?? Default.Axis1,
			axis2 ?? Default.Axis2,
			axis3 ?? Default.Axis3,
			axis4 ?? Default.Axis4,
			axis5 ?? Default.Axis5,
			axis6 ?? Default.Axis6,
			gripper ?? Default.Gripper);
	}

	public override void Write(Utf8JsonWriter writer, ManipulatorCanIds value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(ManipulatorCanIds.Axis1), value.Axis1);
		writer.WriteString(nameof(ManipulatorCanIds.Axis2), value.Axis2);
		writer.WriteString(nameof(ManipulatorCanIds.Axis3), value.Axis3);
		writer.WriteString(nameof(ManipulatorCanIds.Axis4), value.Axis4);
		writer.WriteString(nameof(ManipulatorCanIds.Axis5), value.Axis5);
		writer.WriteString(nameof(ManipulatorCanIds.Axis6), value.Axis6);
		writer.WriteString(nameof(ManipulatorCanIds.Gripper), value.Gripper);
		writer.WriteEndObject();
	}
}
