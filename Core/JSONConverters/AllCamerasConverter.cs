using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class AllCamerasConverter : JsonConverter<AllCameras>
{
	private static readonly AllCameras Default = new();

	public override AllCameras Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		Camera? camera0 = null;
		Camera? camera1 = null;
		Camera? camera2 = null;
		Camera? camera3 = null;
		Camera? camera4 = null;
		Camera? camera5 = null;
		

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
				case nameof(AllCameras.Camera0):
					camera0 = JsonSerializer.Deserialize<Camera>(ref reader, options);
					break;
				case nameof(AllCameras.Camera1):
					camera1 = JsonSerializer.Deserialize<Camera>(ref reader, options);
					break;
				case nameof(AllCameras.Camera2):
					camera2 = JsonSerializer.Deserialize<Camera>(ref reader, options);
					break;
				case nameof(AllCameras.Camera3):
					camera3 = JsonSerializer.Deserialize<Camera>(ref reader, options);
					break;
				case nameof(AllCameras.Camera4):
					camera4 = JsonSerializer.Deserialize<Camera>(ref reader, options);
					break;
				case nameof(AllCameras.Camera5):
					camera5 = JsonSerializer.Deserialize<Camera>(ref reader, options);
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new AllCameras
		(
			camera0 ?? Default.Camera0,
			camera1 ?? Default.Camera1,
			camera2 ?? Default.Camera2,
			camera3 ?? Default.Camera3,
			camera4 ?? Default.Camera4,
			camera5 ?? Default.Camera5
		);
	}

	public override void Write(Utf8JsonWriter writer, AllCameras value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(nameof(AllCameras.Camera0));
		JsonSerializer.Serialize(writer, value.Camera0, options);
		writer.WritePropertyName(nameof(AllCameras.Camera1));
		JsonSerializer.Serialize(writer, value.Camera1, options);
		writer.WritePropertyName(nameof(AllCameras.Camera2));
		JsonSerializer.Serialize(writer, value.Camera2, options);
		writer.WritePropertyName(nameof(AllCameras.Camera3));
		JsonSerializer.Serialize(writer, value.Camera3, options);
		writer.WritePropertyName(nameof(AllCameras.Camera4));
		JsonSerializer.Serialize(writer, value.Camera4, options);
		writer.WritePropertyName(nameof(AllCameras.Camera5));
		JsonSerializer.Serialize(writer, value.Camera5, options);

		writer.WriteEndObject();
	}
}
