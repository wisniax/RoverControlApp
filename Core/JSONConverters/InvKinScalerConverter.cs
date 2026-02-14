using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class InvKinScalerConverter : JsonConverter<InvKinScaler>
{
	private static readonly InvKinScaler Default = new();

	public override InvKinScaler Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		float? maxLinearSpeed = null;
		float? maxAngularSpeed = null;

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
				case nameof(InvKinScaler.MaxLinearSpeed):
					maxLinearSpeed = reader.GetSingle();
					break;
				case nameof(InvKinScaler.MaxAngularSpeed):
					maxAngularSpeed = reader.GetSingle();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new InvKinScaler
		(
			maxLinearSpeed ?? Default.MaxLinearSpeed,
			maxAngularSpeed ?? Default.MaxAngularSpeed
		);
	}

	public override void Write(Utf8JsonWriter writer, InvKinScaler value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(nameof(InvKinScaler.MaxLinearSpeed));
		writer.WriteNumber(nameof(InvKinScaler.MaxLinearSpeed), value.MaxLinearSpeed);
		writer.WritePropertyName(nameof(InvKinScaler.MaxAngularSpeed));
		writer.WriteNumber(nameof(InvKinScaler.MaxAngularSpeed), value.MaxAngularSpeed);
		
		writer.WriteEndObject();
	}
}
