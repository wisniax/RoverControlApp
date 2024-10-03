using System;
using Godot;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

public class GoodOldGamesLikeController : IRoverDriveController
{
	public KinematicMode Mode { get; set; } = KinematicMode.Compatibility;

	public RoverControl CalculateMoveVector()
	{
		//deadzone have to be non zero for IsEqualApprox
		var joyDeadZone = Mathf.Max(
			0.001f,
			Convert.ToSingle(LocalSettings.Singleton.Joystick.Deadzone)
		);
		Vector2 tempVel = Input.GetVector("rover_move_down", "rover_move_up", "rover_move_right", "rover_move_left", joyDeadZone);
		Vector3 velocity = new Vector3(tempVel.X, tempVel.Y, 0);

		if (LocalSettings.Singleton.SpeedLimiter.Enabled)
			velocity *= LocalSettings.Singleton.SpeedLimiter.MaxSpeed;

		velocity.X = Mathf.IsEqualApprox(velocity.X, 0f, joyDeadZone) ? 0 : velocity.X;
		velocity.Y = Mathf.IsEqualApprox(velocity.Y, 0f, joyDeadZone) ? 0 : velocity.Y;

		if (Input.IsActionPressed("camera_zoom_mod"))
			velocity /= 8f;

		return RoverControlVec3Extension.ToRoverControl(velocity);
	}
}
