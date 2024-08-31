using System;
using Godot;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

public class DirectDriveController : IRoverDriveController
{
	private MqttClasses.KinematicMode Mode = MqttClasses.KinematicMode.Ackermann;

	public RoverControl CalculateMoveVector()
	{
		//deadzone have to be non zero for IsEqualApprox
		var joyDeadZone = Mathf.Max(
			0.001f,
			Convert.ToSingle(LocalSettings.Singleton.Joystick.Deadzone)
		);

		Vector3 vec;

		if (!Input.IsActionPressed("crab_mode"))
		{
			vec = new(
				Input.GetAxis("rover_move_backward", "rover_move_forward"),
				Input.GetAxis("rover_move_right", "rover_move_left"),
				0);
			Mode = KinematicMode.Ackermann;
		}
		else
		{
			vec = new(
				0,
				Input.GetAxis("rover_move_right", "rover_move_left"),
				Input.GetAxis("rover_move_backward", "rover_move_forward")
				);
			Mode = KinematicMode.Crab;
		}


		if (LocalSettings.Singleton.SpeedLimiter.Enabled)
		{
			switch (Mode)
			{
				case KinematicMode.Ackermann:
					vec.X *= LocalSettings.Singleton.SpeedLimiter.MaxSpeed;
					break;
				case KinematicMode.Crab:
					vec.Y *= LocalSettings.Singleton.SpeedLimiter.MaxSpeed;
					vec.Z *= LocalSettings.Singleton.SpeedLimiter.MaxSpeed;
					break;
			}
		}
		

		vec.X = Mathf.IsEqualApprox(vec.X, 0f, joyDeadZone) ? 0 : vec.X;
		vec.Y = Mathf.IsEqualApprox(vec.Y, 0f, joyDeadZone) ? 0 : vec.Y;
		vec.Z = Mathf.IsEqualApprox(vec.Z, 0f, joyDeadZone) ? 0 : vec.Z;

		if (Input.IsActionPressed("camera_zoom_mod"))
			vec.X /= 4f;

		return RoverControlVec2Extension.FromVector3(vec);
	}

	public KinematicMode CheckKinematicMode()
	{
		return Mode;
	}
}

