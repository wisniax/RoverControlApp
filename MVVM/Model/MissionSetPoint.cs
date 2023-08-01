using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoverControlApp.Core;

namespace RoverControlApp.MVVM.Model
{
	public class MissionSetPoint
	{
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

		public bool SendNewPointRequest(MqttClasses.RoverSetPoint pointReq)
		{
			throw new NotImplementedException();
		}

		public MqttClasses.ActiveKmlObjects GetAvailableTargets()
		{
			throw new NotImplementedException();
		}
	}
}
