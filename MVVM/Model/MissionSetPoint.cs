using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.MVVM.Model
{
    public class MissionSetPoint
	{
		public event Func<MqttClasses.ActiveKmlObjects?, Task>? ActiveKmlObjectsUpdated;
		private LocalSettings? _localSettings => MainViewModel.Settings;
		public MqttClasses.ActiveKmlObjects ActiveKmlObjects { get; private set; }

		public MissionSetPoint()
		{
			MqttNode.Singleton.MessageReceivedAsync += OnMessageReceivedAsync;
			UpdateActiveKmlObjects();
		}

		private Task OnMessageReceivedAsync(string subtopic, MqttApplicationMessage? content)
		{
			if (subtopic != _localSettings?.Mqtt.TopicKmlListOfActiveObj || content == null)
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
				msg = MqttNode.Singleton.GetReceivedMessageOnTopicAsString(_localSettings?.Mqtt.TopicKmlListOfActiveObj);
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


		[Obsolete("Use property ActiveKmlObjects instead")]
		public MqttClasses.ActiveKmlObjects GetAvailableTargets()
		{
			return ActiveKmlObjects;
			//var cos = new MqttClasses.ActiveKmlObjects();
			//cos.area = new()
			//{
			//	"Area1",
			//	"Area2"
			//};
			//cos.poi = new()
			//{
			//	"Point1",
			//	"Obstacle1"
			//};
			//cos.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			//ActiveKmlObjects = cos;
			//return cos;
		}
	}
}
