﻿using System;
using System.Collections.Generic;
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
			Sampler = 3,
			Autonomy = 4
		}
		public enum KinematicMode
		{
			Compatibility = 0,
			Ackermann = 1,
			Crab = 2,
			Spinner = 3,
			EBrake = 4
		}
		public enum MissionStatus
		{
			Created = 0,
			Starting = 1,
			Started = 2,
			Stopping = 3,
			Stopped = 4,
			Interrupted = 5,
			Waiting = 6
		}
		public enum PointType
		{
			Landmark = 0,
			Obstacle = 1,
			RemovePoint = 2,
			CreatePoly = 3,
			AddPointToPoly = 4,
			RemovePoly = 5,
		}
		public enum PhotoType
		{
			None = 0,
			Generic = 1,
			Spheric = 2
		}
		public enum BatteryStatus
		{
			Disconnected = 0,
			Charging = 1,
			Discharging = 2,
			Full = 3,
			Rest = 4,
			Fault = 5,
			Empty = 6
		}

		public enum HotswapStatus
		{
			OffMan = 0,
			OnMan = 1,
			OffAuto = 2,
			OnAuto = 3
		}

		public enum BatterySet
		{
			Auto = 0,
			On = 1,
			Off = 2,
			//RequestData = 3
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
			public double Vel { get; set; }
			public double XAxis { get; set; }
			public double YAxis { get; set; }
			public KinematicMode Mode { get; set; } = KinematicMode.Compatibility;
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class ManipulatorControl
		{
			public float Axis1 { get; set; } = 0f;
			public float Axis2 { get; set; } = 0f;
			public float Axis3 { get; set; } = 0f;
			public float Axis4 { get; set; } = 0f;
			public float Axis5 { get; set; } = 0f;
			public float Axis6 { get; set; } = 0f;
			public float Gripper { get; set; } = 0f;
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			public override bool Equals(object? obj)
			{
				if (obj is not ManipulatorControl manipObj) return false;
				bool isEqual = true;
				isEqual &= Mathf.IsEqualApprox(Axis1, manipObj.Axis1, 0.005f);
				isEqual &= Mathf.IsEqualApprox(Axis2, manipObj.Axis2, 0.005f);
				isEqual &= Mathf.IsEqualApprox(Axis3, manipObj.Axis3, 0.005f);
				isEqual &= Mathf.IsEqualApprox(Axis4, manipObj.Axis4, 0.005f);
				isEqual &= Mathf.IsEqualApprox(Axis5, manipObj.Axis5, 0.005f);
				isEqual &= Mathf.IsEqualApprox(Axis6, manipObj.Axis6, 0.005f);
				isEqual &= Mathf.IsEqualApprox(Gripper, manipObj.Gripper, 0.005f);
				return isEqual;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Axis1, Axis2, Axis3, Axis4, Axis5, Axis6, Gripper);
			}
		}

		public class SamplerControl
		{
			public float DrillMovement { get; set; } = 0f;
			public float PlatformMovement { get; set; } = 0f;
			public float DrillAction { get; set; } = 0f;
			public float ContainerDegrees0 { get; set; } = 0f;
			public float ContainerDegrees1 { get; set; } = 0f;
			public float ContainerDegrees2 { get; set; } = 0f;
			
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class BatteryInfo
		{
			public int Slot { get; set; }
			public int ID { get; set; }
			public float ChargePercent { get; set; }
			public float Voltage { get; set; }

			public BatteryStatus Status { get; set; }
			public HotswapStatus HotswapStatus { get; set; }
			public float Current { get; set; }
			public int Temperature { get; set; }
			public int Time { get; set; }
		}

		public class BatteryControl
		{
			public int Slot { get; set; }
			public BatterySet Set { get; set; } = BatterySet.Auto;
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class WheelFeedback
		{
			public int ERPM { get; set; }
			public int VescId { get; set; }
			public double ADC1 { get; set; }
			public double ADC2 { get; set; }
			public double ADC3 { get; set; }
			public double AhCharged { get; set; }
			public double AhUsed { get; set; }
			public double Current { get; set; }
			public double CurrentIn { get; set; }
			public double DutyCycle { get; set; }
			public double PPM { get; set; }
			public double PidPos { get; set; }
			public double PrecisePos { get; set; }
			public double Tachometer { get; set; }
			public double TempFet { get; set; }
			public double TempMotor { get; set; }
			public double VoltsIn { get; set; }
			public double WhCharged { get; set; }
			public double WhUsed { get; set; }
			public long Timestamp { get; set; }
		}

		public class RoverFeedback
		{
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			public bool EStop { get; set; } = true;
			public string Status { get; set; } = string.Empty;
		}

		public enum MissionPlannerMessageType
		{
			PointToNavigate = 0,
			StartSlashContinue = 1,
			PauseMission = 2,
			CancelMission = 3
		}

		public class MissionPlannerMessage
		{
			public float RequestedPosX { get; set; }
			public float RequestedPosY { get; set; }
			public float Deadzone { get; set; }
			public MissionPlannerMessageType MessageType { get; set; }
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class MissionPlannerFeedback
		{
			public float CurrentPosX { get; set; }
			public float CurrentPosY { get; set; }
			public MissionStatus MissionStatus { get; set; }
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class RoverMissionStatus
		{
			public MissionStatus MissionStatus { get; set; } = MissionStatus.Stopped;
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public class RoverSetPoint
		{
			public PointType PointType { get; set; }
			public string? Target { get; set; }
			public string? Description { get; set; }
			public PhotoType PhotoType { get; set; }
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}
		public class ActiveKmlObjects
		{
			public List<string> area { get; set; } = [];
			public List<string> poi { get; set; } = [];
			public long? Timestamp { get; set; }
		}
		public class AngularVelocity
		{
			public double x { get; set; }
			public double y { get; set; }
			public double z { get; set; }
		}

		public class LinearAcceleration
		{
			public double x { get; set; }
			public double y { get; set; }
			public double z { get; set; }
		}

		public class Orientation
		{
			public double x { get; set; }
			public double y { get; set; }
			public double z { get; set; }
			public double w { get; set; }
		}

		public class ZedImuData
		{
			public List<double> orientation_covariance { get; set; } = [];
			public List<double> angular_velocity_covariance { get; set; } = [];
			public List<double> linear_acceleration_covariance { get; set; } = [];
			public AngularVelocity angular_velocity { get; set; } = new();
			public LinearAcceleration linear_acceleration { get; set; } = new();
			public Orientation orientation { get; set; } = new();
			public long Timestamp { get; set; }
		}

	}
}
