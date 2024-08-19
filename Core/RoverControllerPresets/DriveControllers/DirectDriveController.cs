using System;
using Godot;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

public class DirectDriveController : IRoverDriveController
{
	public RoverControl CalculateMoveVector()
	{
		//deadzone have to be non zero for IsEqualApprox
		var joyDeadZone = Mathf.Max(
			0.001f,
			Convert.ToSingle(LocalSettings.Singleton.Joystick.Deadzone)
		);

		Vector2 vec = new(
			Input.GetAxis("rover_move_backward", "rover_move_forward"),
			Input.GetAxis("rover_move_right", "rover_move_left")
		);

		if (LocalSettings.Singleton.SpeedLimiter.Enabled)
			vec.X *= LocalSettings.Singleton.SpeedLimiter.MaxSpeed;

		vec.X = Mathf.IsEqualApprox(vec.X, 0f, joyDeadZone) ? 0 : vec.X;
		vec.Y = Mathf.IsEqualApprox(vec.Y, 0f, joyDeadZone) ? 0 : vec.Y;

		if (Input.IsActionPressed("camera_zoom_mod"))
			vec.X /= 4f;

		return RoverControlVec2Extension.FromVector2(vec);
	}
}

