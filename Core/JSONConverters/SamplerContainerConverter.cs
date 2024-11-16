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
		float? closedDegrees = null;
		float? openDegrees = null;

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
				case nameof(SamplerContainer.ClosedDegrees):
					closedDegrees = reader.GetSingle();
					break;
				case nameof(SamplerContainer.OpenDegrees):
					openDegrees = reader.GetSingle();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new SamplerContainer
		(
			customName ?? Default.CustomName,
			closedDegrees ?? Default.ClosedDegrees,
			openDegrees ?? Default.OpenDegrees
		);
	}

	public override void Write(Utf8JsonWriter writer, SamplerContainer value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(SamplerContainer.CustomName), value.CustomName);
		writer.WriteNumber(nameof(SamplerContainer.ClosedDegrees), value.ClosedDegrees);
		writer.WriteNumber(nameof(SamplerContainer.OpenDegrees), value.OpenDegrees);
		writer.WriteEndObject();
	}
}