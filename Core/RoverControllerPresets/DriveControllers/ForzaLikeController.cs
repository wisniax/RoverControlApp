﻿using System;
using System.Collections.Generic;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

public class ForzaLikeController : IRoverDriveController
{
	private readonly string[] _usedActions =
	[
		"rover_move_backward",
		"rover_move_forward",
		"rover_move_right",
		"rover_move_left",
		"rover_rotate_right",
		"rover_rotate_left",
	];

	public RoverControl CalculateMoveVector(in InputEvent inputEvent, in RoverControl lastState)
	{
		float velocity = Input.GetAxis("rover_move_backward", "rover_move_forward");
		velocity = Mathf.IsEqualApprox(velocity, 0f, 0.005f) ? 0 : velocity;

		if (LocalSettings.Singleton.SpeedLimiter.Enabled) velocity *= LocalSettings.Singleton.SpeedLimiter.MaxSpeed;

		float turn = Input.GetAxis("rover_move_right", "rover_move_left");
		turn = Mathf.IsEqualApprox(turn, 0f, Mathf.Max(0.1f, Convert.ToInt32(LocalSettings.Singleton.Joystick.MinimalInput))) ? 0 : turn;

		turn *= velocity; // Max turn angle: 45 deg.

		Vector3 vec = new Vector3(velocity, turn, 0);
		float forcedSteer = Input.GetAxis("rover_rotate_right", "rover_rotate_left");

		if (!Mathf.IsEqualApprox(forcedSteer, 0f, 0.05f))
			vec.Y = forcedSteer / 5f;

		var ret = vec.ToRoverControl();
		ret.Mode = OperateKinematicMode(inputEvent, lastState);

		return ret;
	}

	public KinematicMode OperateKinematicMode(in InputEvent inputEvent, in RoverControl lastState) => KinematicMode.Compatibility;

	public Dictionary<string, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
	$"""
	{nameof(IRoverDriveController)}/{nameof(ForzaLikeController)} is a legacy controller. May be unsupported.
	""";
}
