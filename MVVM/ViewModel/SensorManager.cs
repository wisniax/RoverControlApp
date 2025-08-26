using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static RoverControlApp.Core.MqttClasses;
using RoverControlApp.MVVM.ViewModel.SensorsModels;

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class SensorManager : Panel
	{
		[Export]
		SensorDataController _surfaceWeightData = null!;
		[Export]
		SensorDataController _deepWeightData = null!;
		[Export]
		SensorDataController _rockWeightData = null!;
		[Export]
		SensorDataController _phData = null!;
		[Export]
		DistanceSensorController _distanceSensor = null!;
		[Export]
		Label _lastUpdateLabel = null!;


		SamplerFeedback _lastData = null!;

		public SensorManager()
		{
			_lastData = new SamplerFeedback();
		}

		public override void _EnterTree()
		{
			if (MqttNode.Singleton is not null) {
				MqttNode.Singleton.MessageReceivedAsync += OnSensorDataChanged;
			}
			_surfaceWeightData.Initialize("Surface Weight", 0f, 750f, "g" );
			_deepWeightData.Initialize("Deep Weight", 0f, 750f, "g");
			_rockWeightData.Initialize("Rock Weight", 0f, 750f, "g");
			_phData.Initialize( "Soil pH", 0f, 14f, "pH");
			_distanceSensor.Initialize(50f, "cm","Distance");
		}

		public override void _ExitTree()
		{
			if (MqttNode.Singleton is not null)
				MqttNode.Singleton.MessageReceivedAsync -= OnSensorDataChanged;
		}

		public async Task OnSensorDataChanged(string subTopic, MqttApplicationMessage? msg)
		{
			if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicSamplerFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicSamplerFeedback)
				return;
			if (msg is null || msg.PayloadSegment.Count == 0)
			{
				EventLogger.LogMessage("WeightSensorController", EventLogger.LogLevel.Error, "Empty payload");
				return;
			}
			SamplerFeedback? dataNullable = JsonSerializer.Deserialize<SamplerFeedback>(msg.ConvertPayloadToString());
			if (dataNullable == null)
			{
				EventLogger.LogMessage("WeightSensorController", EventLogger.LogLevel.Error, "Deserialization returned null");
				return;
			}
			_lastData = dataNullable;

			
			CallDeferred(nameof(UpdateSensorValues));

			return;
		}

		private void UpdateSensorValues()
		{
			if(_rockWeightData is null || _deepWeightData is null || _surfaceWeightData is null || _phData is null || _lastUpdateLabel is null )
			{
				EventLogger.LogMessage("SensorManager", EventLogger.LogLevel.Error, "One or more UI elements are not assigned.");
				return;
			}
			if(_lastData is null)
			{
				EventLogger.LogMessage("SensorManager", EventLogger.LogLevel.Error, "No data to update.");
				return;
			}
			_surfaceWeightData.SensorLastValue = _lastData.SurfaceWeight;
			_deepWeightData.SensorLastValue = _lastData.DeepWeight;
			_rockWeightData.SensorLastValue = _lastData.RockWeight;
			_phData.SensorLastValue = _lastData.Ph;

			_distanceSensor.SensorLastValue = _lastData.Distance;
			var dt = DateTimeOffset.FromUnixTimeMilliseconds(_lastData.Timestamp).ToLocalTime();
			_lastUpdateLabel.Text = $"Last Update: {dt:HH:mm:ss}";
		}

		public override void _Ready()
		{

		}

	}
}
