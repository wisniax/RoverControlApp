using RoverControlApp.Core.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.JSONConverters;

public class ImuBoardCanIdsConverter : JsonConverter<ImuBoardCanIds>
{
	private static readonly ImuBoardCanIds Default = new();

	public override ImuBoardCanIds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		string? frontLeft = null;
		string? frontRight = null;
		string? rearRight = null;
		string? rearLeft = null;
		string? body = null;

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
				case nameof(ImuBoardCanIds.FrontLeft):
					frontLeft = reader.GetString();
					break;
				case nameof(ImuBoardCanIds.FrontRight):
					frontRight = reader.GetString();
					break;
				case nameof(ImuBoardCanIds.RearRight):
					rearRight = reader.GetString();
					break;
				case nameof(ImuBoardCanIds.RearLeft):
					rearLeft = reader.GetString();
					break;
				case nameof(ImuBoardCanIds.Body):
					body = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new ImuBoardCanIds(
			frontLeft ?? Default.FrontLeft,
			frontRight ?? Default.FrontRight,
			rearRight ?? Default.RearRight,
			rearLeft ?? Default.RearLeft,
			body ?? Default.Body);
	}

	public override void Write(Utf8JsonWriter writer, ImuBoardCanIds value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(ImuBoardCanIds.FrontLeft), value.FrontLeft);
		writer.WriteString(nameof(ImuBoardCanIds.FrontRight), value.FrontRight);
		writer.WriteString(nameof(ImuBoardCanIds.RearRight), value.RearRight);
		writer.WriteString(nameof(ImuBoardCanIds.RearLeft), value.RearLeft);
		writer.WriteString(nameof(ImuBoardCanIds.Body), value.Body);
		writer.WriteEndObject();
	}
}
