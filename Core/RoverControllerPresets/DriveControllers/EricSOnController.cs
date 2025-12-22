using System;
using System.Collections.Generic;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

public class EricSOnController : IRoverDriveController
{
	private readonly StringName[] _usedActions =
	[
		RcaInEvName.RoverMoveBackward,
		RcaInEvName.RoverMoveForward,
		RcaInEvName.RoverMoveRight,
		RcaInEvName.RoverMoveLeft,
		RcaInEvName.RoverRotateRight,
		RcaInEvName.RoverRotateLeft,
	];

	private const float TURN_ANGLE = 89;

	public RoverControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in RoverControl lastState)
	{
		float velocity = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.RoverMoveBackward, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveForward, targetInputDevice));
		velocity = Mathf.IsEqualApprox(velocity, 0f, 0.005f) ? 0 : velocity;

		if (LocalSettings.Singleton.SpeedLimiter.Enabled) velocity *= LocalSettings.Singleton.SpeedLimiter.MaxSpeed;

		float turn = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.RoverMoveRight, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveLeft, targetInputDevice));
		turn = Mathf.IsEqualApprox(turn, 0f, Mathf.Max(0.1f, Convert.ToSingle(LocalSettings.Singleton.Joystick.MinimalInput))) ? 0 : turn;

		// turn *= velocity * TURN_COEFF; // Max turn angle: 45 deg.

		// (Mathf.Abs(turn) >= 1f)
		//	velocity /= Mathf.Abs(turn);

		turn *= TURN_ANGLE * Mathf.Pi / 180;

		Vector2 vec = new Vector2(velocity, 0f).Rotated(turn);

		var maxVal = -0.0069f * Mathf.Abs(turn * 180 / Mathf.Pi) + 1;

		vec = vec.LimitLength(maxVal);

		float forcedSteer = Input.GetAxis(DualSeatEvent.GetName(RcaInEvName.RoverRotateRight, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverRotateLeft, targetInputDevice));
		if (!Mathf.IsEqualApprox(forcedSteer, 0f, 0.05f))
			vec.Y = forcedSteer / 4f;

		Vector3 vector = new Vector3(vec.X, vec.Y, 0);

		var ret = vector.ToRoverControl();
		ret.Mode = OperateKinematicMode(inputEvent, targetInputDevice, lastState);

		return ret;
	}

	public KinematicMode OperateKinematicMode(in InputEvent inputEvent, DualSeatEvent.InputDevice _, in RoverControl lastState) => KinematicMode.Compatibility;

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
	$"""
	{nameof(IRoverDriveController)}/{nameof(EricSOnController)} is a legacy controller. May be unsupported.
	""";

}
