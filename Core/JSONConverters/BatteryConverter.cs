using RoverControlApp.Core.Settings;
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
		int? expectedMessageInterval = null;
		bool? batteryStatusByBMS = null;
		bool? averageAll = null;
		bool? altMode = null;
		bool? showOnLow = null;

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
				case nameof(Battery.ExpectedMessageInterval):
					expectedMessageInterval = reader.GetInt32();
					break;
				case nameof(Battery.BatteryStatusByBMS):
					batteryStatusByBMS = reader.GetBoolean();
					break;
				case nameof(Battery.AverageAll):
					averageAll = reader.GetBoolean();
					break;
				case nameof(Battery.AltMode):
					altMode = reader.GetBoolean();
					break;
				case nameof(Battery.ShowOnLow):
					showOnLow = reader.GetBoolean();
					break;
			}
		}

		return new Battery
		(
			warningVoltage ?? Default.WarningVoltage,
			criticalVoltage ?? Default.CriticalVoltage,
			warningTemperature ?? Default.WarningTemperature,
			expectedMessageInterval ?? Default.ExpectedMessageInterval,
			batteryStatusByBMS ?? Default.BatteryStatusByBMS,
			averageAll ?? Default.AverageAll,
			altMode ?? Default.AltMode,
			showOnLow ?? Default.ShowOnLow
		);
	}

	public override void Write(Utf8JsonWriter writer, Battery value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber(nameof(Battery.WarningVoltage), value.WarningVoltage);
		writer.WriteNumber(nameof(Battery.CriticalVoltage), value.CriticalVoltage);
		writer.WriteNumber(nameof(Battery.WarningTemperature), value.WarningTemperature);
		writer.WriteNumber(nameof(Battery.ExpectedMessageInterval), value.ExpectedMessageInterval);
		writer.WriteBoolean(nameof(Battery.BatteryStatusByBMS), value.BatteryStatusByBMS);
		writer.WriteBoolean(nameof(Battery.AverageAll), value.AverageAll);
		writer.WriteBoolean(nameof(Battery.AltMode), value.AltMode);
		writer.WriteBoolean(nameof(Battery.ShowOnLow), value.ShowOnLow);
		writer.WriteEndObject();
	}
}
