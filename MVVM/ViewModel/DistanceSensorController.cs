using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel.SensorsModels
{
	public partial class DistanceSensorController : Panel
	{
		float _sensorLastValue = 0f;
		float _maxValue = 100f;
		string _unit = "cm";
		string _nameLabel = "Distance Sensor";
		[Export]
		DistanceIndicator _indicator = null!;
		[Export]
		Label _currentValueLabel = null!;
		[Export]
		Label _nameLabelLabel = null!;

		public string NameLabel
		{
			get => _nameLabel;
			set
			{
				_nameLabel = value;
				if (_nameLabelLabel is not null)
					_nameLabelLabel.Text = $"{NameLabel}";
			}
		}

		public string Unit
		{
			get => _unit;
			set
			{
				_unit = value;
			}
		}

		public float MaxValue
		{
			get => _maxValue;
			set
			{
				_maxValue = value;
				if (_indicator is not null)
				{
					_indicator.MaxDistance = value;
					UpdateDisplay();
				}
			}
		}

		public float SensorLastValue
		{
			get => _sensorLastValue;
			set
			{
				_sensorLastValue = value;
				if(_indicator is not null)
					_indicator.Distance = value;
				if(_currentValueLabel is not null)
					_currentValueLabel.Text = $"Last value: {value:0.0} {Unit}";
			}
		}

		private void UpdateDisplay()
		{
			if (_currentValueLabel != null)
				_currentValueLabel.Text = $"Distance: {_sensorLastValue:F2} cm";

			if (_indicator != null)
				_indicator.Distance = (_sensorLastValue);
		}

		public void Initialize(float maxValue, string unit, string sensorName)
		{
			MaxValue = maxValue;
			Unit = unit;
			NameLabel = sensorName;

		}

	}
}
