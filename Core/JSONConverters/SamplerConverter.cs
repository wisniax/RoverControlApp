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

		int? roverDriveController = null;
		float? deadzone = null;
		bool? vibrateOnModeChange = null;
		float? containerDegreesClosed0 = null;
		float? containerDegreesOpened0 = null;
		float? containerDegreesClosed1 = null;
		float? containerDegreesOpened1 = null;
		float? containerDegreesClosed2 = null;
		float? containerDegreesOpened2 = null;

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
				case nameof(Sampler.RoverDriveController):
					roverDriveController = reader.GetInt32();
					break;
				case nameof(Sampler.Deadzone):
					deadzone = reader.GetSingle();
					break;
				case nameof(Sampler.VibrateOnModeChange):
					vibrateOnModeChange = reader.GetBoolean();
					break;
				case nameof(Sampler.ContainerDegreesClosed0):
					containerDegreesClosed0 = reader.GetSingle();
					break;
				case nameof(Sampler.ContainerDegreesOpened0):
					containerDegreesOpened0 = reader.GetSingle();
					break;
				case nameof(Sampler.ContainerDegreesClosed1):
					containerDegreesClosed1 = reader.GetSingle();
					break;
				case nameof(Sampler.ContainerDegreesOpened1):
					containerDegreesOpened1 = reader.GetSingle();
					break;
				case nameof(Sampler.ContainerDegreesClosed2):
					containerDegreesClosed2 = reader.GetSingle();
					break;
				case nameof(Sampler.ContainerDegreesOpened2):
					containerDegreesOpened2 = reader.GetSingle();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new Sampler
		(
			roverDriveController ?? Default.RoverDriveController,
			deadzone ?? Default.Deadzone,
			vibrateOnModeChange ?? Default.VibrateOnModeChange,
			containerDegreesClosed0 ?? Default.ContainerDegreesClosed0,
			containerDegreesOpened0 ?? Default.ContainerDegreesOpened0,
			containerDegreesClosed1 ?? Default.ContainerDegreesClosed1,
			containerDegreesOpened1 ?? Default.ContainerDegreesOpened1,
			containerDegreesClosed2 ?? Default.ContainerDegreesClosed2,
			containerDegreesOpened2 ?? Default.ContainerDegreesOpened2

		);
	}

	public override void Write(Utf8JsonWriter writer, Sampler value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber(nameof(Sampler.RoverDriveController), value.RoverDriveController);
		writer.WriteNumber(nameof(Sampler.Deadzone), value.Deadzone);
		writer.WriteBoolean(nameof(Sampler.VibrateOnModeChange), value.VibrateOnModeChange);
		writer.WriteNumber(nameof(Sampler.ContainerDegreesClosed0), value.ContainerDegreesClosed0);
		writer.WriteNumber(nameof(Sampler.ContainerDegreesOpened0), value.ContainerDegreesOpened0);
		writer.WriteNumber(nameof(Sampler.ContainerDegreesClosed1), value.ContainerDegreesClosed1);
		writer.WriteNumber(nameof(Sampler.ContainerDegreesOpened1), value.ContainerDegreesOpened1);
		writer.WriteNumber(nameof(Sampler.ContainerDegreesClosed2), value.ContainerDegreesClosed2);
		writer.WriteNumber(nameof(Sampler.ContainerDegreesOpened2), value.ContainerDegreesOpened2);
		writer.WriteEndObject();
	}
}