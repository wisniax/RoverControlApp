using System;
using Godot;
using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

public class DirectDriveController : IRoverDriveController
{
	public float SpeedModifier => LocalSettings.Singleton.SpeedLimiter.Enabled ? LocalSettings.Singleton.SpeedLimiter.MaxSpeed : 1f;
	public KinematicMode Mode { get; set; } = KinematicMode.Ackermann;

	public RoverControl CalculateMoveVector()
	{
		//deadzone have to be non zero for IsEqualApprox
		var joyDeadZone = Mathf.Max(
			0.001f,
			Convert.ToSingle(LocalSettings.Singleton.Joystick.Deadzone)
		);

		Vector3 vec;

		switch (Mode)
		{
			case KinematicMode.Ackermann:
				vec = new(
					Input.GetAxis("rover_move_backward", "rover_move_forward"),
					Input.GetAxis("rover_move_right", "rover_move_left"),
					0);
				break;
			case KinematicMode.Crab:
				vec = new(
					Input.GetAxis("rover_move_backward", "rover_move_forward"),
					Input.GetAxis("rover_move_right", "rover_move_left"),
					Input.GetAxis("rover_move_down", "rover_move_up")
				);
				break;
			case KinematicMode.Spinner:
				vec = new(
					Input.GetAxis("rover_move_backward", "rover_move_forward"),
					0f,
					0f
				);
				break;
			case KinematicMode.EBrake:
				vec = new(0, 0, 0); //todo wheels in X pattern?
				break;
			default:
				vec = new(0, 0, 0);
				break;
		}

		vec.X *= SpeedModifier;

		vec.X = Mathf.IsEqualApprox(vec.X, 0f, joyDeadZone * SpeedModifier) ? 0 : vec.X;
		vec.Y = Mathf.IsEqualApprox(vec.Y, 0f, joyDeadZone) ? 0 : vec.Y;
		vec.Z = Mathf.IsEqualApprox(vec.Z, 0f, joyDeadZone) ? 0 : vec.Z;

		return RoverControlVec3Extension.ToRoverControl(vec);
	}
}

