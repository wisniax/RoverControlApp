using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using RoverControlApp.Core.Settings;

namespace RoverControlApp.Core.JSONConverters;

public class ManipulatorConverter : JsonConverter<Manipulator>
{
	private static readonly Manipulator Default = new();

	public override Manipulator Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		int? roverManipulatorController = null;
		bool? holdToChangeManipulatorAxes = null;
		InvKinScaler? invKinScaler = null;

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
				case nameof(Manipulator.RoverManipulatorController):
					roverManipulatorController = reader.GetInt32();
					break;
				case nameof(Manipulator.HoldToChangeManipulatorAxes):
					holdToChangeManipulatorAxes = reader.GetBoolean();
					break;
				case nameof(Manipulator.InvKinScaler):
					invKinScaler = JsonSerializer.Deserialize<InvKinScaler>(ref reader, options);
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new Manipulator
		(
			roverManipulatorController ?? Default.RoverManipulatorController,
			holdToChangeManipulatorAxes ?? Default.HoldToChangeManipulatorAxes,
			invKinScaler ?? Default.InvKinScaler
		);
	}

	public override void Write(Utf8JsonWriter writer, Manipulator value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber(nameof(Manipulator.RoverManipulatorController), value.RoverManipulatorController);
		writer.WriteBoolean(nameof(Manipulator.HoldToChangeManipulatorAxes), value.HoldToChangeManipulatorAxes);
		writer.WritePropertyName(nameof(Manipulator.InvKinScaler));
		JsonSerializer.Serialize(writer, value.InvKinScaler, options);
		writer.WriteEndObject();
	}
}
