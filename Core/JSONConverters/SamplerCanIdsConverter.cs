using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class SamplerCanIdsConverter : JsonConverter<SamplerCanIds>
{
	private static readonly SamplerCanIds Default = new();

	public override SamplerCanIds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		string? platform = null;
		string? drillMovement = null;
		string? drill = null;
		string? containerA = null;
		string? containerB = null;
		string? vacuumSuction = null;
		string? vacuumA = null;
		string? vacuumB = null;
		int? expectedFeedbackInterval = null;

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
				case nameof(SamplerCanIds.Platform):
					platform = reader.GetString();
					break;
				case nameof(SamplerCanIds.DrillMovement):
					drillMovement = reader.GetString();
					break;
				case nameof(SamplerCanIds.Drill):
					drill = reader.GetString();
					break;
				case nameof(SamplerCanIds.ContainerA):
					containerA = reader.GetString();
					break;
				case nameof(SamplerCanIds.ContainerB):
					containerB = reader.GetString();
					break;
				case nameof(SamplerCanIds.VacuumSuction):
					vacuumSuction = reader.GetString();
					break;
				case nameof(SamplerCanIds.VacuumA):
					vacuumA = reader.GetString();
					break;
				case nameof(SamplerCanIds.VacuumB):
					vacuumB = reader.GetString();
					break;
				case nameof(SamplerCanIds.ExpectedFeedbackInterval):
					expectedFeedbackInterval = reader.GetInt32();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new SamplerCanIds(
			platform ?? Default.Platform,
			drillMovement ?? Default.DrillMovement,
			drill ?? Default.Drill,
			containerA ?? Default.ContainerA,
			containerB ?? Default.ContainerB,
			vacuumSuction ?? Default.VacuumSuction,
			vacuumA ?? Default.VacuumA,
			vacuumB ?? Default.VacuumB,
			expectedFeedbackInterval ?? Default.ExpectedFeedbackInterval);
	}

	public override void Write(Utf8JsonWriter writer, SamplerCanIds value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(SamplerCanIds.Platform), value.Platform);
		writer.WriteString(nameof(SamplerCanIds.DrillMovement), value.DrillMovement);
		writer.WriteString(nameof(SamplerCanIds.Drill), value.Drill);
		writer.WriteString(nameof(SamplerCanIds.ContainerA), value.ContainerA);
		writer.WriteString(nameof(SamplerCanIds.ContainerB), value.ContainerB);
		writer.WriteString(nameof(SamplerCanIds.VacuumSuction), value.VacuumSuction);
		writer.WriteString(nameof(SamplerCanIds.VacuumA), value.VacuumA);
		writer.WriteString(nameof(SamplerCanIds.VacuumB), value.VacuumB);
		writer.WriteNumber(nameof(SamplerCanIds.ExpectedFeedbackInterval), value.ExpectedFeedbackInterval);
		writer.WriteEndObject();
	}
}
