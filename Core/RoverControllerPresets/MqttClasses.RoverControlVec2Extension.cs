using System;
using Godot;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public static class RoverControlVec2Extension
{
	public static Vector2 ToVector2(this RoverControl roverControl) =>
		new((float)roverControl.XVelAxis, (float)roverControl.ZRotAxis);

	public static RoverControl FromVector2(Vector2 vector) =>
		new() { XVelAxis = vector.X, ZRotAxis = vector.Y };
}
