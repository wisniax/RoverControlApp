using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class SamplerConverter : JsonConverter<Sampler>
{
	private static readonly Sampler Default = new();

	public override Sampler Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		SamplerContainer? container0 = null;
		SamplerContainer? container1 = null;
		SamplerContainer? container2 = null;

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
				case nameof(Sampler.Container0):
					container0 = JsonSerializer.Deserialize<SamplerContainer>(ref reader, options);
					break;
				case nameof(Sampler.Container1):
					container1 = JsonSerializer.Deserialize<SamplerContainer>(ref reader, options);
					break;
				case nameof(Sampler.Container2):
					container2 = JsonSerializer.Deserialize<SamplerContainer>(ref reader, options);
					break;
				
				default:
					reader.Skip();
					break;
			}
		}

		return new Sampler
		(
			container0 ?? Default.Container0,
			container1 ?? Default.Container1,
			container2 ?? Default.Container2
		);
	}

	public override void Write(Utf8JsonWriter writer, Sampler value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(nameof(Sampler.Container0));
		JsonSerializer.Serialize(writer, value.Container0, options);
		writer.WritePropertyName(nameof(Sampler.Container1));
		JsonSerializer.Serialize(writer, value.Container1, options);
		writer.WritePropertyName(nameof(Sampler.Container2));
		JsonSerializer.Serialize(writer, value.Container2, options);
		writer.WriteEndObject();
	}
}