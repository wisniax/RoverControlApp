using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class CameraConnectionConverter : JsonConverter<CameraConnection>
{
	private static readonly CameraConnection Default = new(0);

	public override CameraConnection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		string? ip = null;
		string? login = null;
		string? password = null;
		string? rtspStreamPathHD = null;
		string? rtspStreamPathSD = null;
		int? rtspPort = null;
		int? ptzPort = null;

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
				case nameof(CameraConnection.Ip):
					ip = reader.GetString();
					break;
				case nameof(CameraConnection.Login):
					login = reader.GetString();
					break;
				case nameof(CameraConnection.Password):
					password = reader.GetString();
					break;
				case nameof(CameraConnection.RtspStreamPathHD):
					rtspStreamPathHD = reader.GetString();
					break;
				case nameof(CameraConnection.RtspStreamPathSD):
					rtspStreamPathSD = reader.GetString();
					break;
				case nameof(CameraConnection.RtspPort):
					rtspPort = reader.GetInt32();
					break;
				case nameof(CameraConnection.PtzPort):
					ptzPort = reader.GetInt32();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new CameraConnection
		(
			ip ?? Default.Ip,
			login ?? Default.Login,
			password ?? Default.Password,
			rtspStreamPathHD ?? Default.RtspStreamPathHD,
			rtspStreamPathSD ?? Default.RtspStreamPathSD,
			rtspPort ?? Default.RtspPort, 
			ptzPort ?? Default.PtzPort
		);
	}

	public override void Write(Utf8JsonWriter writer, CameraConnection value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(CameraConnection.Ip), value.Ip);
		writer.WriteString(nameof(CameraConnection.Login), value.Login);
		writer.WriteString(nameof(CameraConnection.Password), value.Password);
		writer.WriteString(nameof(CameraConnection.RtspStreamPathHD), value.RtspStreamPathHD);
		writer.WriteString(nameof(CameraConnection.RtspStreamPathSD), value.RtspStreamPathSD);
		writer.WriteNumber(nameof(CameraConnection.RtspPort), value.RtspPort);
		writer.WriteNumber(nameof(CameraConnection.PtzPort), value.PtzPort);
		writer.WriteEndObject();
	}
}