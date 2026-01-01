using System;
using System.Collections.Generic;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

public class GoodOldGamesLikeController : IRoverDriveController
{
	private readonly StringName[] _usedActions =
	[
		RcaInEvName.RoverMoveUp,
		RcaInEvName.RoverMoveDown,
		RcaInEvName.RoverMoveRight,
		RcaInEvName.RoverMoveLeft,
	];

	public RoverControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in RoverControl lastState)
	{
		//deadzone have to be non zero for IsEqualApprox
		var joyDeadZone = Mathf.Max(
			0.001f,
			Convert.ToSingle(LocalSettings.Singleton.Joystick.MinimalInput)
		);
		Vector2 tempVel = Input.GetVector(DualSeatEvent.GetName(RcaInEvName.RoverMoveDown, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveUp, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveRight, targetInputDevice), DualSeatEvent.GetName(RcaInEvName.RoverMoveLeft, targetInputDevice), joyDeadZone);
		Vector3 velocity = new Vector3(tempVel.X, tempVel.Y, 0);

		if (LocalSettings.Singleton.SpeedLimiter.Enabled)
			velocity *= LocalSettings.Singleton.SpeedLimiter.MaxSpeed;

		velocity.X = Mathf.IsEqualApprox(velocity.X, 0f, joyDeadZone) ? 0 : velocity.X;
		velocity.Y = Mathf.IsEqualApprox(velocity.Y, 0f, joyDeadZone) ? 0 : velocity.Y;

		var ret = velocity.ToRoverControl();
		ret.Mode = OperateKinematicMode(inputEvent, targetInputDevice, lastState);

		return ret;
	}

	public KinematicMode OperateKinematicMode(in InputEvent inputEvent, DualSeatEvent.InputDevice _, in RoverControl lastState) => KinematicMode.Compatibility;

	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
	$"""
	{nameof(IRoverDriveController)}/{nameof(GoodOldGamesLikeController)} is a legacy controller. May be unsupported.
	""";
}
