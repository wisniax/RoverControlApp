using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using RoverControlApp.Core.Settings;

namespace RoverControlApp.Core.JSONConverters;

public class CalibrationConverter : JsonConverter<Calibration>
{
	private static readonly Calibration Default = new();

	public override Calibration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		CalibrationMotor? calibrationMotor = null;
		int? msgLimiter = null;
		float? offsetValue = null;

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
				case nameof(Calibration.CalibrationMotor):
					calibrationMotor = JsonSerializer.Deserialize<CalibrationMotor>(ref reader, options);
					break;
				case nameof(Calibration.MsgLimiter):
					msgLimiter = reader.GetInt32();
					break;
				case nameof(Calibration.OffsetValue):
					offsetValue = reader.GetSingle();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new Calibration
		(
			calibrationMotor ?? Default.CalibrationMotor,
			offsetValue ?? Default.OffsetValue,
			msgLimiter ?? Default.MsgLimiter
		);
	}

	public override void Write(Utf8JsonWriter writer, Calibration value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(nameof(Calibration.CalibrationMotor));
		JsonSerializer.Serialize(writer, value.CalibrationMotor, options);
		writer.WriteNumber(nameof(Calibration.MsgLimiter), value.MsgLimiter);
		writer.WriteNumber(nameof(Calibration.OffsetValue), value.OffsetValue);
		writer.WriteEndObject();
	}
}
