using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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



		public override void _EnterTree()
		{
			_surfaceWeightData.Initialize("Surface Weight", 0f, 750f, "g", LocalSettings.Singleton.Mqtt.TopicWeightSensor, 0x85);
			_deepWeightData.Initialize("Deep Weight", 0f, 750f, "g", LocalSettings.Singleton.Mqtt.TopicWeightSensor, 0x85);
			_rockWeightData.Initialize("Rock Weight", 0f, 750f, "g", LocalSettings.Singleton.Mqtt.TopicWeightSensor, 0x85);
			_phData.Initialize( "Soil pH", 0f, 14f, "pH", LocalSettings.Singleton.Mqtt.TopicPhSensor, 0x76);
		}

		public override void _Ready()
		{

		}

	}
}
