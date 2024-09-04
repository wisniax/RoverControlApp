using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class CameraConverter : JsonConverter<Camera>
{
	private static readonly Camera Default = new(0);

	public override Camera Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		CameraConnection? connectionSettings = null;
		bool? inverseAxis = null;
		bool? enableRtspStream = null;
		bool? enablePtzControl = null;
		double? ptzRequestFrequency = null;
		bool? dontRefresh = null;

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
				case nameof(Camera.ConnectionSettings):
					connectionSettings = JsonSerializer.Deserialize<CameraConnection>(ref reader, options);
					break;
				case nameof(Camera.InverseAxis):
					inverseAxis = reader.GetBoolean();
					break;
				case nameof(Camera.EnableRtspStream):
					enableRtspStream = reader.GetBoolean();
					break;
				case nameof(Camera.EnablePtzControl):
					enablePtzControl = reader.GetBoolean();
					break;
				case nameof(Camera.PtzRequestFrequency):
					ptzRequestFrequency = reader.GetDouble();
					break;
				case nameof(Camera.DontRefresh):
					dontRefresh = reader.GetBoolean();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new Camera
		(
			connectionSettings ?? Default.ConnectionSettings,
			inverseAxis ?? Default.InverseAxis,
			enableRtspStream ?? Default.EnableRtspStream,
			enablePtzControl ?? Default.EnablePtzControl,
			ptzRequestFrequency ?? Default.PtzRequestFrequency,
			dontRefresh ?? Default.DontRefresh
		);
	}

	public override void Write(Utf8JsonWriter writer, Camera value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(nameof(Camera.ConnectionSettings));
		JsonSerializer.Serialize(writer, value.ConnectionSettings, options);
		writer.WriteBoolean(nameof(Camera.InverseAxis), value.InverseAxis);
		writer.WriteBoolean(nameof(Camera.EnableRtspStream), value.EnableRtspStream);
		writer.WriteBoolean(nameof(Camera.EnablePtzControl), value.EnablePtzControl);
		writer.WriteNumber(nameof(Camera.PtzRequestFrequency), value.PtzRequestFrequency);
		writer.WriteBoolean(nameof(Camera.DontRefresh), value.DontRefresh);
		writer.WriteEndObject();
	}
}
