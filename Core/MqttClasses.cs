using System;
using System.ServiceModel;
using Godot;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global

namespace RoverControlApp.Core
{
	public abstract class MqttClasses
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
			public bool PadConnected { get; set; }
			public ControlMode ControlMode { get; set; } = ControlMode.EStop;
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class RoverControl
		{
			public double XVelAxis { get; set; }
			public double ZRotAxis { get; set; }
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class ManipulatorControl
		{
			public float Axis1 { get; set; }
			public float Axis2 { get; set; }
			public float Axis3 { get; set; }
			public float Axis4 { get; set; }
			public float Axis5 { get; set; }
			public float Gripper { get; set; }
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			public override bool Equals(object? obj)
			{
				if (obj is not ManipulatorControl manipObj) return false;
				bool isEqual = true;
				isEqual &= Mathf.IsEqualApprox(Axis1, manipObj.Axis1, 0.01f);
				isEqual &= Mathf.IsEqualApprox(Axis2, manipObj.Axis2, 0.01f);
				isEqual &= Mathf.IsEqualApprox(Axis3, manipObj.Axis3, 0.01f);
				isEqual &= Mathf.IsEqualApprox(Axis4, manipObj.Axis4, 0.01f);
				isEqual &= Mathf.IsEqualApprox(Axis5, manipObj.Axis5, 0.01f);
				isEqual &= Mathf.IsEqualApprox(Gripper, manipObj.Gripper, 0.01f);
				return isEqual;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Axis1, Axis2, Axis3, Axis4, Axis5, Gripper);
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
