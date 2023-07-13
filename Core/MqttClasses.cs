using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Onvif.Core.Client.Common;

namespace RoverControlApp.Core
{
	public class MqttClasses
	{
		public enum ControlMode
		{
			EStop = 0,
			Rover = 1,
			Manipulator = 2,
			Autonomy = 3
		}

		public class RoverStatus
		{
			public CommunicationState CommunicationState { get; set; } = CommunicationState.Closed;
			public bool PadConnected { get; set; } = false;
			public ControlMode ControlMode { get; set; } = ControlMode.EStop;
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class RoverControl
		{
			public double XVelAxis { get; set; } = 0;
			public double ZRotAxis { get; set; } = 0;
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class ManipulatorControl
		{
			public float Axis0 { get; set; } = 0;
			public float Axis1 { get; set; } = 0;
			public float Axis2 { get; set; } = 0;
			public float Axis3 { get; set; } = 0;
			public float Axis4X { get; set; } = 0;
			public float Axis4Rot { get; set; } = 0;
			public float Gripper { get; set; } = 0;
		}

		public class RoverFeedback
		{
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			public bool EStop { get; set; } = true;
			public string Status { get; set; } = string.Empty;
		}
	}
}
