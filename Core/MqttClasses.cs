using System;
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
			Interrupted = 5
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

		[Flags]
		public enum HotswapStatus
		{
			None = 0,
			Hotswap1 = 1 << 0,
			Hotswap2 = 1 << 1,
			Hotswap3 = 1 << 2,
			BlackMushroom = 1 << 3,
			GPIO1 = 1 << 4,
			GPIO2 = 1 << 5,
			GPIO3 = 1 << 6,
			GPIO4 = 1 << 7,
		}

		public enum BatterySet
		{
			Auto = 0,
			On = 1,
			Off = 2,
			//RequestData = 3
		}

		public enum MushroomStatus
		{
			Unmolded = 0,
			Molded = 1,
			NotAvailable = 2
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

		public class RoboticArmControl
		{
			public ActionType ActionType { get; set; } = ActionType.Stop;
			public string? Reference { get; set; } = "base_link";
			public ForwardKinMode? ForwardKin { get; set; }
			public InverseJoystickMode? InvJoystick { get; set; } // Joystick Control is a "ROS" name for it
			public InversePositionMode? InvPosition { get; set; }
			public bool ForceCartesian { get; set; }
			public bool ForceMovement { get; set; }
			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}
		public enum ActionType
		{
			Stop = 0,
			ForwardKin = 1,
			InvKinJoystick = 2,
			InvKinPosition = 3,
			InvKinOffset = 4,
			GoToReference = 5 // with Reference e.g. "inverse_home_pose"
							  // ... predefined positions (Driving position, sampler, etc)? Later.
		}
		public class ForwardKinMode
		{
			public float Axis1 { get; set; } = 0;
			public float Axis2 { get; set; } = 0;
			public float Axis3 { get; set; } = 0;
			public float Axis4 { get; set; } = 0;
			public float Axis5 { get; set; } = 0;
			public float Axis6 { get; set; } = 0;
		}
		public class InverseJoystickMode
		{
			public Vector3 LinearSpeed { get; set; }
			public Vector3 RotationSpeed { get; set; }
		}
		public class InversePositionMode
		{
			public Vector3 Position { get; set; }
			public Quaternion Rotation { get; set; }
		}

		public class SamplerControl
		{
			public float DrillMovement { get; set; } = 0f;
			public float PlatformMovement { get; set; } = 0f;
			public float DrillAction { get; set; } = 0f;
			public float ContainerDegrees0 { get; set; } = 0f;
			public float ContainerDegrees1 { get; set; } = 0f;
			public float ContainerDegrees2 { get; set; } = 0f;
			public float ContainerDegrees3 { get; set; } = 0f;
			public float ContainerDegrees4 { get; set; } = 0f;

			public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			public float this [int index]
			{
				get => index switch
				{
					0 => ContainerDegrees0,
					1 => ContainerDegrees1,
					2 => ContainerDegrees2,
					3 => ContainerDegrees3,
					4 => ContainerDegrees4,
					_ => throw new IndexOutOfRangeException("Container index out of range")
				};
				set
				{
					switch(index)
					{
						case 0:
							ContainerDegrees0 = value;
							break;
						case 1:
							ContainerDegrees1 = value;
							break;
						case 2:
							ContainerDegrees2 = value;
							break;
						case 3:
							ContainerDegrees3 = value;
							break;
						case 4:
							ContainerDegrees4 = value;
							break;
						default:
							throw new IndexOutOfRangeException("Container index out of range");
					}
				}
			}
		}

		public class BatteryInfo
		{
			public BatteryStatus Status { get; set; }
			public HotswapStatus HotswapStatus { get; set; }
			public int ID { get; set; }
			public int Slot { get; set; }
			public float ChargePercent { get; set; }
			public float Current { get; set; }
			public float Temperature { get; set; }
			public float Voltage { get; set; }

			public long Timestamp { get; set; }
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
