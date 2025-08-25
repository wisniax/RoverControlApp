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

namespace RoverControlApp.MVVM.ViewModel
{
	public partial class SensorDataController : Panel
	{
		private SensorData _sensor = null!;
		float _sensorMin;
		float _sensorMax;

		private string _sensorTopic = String.Empty;
		[Export]
		private Label _sensorLabel = null!;
		[Export]
		private Label _sensorValue = null!;
		[Export]
		private Label _sensorValueMin = null!;
		[Export]
		private Label _sensorValueMax = null!;
		[Export]
		private SliderControllerStartLeft _sensorSlider = null!;
		
		 
		public string SensorUnit { get; set; } = "";

		public string SensorLabel
		{
			get => _sensor.SensorLabel;
			set
			{
				try
				{
					if(_sensor is not null)
						_sensor.SensorLabel = value;
					_sensorLabel.Text = value;
				}
				catch (Exception ex)
				{
					GD.PrintErr(ex.Message);
				}
			}
		}

		public float SensorMin
		{
			get => _sensorMin;
			set
			{
				try
				{
					_sensorMin = value;
					_sensorValueMin.Text = value.ToString();
				}
				catch (Exception ex)
				{
					GD.PrintErr(ex.Message);
				}
			}
		}

		public float SensorMax
		{
			get => _sensorMax;
			set
			{
				try
				{
					_sensorMax = value;
					_sensorValueMax.Text = value.ToString();
				}
				catch (Exception ex)
				{
					GD.PrintErr(ex.Message);
				}
			}
		}

		public float SensorLastValue
		{
			get => _sensor.SensorLastValue;
			set
			{
				try
				{
					if(_sensor is not null)
						_sensor.SensorLastValue = value;

				}
				catch (Exception ex)
				{
					GD.PrintErr(ex.Message);
				}
			}
		}


		public string SensorTopic
		{
			get => _sensorTopic;
			set
			{
				try
				{
					_sensorTopic = value;
					GD.Print($"Sensor topic set to: {_sensorTopic}");
				}
				catch (Exception ex)
				{
					GD.PrintErr(ex.Message);
				}
			}
		}

		public int VescId
		{
			get => _sensor.VescId;
			set
			{
				try
				{
					if(_sensor is not null)
						_sensor.VescId = value;
				}
				catch (Exception ex)
				{
					GD.PrintErr(ex.Message);
				}
			}
		}

		public void Initialize(string sensorLabel, float sensorMin, float sensorMax,string sensorUnit ,string sensorTopic, int vescId)
		{
			this.SensorLabel = sensorLabel;
			this.SensorMin = sensorMin;
			this.SensorMax = sensorMax;
			this.SensorUnit = sensorUnit;
			this._sensor = new SensorData(sensorLabel, vescId);
			_sensorTopic = sensorTopic;

			CallDeferred(nameof(SetSliderBounds));

			MqttNode.Singleton.MessageReceivedAsync += OnSensorDataChanged;
		}

		public SensorDataController()
		{
			this.SensorLabel = "No Name";
			this.SensorMin = 0f;
			this.SensorMax = 100f;
			this._sensor = new SensorData("No Name", 0);
			_sensorTopic = String.Empty;
		}

		public override void _EnterTree()
		{
			
		}

		public override void _ExitTree()
		{
			MqttNode.Singleton.MessageReceivedAsync -= OnSensorDataChanged;
		}

		public override void _Ready()
		{

		}

		public async Task OnSensorDataChanged(string subTopic, MqttApplicationMessage? msg)
		{
			if (string.IsNullOrEmpty(_sensorTopic) || subTopic != _sensorTopic)
				return ;
			if (msg is null || msg.PayloadSegment.Count == 0)
			{
				EventLogger.LogMessage("WeightSensorController", EventLogger.LogLevel.Error, "Empty payload");
				return ;
			}
			SensorData? dataNullable = JsonSerializer.Deserialize<SensorData>(msg.ConvertPayloadToString());
			if (dataNullable == null)
			{
				EventLogger.LogMessage("WeightSensorController", EventLogger.LogLevel.Error, "Deserialization returned null");
				return ;
			}
			SensorData data = dataNullable;
			
			if (data.SensorLabel != _sensor.SensorLabel)
				return ;
			SensorLastValue = data.SensorLastValue;

			CallDeferred(nameof(UpdateSensorDisplay));

			return ;
		}

		private void UpdateSensorDisplay()
		{
			_sensorValue.Text = $"Last value: {_sensor.SensorLastValue} {SensorUnit}";
			if (_sensorSlider is not null)
			{
				_sensorSlider.InputValue(Mathf.Clamp(_sensor.SensorLastValue, SensorMin, SensorMax));
			}
		}

		private void SetSliderBounds()
		{
			if (_sensorSlider is not null)
			{
				_sensorSlider.InputMinValue(SensorMin);
				_sensorSlider.InputMaxValue(SensorMax);
			}
		}

	}
}
