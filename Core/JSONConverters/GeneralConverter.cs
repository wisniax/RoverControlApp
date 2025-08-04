﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using RoverControlApp.Core.Settings;

namespace RoverControlApp.Core.JSONConverters;

public class GeneralConverter : JsonConverter<General>
{
	private static readonly General Default = new();

	public override General Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected start of an object.");

		bool? verboseDebug = null;
		string? missionControlPosition = null;
		string? missionControlSize = null;
		long? backCaptureLength = null;
		int? noInputSecondsToEstop = null!;
		bool? pedanticEstop = null!;
		string? missionControlMapPath = null!;

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
				case nameof(General.VerboseDebug):
					verboseDebug = reader.GetBoolean();
					break;
				case nameof(General.MissionControlPosition):
					missionControlPosition = reader.GetString();
					break;
				case nameof(General.MissionControlSize):
					missionControlSize = reader.GetString();
					break;
				case nameof(General.BackCaptureLength):
					backCaptureLength = reader.GetInt64();
					break;
				case nameof(General.NoInputSecondsToEstop):
					noInputSecondsToEstop = reader.GetInt32();
					break;
				case nameof(General.PedanticEstop):
					pedanticEstop = reader.GetBoolean();
					break;
				case nameof(General.MissionControlMapPath):
					missionControlMapPath = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new General
		(
			verboseDebug ?? Default.VerboseDebug,
			missionControlPosition ?? Default.MissionControlPosition,
			missionControlSize ?? Default.MissionControlSize,
			backCaptureLength ?? Default.BackCaptureLength,
			noInputSecondsToEstop ?? Default.NoInputSecondsToEstop,
			pedanticEstop ?? Default.PedanticEstop,
			missionControlMapPath ?? Default.MissionControlMapPath
		);
	}

	public override void Write(Utf8JsonWriter writer, General value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteBoolean(nameof(General.VerboseDebug), value.VerboseDebug);
		writer.WriteString(nameof(General.MissionControlPosition), value.MissionControlPosition);
		writer.WriteString(nameof(General.MissionControlSize), value.MissionControlSize);
		writer.WriteNumber(nameof(General.BackCaptureLength), value.BackCaptureLength);
		writer.WriteNumber(nameof(General.NoInputSecondsToEstop), value.NoInputSecondsToEstop);
		writer.WriteBoolean(nameof(General.PedanticEstop), value.PedanticEstop);
		writer.WriteString(nameof(General.MissionControlMapPath), value.MissionControlMapPath);
		writer.WriteEndObject();
	}
}
