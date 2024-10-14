using System;
using Godot;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public static class RoverControlVec3Extension
{
	public static Vector3 FromRoverControl(this RoverControl roverControl) =>
		new((float)roverControl.Vel, (float)roverControl.XAxis, (float)roverControl.YAxis);

	public static RoverControl ToRoverControl(this Vector3 vector) =>
		new() { Vel = vector.X, XAxis = vector.Y, YAxis = vector.Z };
}
