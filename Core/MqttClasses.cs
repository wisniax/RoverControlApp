using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Onvif.Core.Client.Common;

namespace RoverControlApp.Core
{
    public class MqttClasses
    {
        public enum ControlMode
        {
            Rover = 0,
            Manipulator = 1
        };

        public class JoyStatus
        {
            public bool PadConnected { get; set; } = false;
            public ControlMode ControlMode { get; set; } = ControlMode.Rover;
        }

        public class RoverControl
        {
            public double XAxis { get; set; } = 0;
            public double YAxis { get; set; } = 0;
            public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public class RoverManipulator
        {
            public double Axis0 { get; set; } = 0;
            public double Axis1 { get; set; } = 0;
            public double Axis2 { get; set; } = 0;
            public double Axis3 { get; set; } = 0;
            public double Axis4X { get; set; } = 0;
            public double Axis4Rot { get; set; } = 0;
            public double Gripper { get; set; } = 0;
        }

        public class RoverFeedback
        {
	        public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			public bool EStop {get; set; } = true;
			public string Status {get; set; } = string.Empty;
		}
	}
}
