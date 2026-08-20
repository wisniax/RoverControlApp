using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class RosoutConverter : JsonConverter<Rosout>
{
	private static readonly Rosout Default = new();

	public override Rosout Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		bool? message = null;
		bool? function = null;
		bool? file = null;
		bool? line = null;

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
				case nameof(Rosout.Message):
					message = reader.GetBoolean();
					break;
				case nameof(Rosout.Function):
					function = reader.GetBoolean();
					break;
				case nameof(Rosout.File):
					file = reader.GetBoolean();
					break;
				case nameof(Rosout.Line):
					line = reader.GetBoolean();
					break;
				default:
					reader.Skip(); // Ignoruj nieznane pola, jeśli pojawią się w pliku
					break;
			}
		}

		return new Rosout
		(
			message ?? Default.Message,
			function ?? Default.Function,
			file ?? Default.File,
			line ?? Default.Line
		);
	}

	public override void Write(Utf8JsonWriter writer, Rosout value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteBoolean(nameof(Rosout.Message), value.Message);
		writer.WriteBoolean(nameof(Rosout.Function), value.Function);
		writer.WriteBoolean(nameof(Rosout.File), value.File);
		writer.WriteBoolean(nameof(Rosout.Line), value.Line);
		writer.WriteEndObject();
	}
}
