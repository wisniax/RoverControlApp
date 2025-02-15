﻿using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class BatteryConverter : JsonConverter<Battery>
{
	private static readonly Battery Default = new();

	public override Battery Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");
		
		float? warningVoltage = null;
		float? criticalVoltage = null;
		float? warningTemperature = null;

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
				case nameof(Battery.WarningVoltage):
					warningVoltage = reader.GetSingle();
					break;
				case nameof(Battery.CriticalVoltage):
					criticalVoltage = reader.GetSingle();
					break;
				case nameof(Battery.WarningTemperature):
					warningTemperature = reader.GetSingle();
					break;
			}
		}

		return new Battery
		(
			warningVoltage ?? Default.WarningVoltage,
			criticalVoltage ?? Default.CriticalVoltage,
			warningTemperature ?? Default.WarningTemperature
		);
	}

	public override void Write(Utf8JsonWriter writer, Battery value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber(nameof(Battery.WarningVoltage), value.WarningVoltage);
		writer.WriteNumber(nameof(Battery.CriticalVoltage), value.CriticalVoltage);
		writer.WriteNumber(nameof(Battery.WarningTemperature), value.WarningTemperature);
		writer.WriteEndObject();
	}
}