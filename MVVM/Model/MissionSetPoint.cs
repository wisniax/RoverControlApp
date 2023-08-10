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
		private MqttClient? _mqttClient => MainViewModel.MqttClient;
		private LocalSettings.Vars? _localSettings => MainViewModel.Settings?.Settings;
		public MqttClasses.ActiveKmlObjects ActiveKmlObjects { get; private set; }

		public MissionSetPoint()
		{
			_mqttClient.OnMessageReceivedAsync += OnMessageReceivedAsync;
			UpdateActiveKmlObjects();
		}

		private Task OnMessageReceivedAsync(string subtopic, MqttApplicationMessage? content)
		{
			if (subtopic != _localSettings?.Mqtt.TopicKmlSetPoint || content == null)
				return Task.CompletedTask;

			UpdateActiveKmlObjects();
			return Task.CompletedTask;
		}

		public void UpdateActiveKmlObjects()
		{
			MqttClasses.ActiveKmlObjects? activeKmlObjects;
			try
			{
				activeKmlObjects = JsonSerializer.Deserialize<MqttClasses.ActiveKmlObjects>(_mqttClient?.GetReceivedMessageOnTopicAsString(_localSettings?.Mqtt.TopicKmlSetPoint));
			}
			catch (Exception e)
			{
				MainViewModel.EventLogger?.LogMessage($"MissionSetPoint: Deserializing failed with error: {e}");
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
			if (_mqttClient == null || _localSettings?.Mqtt == null) return;
			await _mqttClient.EnqueueAsync(_localSettings.Mqtt.TopicKmlSetPoint,
				JsonSerializer.Serialize(pointReq));
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
