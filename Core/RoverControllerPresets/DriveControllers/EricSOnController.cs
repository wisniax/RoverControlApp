using System;
using Godot;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

public class EricSOnController : IRoverDriveController
{
	private const float TURN_ANGLE = 89;
	public KinematicMode Mode { get; set; } = KinematicMode.Compatibility;

	public RoverControl CalculateMoveVector()
	{
		float velocity = Input.GetAxis("rover_move_backward", "rover_move_forward");
		velocity = Mathf.IsEqualApprox(velocity, 0f, 0.005f) ? 0 : velocity;

		if (LocalSettings.Singleton.SpeedLimiter.Enabled) velocity *= LocalSettings.Singleton.SpeedLimiter.MaxSpeed;

		float turn = Input.GetAxis("rover_move_right", "rover_move_left");
		turn = Mathf.IsEqualApprox(turn, 0f, Mathf.Max(0.1f, Convert.ToSingle(LocalSettings.Singleton.Joystick.Deadzone))) ? 0 : turn;

		// turn *= velocity * TURN_COEFF; // Max turn angle: 45 deg.

		// (Mathf.Abs(turn) >= 1f)
		//	velocity /= Mathf.Abs(turn);

		turn *= TURN_ANGLE * Mathf.Pi / 180;

		Vector2 vec = new Vector2(velocity, 0f).Rotated(turn);

		var maxVal = -0.0069f * Mathf.Abs(turn * 180 / Mathf.Pi) + 1;

		vec = vec.LimitLength(maxVal);

		float forcedSteer = Input.GetAxis("rover_rotate_right", "rover_rotate_left");
		if (!Mathf.IsEqualApprox(forcedSteer, 0f, 0.05f))
			vec.Y = forcedSteer / 4f;

		Vector3 vector = new Vector3(vec.X, vec.Y, 0);

		return RoverControlVec3Extension.ToRoverControl(vector);
	}
}
