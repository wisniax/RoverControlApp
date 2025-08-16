using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverter;

public partial class WebRTCStreamConverter : JsonConverter<WebRTCStream>
{
	private static readonly WebRTCStream Default = new();

	public override WebRTCStream? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		string? IceServers = null;
		string? SignalingServer = null;
		int? MaxBitrate = null;
		string PreferedVideoCodec = "H264";

		while(reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
				break;
				
			if (reader.TokenType != JsonTokenType.PropertyName)
				throw new JsonException("Expected property name.");
				
			string propertyName = reader.GetString()!;
			reader.Read();
			
			switch (propertyName)
			{
				case nameof(WebRTCStream.IceServer):
					IceServers = reader.GetString();
					break;
				case nameof(WebRTCStream.SignalingServer):
					SignalingServer = reader.GetString();
					break;
				case nameof(WebRTCStream.MaxBitrate):
					MaxBitrate = reader.GetInt32();
					break;
				case nameof(WebRTCStream.PreferedVideoCodec):
					PreferedVideoCodec = reader.GetString()!;
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new WebRTCStream
		(
			IceServers ?? Default.IceServer,
			SignalingServer ?? Default.SignalingServer,
			MaxBitrate ?? Default.MaxBitrate,
			PreferedVideoCodec ?? Default.PreferedVideoCodec
		);
	}

	public override void Write(Utf8JsonWriter writer, WebRTCStream value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(WebRTCStream.IceServer), value.IceServer);
		writer.WriteString(nameof(WebRTCStream.SignalingServer), value.SignalingServer);
		writer.WriteNumber(nameof(WebRTCStream.MaxBitrate), value.MaxBitrate);
		writer.WriteString(nameof(WebRTCStream.PreferedVideoCodec), value.PreferedVideoCodec);
		writer.WriteEndObject();
	}
}
