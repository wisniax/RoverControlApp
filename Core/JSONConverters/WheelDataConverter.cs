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

		int? maxRPM = null;

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
				case nameof(WheelData.MaxRPM):
					maxRPM = reader.GetInt32();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new WheelData
		(
			maxRPM ?? Default.MaxRPM
		);
	}

	public override void Write(Utf8JsonWriter writer, WheelData value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber(nameof(WheelData.MaxRPM), value.MaxRPM);
		writer.WriteEndObject();
	}
}
