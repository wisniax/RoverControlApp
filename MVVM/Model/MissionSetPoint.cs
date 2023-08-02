using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RoverControlApp.Core;
using RoverControlApp.MVVM.ViewModel;

namespace RoverControlApp.MVVM.Model
{
	public class MissionSetPoint
	{
		private MqttClient? _mqttClient => MainViewModel.MqttClient;
		private LocalSettings.Vars? _localSettings;
		public MqttClasses.ActiveKmlObjects ActiveKmlObjects { get; private set; }

		public MissionSetPoint(LocalSettings.Vars vars)
		{
			_localSettings = vars;
		}

		public MqttClasses.RoverSetPoint GenerateNewPointRequest(MqttClasses.PointType pointType, string targetStr, string description, MqttClasses.PhotoType photoType)
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

		public MqttClasses.ActiveKmlObjects GetAvailableTargets()
		{
			var cos = new MqttClasses.ActiveKmlObjects();
			cos.area.Add("Area1");
			cos.area.Add("Area2");
			cos.poi.Add("Point1");
			cos.poi.Add("Obstacle1");
			return cos;
		}
	}
}
