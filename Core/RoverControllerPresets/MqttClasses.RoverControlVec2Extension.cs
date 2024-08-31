using System;
using Godot;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public static class RoverControlVec2Extension
{
	public static Vector3 ToVector3(this RoverControl roverControl) =>
		new((float)roverControl.Vel, (float)roverControl.XAxis, (float)roverControl.YAxis);

	public static RoverControl FromVector3(Vector3 vector) =>
		new() { Vel = vector.X, XAxis = vector.Y, YAxis = vector.Z };
}
