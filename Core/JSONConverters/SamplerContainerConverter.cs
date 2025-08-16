using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class SamplerContainerConverter : JsonConverter<SamplerContainer>
{
	private static readonly SamplerContainer Default = new();

	public override SamplerContainer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		string? customName = null;
		float? position0 = null;
		float? position1 = null;
		float? position2 = null;

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
				case nameof(SamplerContainer.CustomName):
					customName = reader.GetString();
					break;
				case nameof(SamplerContainer.Position0):
					position0 = reader.GetSingle();
					break;
				case nameof(SamplerContainer.Position1):
					position1 = reader.GetSingle();
					break;
				case nameof(SamplerContainer.Position2):
					position2 = reader.GetSingle();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new SamplerContainer
		(
			customName ?? Default.CustomName,
			position0 ?? Default.Position0,
			position1 ?? Default.Position1,
			position0 ?? Default.Position2
		);
	}

	public override void Write(Utf8JsonWriter writer, SamplerContainer value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(SamplerContainer.CustomName), value.CustomName);
		writer.WriteNumber(nameof(SamplerContainer.Position0), value.Position0);
		writer.WriteNumber(nameof(SamplerContainer.Position1), value.Position1);
		writer.WriteNumber(nameof(SamplerContainer.Position2), value.Position2);
		writer.WriteEndObject();
	}
}
