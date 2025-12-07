using MQTTnet;
using RoverControlApp.Core;
using Godot;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model
{
	public partial class MissionSetPoint : Node
	{
		private bool _disposedValue = false;
		
		public event Func<MqttClasses.ActiveKmlObjects?, Task>? ActiveKmlObjectsUpdated;
		public MqttClasses.ActiveKmlObjects? ActiveKmlObjects { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public static MissionSetPoint Singleton { get; private set; }
#pragma warning restore CS8618

		/*
		*	Godot overrides
		*/
        public override void _Ready()
        {
            base._Ready();
			MqttNode.Singleton.MessageReceivedAsync += OnMessageReceivedAsync;
			UpdateActiveKmlObjects();
			Singleton ??= this;
        }

        protected override void Dispose(bool disposing)
        {
			if(_disposedValue) return;

			if(disposing)
			{
				MqttNode.Singleton.MessageReceivedAsync -= OnMessageReceivedAsync;
			}

			_disposedValue = true;
            base.Dispose(disposing);
        }

		/*
		*	Godot overrides end
		*/

		private Task OnMessageReceivedAsync(string subtopic, MqttApplicationMessage? content)
		{
			if (subtopic != LocalSettings.Singleton.Mqtt.TopicKmlListOfActiveObj || content == null)
				return Task.CompletedTask;

			UpdateActiveKmlObjects();
			return Task.CompletedTask;
		}

		public void UpdateActiveKmlObjects()
		{
			string? msg = "";
			MqttClasses.ActiveKmlObjects? activeKmlObjects;
			try
			{ 
				msg = MqttNode.Singleton.GetReceivedMessageOnTopicAsString(LocalSettings.Singleton.Mqtt.TopicKmlListOfActiveObj);
				if (string.IsNullOrEmpty(msg)) return;
				activeKmlObjects = JsonSerializer.Deserialize<MqttClasses.ActiveKmlObjects>(msg);
			}
			catch (Exception e)
			{
				EventLogger.LogMessage("MissionSetPoint", EventLogger.LogLevel.Error, $"Deserializing failed with error: {e} while trying to deserialize message {msg}");
				return;
			}
			if (activeKmlObjects == null) return;
			ActiveKmlObjects = activeKmlObjects;
			ActiveKmlObjectsUpdated?.Invoke(activeKmlObjects);
		}

		public static MqttClasses.RoverSetPoint GenerateNewPointRequest(MqttClasses.PointType pointType, string targetStr, string description, MqttClasses.PhotoType photoType)
		{
			return new MqttClasses.RoverSetPoint()
			{
				PointType = pointType,
				Target = targetStr,
				Description = description,
				PhotoType = photoType
			};
		}

		public async Task SendNewPointRequest(MqttClasses.RoverSetPoint pointReq)
		{
			await MqttNode.Singleton.EnqueueMessageAsync
			(
				LocalSettings.Singleton.Mqtt.TopicKmlSetPoint,
				JsonSerializer.Serialize(pointReq)
			);
		}
	}
}
