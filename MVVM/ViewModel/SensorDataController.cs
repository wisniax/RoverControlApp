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
		private float _sensorLastValue;
		private string _sensorLabelText = String.Empty;
		float _sensorMin;
		float _sensorMax;

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
			get => _sensorLabelText;
			set
			{
				try
				{
					_sensorLabelText = value;
					if(_sensorLabel is not null)
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
					if( _sensorValueMin is not null)
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
					if( _sensorValueMax is not null)
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
			get => _sensorLastValue;
			set
			{
				try
				{
					
					_sensorLastValue = value;
					UpdateSensorDisplay();
				}
				catch (Exception ex)
				{
					GD.PrintErr(ex.Message);
				}
			}
		}

		public void Initialize(string sensorLabel, float sensorMin, float sensorMax,string sensorUnit)
		{
			this.SensorLabel = sensorLabel;
			this.SensorMin = sensorMin;
			this.SensorMax = sensorMax;
			this.SensorUnit = sensorUnit;
			
			CallDeferred(nameof(SetSliderBounds));

		}

		public SensorDataController()
		{
			this.SensorLabel = "No Name";
			this.SensorMin = 0f;
			this.SensorMax = 100f;
			this._sensorLastValue = 0f;
		}

		public override void _EnterTree()
		{
			
		}

		public override void _ExitTree()
		{

		}

		public override void _Ready()
		{

		}

		

		private void UpdateSensorDisplay()
		{
			if (_sensorValue is not null)
				_sensorValue.Text = $"Last value: {_sensorLastValue:F2} {SensorUnit}";
			if (_sensorSlider is not null)
			{
				_sensorSlider.InputValue(Mathf.Clamp(_sensorLastValue, SensorMin, SensorMax));
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
