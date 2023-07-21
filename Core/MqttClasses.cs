using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Godot;
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
			public float Axis1 { get; set; } = 0;
			public float Axis2 { get; set; } = 0;
			public float Axis3 { get; set; } = 0;
			public float Axis4 { get; set; } = 0;
			public float Axis5 { get; set; } = 0;
			public float Gripper { get; set; } = 0;
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			public override bool Equals(object? obj)
			{
				if (obj is not ManipulatorControl manipObj) return false;
				bool isEqual = true;
				isEqual &= Mathf.IsEqualApprox(Axis1, manipObj.Axis1, 0.05f);
				isEqual &= Mathf.IsEqualApprox(Axis2, manipObj.Axis2, 0.05f);
				isEqual &= Mathf.IsEqualApprox(Axis3, manipObj.Axis3, 0.05f);
				isEqual &= Mathf.IsEqualApprox(Axis4, manipObj.Axis4, 0.05f);
				isEqual &= Mathf.IsEqualApprox(Axis5, manipObj.Axis5, 0.05f);
				isEqual &= Mathf.IsEqualApprox(Gripper, manipObj.Gripper, 0.05f);
				return isEqual;
				//return base.Equals(obj);
			}
		}

		public class RoverFeedback
		{
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			public bool EStop { get; set; } = true;
			public string Status { get; set; } = string.Empty;
		}
	}
}
