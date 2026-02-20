using Godot;
using RoverControlApp.MVVM.Model;
using System;
using System.Collections.Generic;

namespace RoverControlApp.Core.RoverControllerPresets.DriveControllers;

// Zmiana velocity podczas trzymania velocity na padzie

public class CalibrateAxisController : IRoverCalibrateController
{
	// For Joystick Options
	private float primaryThreshold = 0.8f;    // primary axis must reach this to trigger
	private float centerDeadzone = 0.15f;     // when both axes are within this, we reset
	private bool actionTriggered = false;     // prevents repeated triggers until center
	private float lastBumperValue = 0f; 

	private readonly StringName[] _usedActions =
	[
		RcaInEvName.CalibrateRotateLeft,
		RcaInEvName.CalibrateRotateRight,

		RcaInEvName.CalibrateRotateLeftOnce,
		RcaInEvName.CalibrateRotateRightOnce,

		RcaInEvName.CalibrateAxisNext,
		RcaInEvName.CalibrateAxisBack,

		RcaInEvName.CalibrateActionTop,
		RcaInEvName.CalibrateActionBottom,
		RcaInEvName.CalibrateActionLeft,
		RcaInEvName.CalibrateActionRight
	];

	public bool HandleInput(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice)
	{
		// Making sure to hold or press wheel to activate the Calibration Mode
		if (!OperateMode(inputEvent, targetInputDevice))
			return false;

		// Making sure the panel is visible. Making it more safe, to not calibrate if don't wanted to
		if (LocalSettingsMemory.Singleton.CalibrateAxis.PanelVisibilty != true)
			return false;

		// Handlers
		RotateBumper(inputEvent, targetInputDevice);
		RotateOnce(inputEvent, targetInputDevice);
		ChangeAxis(inputEvent, targetInputDevice);
		ActionHandler(inputEvent, targetInputDevice);

		return true;
	}


	// Activation of CalibrateAxis - Wheel Hold or Click like DriveMode
	private bool OperateMode(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice)
	{
		switch (LocalSettings.Singleton.Joystick.ToggleableKinematics)
		{
			// Toggle
			case true when inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CalibrateMode, targetInputDevice), allowEcho: false, exactMatch: true):
				return true;
			case true: // Toggle with no action
				return false;
			// Hold
			case false when Input.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CalibrateMode, targetInputDevice), exactMatch: true):
				return true;
			case false: // default for hold
				return false;
		}
	}

	// Left Joystick: to Left - Back, to Right - Next 
	private void ChangeAxis(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice)
	{
		// Get joystick vector from actions mapped for this input device
		float x = Input.GetAxis(
			DualSeatEvent.GetName(RcaInEvName.CalibrateAxisBack, targetInputDevice),
			DualSeatEvent.GetName(RcaInEvName.CalibrateAxisNext, targetInputDevice)
		);

		// If both axes are near center, reset the trigger so next action can fire
		if (Math.Abs(x) < centerDeadzone)
		{
			actionTriggered = false;
			return;
		}

		// If already triggered and not returned to center, do nothing
		if (actionTriggered)
			return;

		// Decide direction based on thresholds:
		bool canTriggerLeft = x <= -primaryThreshold;
		bool canTriggerRight = x >= primaryThreshold;

		int trueCount = (canTriggerLeft ? 1 : 0) + (canTriggerRight ? 1 : 0);
		if (trueCount != 1) { return; }

		// Getting the wheel and increment according to the action
		int wheel = LocalSettingsMemory.Singleton.CalibrateAxis.ChoosenWheel;
		if (canTriggerLeft && wheel == -1) { wheel = 3; } else if (canTriggerLeft) { wheel--; } else { wheel++; }

		if (canTriggerLeft || canTriggerRight)
		{
			// Setting the incremeneted value
			LocalSettingsMemory.Singleton.CalibrateAxis.ChoosenWheel = Mathf.PosMod(wheel, 4);
			actionTriggered = true;
		}

	}

	// Velocity: L2 - CCW (Left), R2 - CW (Right)
	private void RotateBumper(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice)
	{
		// Geting the last action to do more conditions
		CalibrateController.LastActions lastAction = CalibrateController.LastAction;

		// Read raw axis value
		float rotateBumpers = Input.GetAxis(
			DualSeatEvent.GetName(RcaInEvName.CalibrateRotateLeft, targetInputDevice),
			DualSeatEvent.GetName(RcaInEvName.CalibrateRotateRight, targetInputDevice)
		);

		// If the offset is pressed during velocity, it will not be executed, and the next bumper will not return to its starting position.
		if (
			(lastBumperValue < 0f && inputEvent.IsActionReleased(DualSeatEvent.GetName(RcaInEvName.CalibrateRotateLeft, targetInputDevice))) ||
			(lastBumperValue > 0f && inputEvent.IsActionReleased(DualSeatEvent.GetName(RcaInEvName.CalibrateRotateRight, targetInputDevice)))
		) lastBumperValue = 0f;


		// Don't run i bumper are not moved and the action is not yet started
		if (rotateBumpers == 0f && lastAction != CalibrateController.LastActions.VelocityStarted) return;

		// Don't run i Offset are set, Velocity go stop and waiting for bumper to go to the start position
		if (lastBumperValue != 0f && lastAction == CalibrateController.LastActions.Offset) return;

		// Don't run is velocity is stoppped
		if (lastBumperValue != 0f && lastAction == CalibrateController.LastActions.VelocityStopped) return;

		// Getting accual vescId
		byte vescId = LocalSettingsMemory.Singleton.CalibrateAxis.ChoosenAxis;
		if (vescId == byte.MaxValue) return; // vescId cannt be MaxValue

		// Getting some values
		float multiple = Math.Abs(rotateBumpers);
		float velocity = Math.Abs(LocalSettingsMemory.Singleton.CalibrateAxis.VelocityValue);
		if (velocity == 0f) return; // Velocity cannot be zero

		// Multiplaing the current amount by bumper pressing state, and te rotation
		float newVelocity = velocity * multiple * (rotateBumpers > 0f ? 1f : -1f);
		lastBumperValue = rotateBumpers;

		if (rotateBumpers != 0f)
		{
			// Updating if running, if not then Start
			if (CalibrateController.IsVelocityRunning())
			{
				CalibrateController.UpdateVelocity(newVelocity);
			}
			else
			{
				CalibrateController.StartVelocity(vescId, newVelocity);
			}
		}
		else
		{
			// If bumpers are in start position then stop
			CalibrateController.StopVelocity();
		}

	}

	// Offset: L1 - CCW (Left), R1 - CW (Right)
	private void RotateOnce(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice)
	{
		// Reading the actions
		bool left = inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CalibrateRotateLeftOnce, targetInputDevice));
		bool right = inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CalibrateRotateRightOnce, targetInputDevice));

		// Making sure the are not pressed together
		if ((!left && !right) || (left && right)) return;

		// Gettings vesc id
		byte vescId = LocalSettingsMemory.Singleton.CalibrateAxis.ChoosenAxis;
		if (vescId == byte.MaxValue) return;

		// Getting offset from panel
		float offset = Mathf.Abs(LocalSettingsMemory.Singleton.CalibrateAxis.OffsetValue);
		if (offset == 0f) return;

		if (left) { CalibrateController.SendOffsetAsync(vescId, (-1) * offset); }
		if (right) { CalibrateController.SendOffsetAsync(vescId, offset); }
	}

	// D-pad Actions: Up = Stop, Down = Cancel, Left = Confirm, Right = Return to Origin
	private void ActionHandler(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice)
	{
		// Reading of the action
		bool left = inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CalibrateActionLeft, targetInputDevice));
		bool right = inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CalibrateActionRight, targetInputDevice));
		bool bottom = inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CalibrateActionBottom, targetInputDevice));
		bool top = inputEvent.IsActionPressed(DualSeatEvent.GetName(RcaInEvName.CalibrateActionTop, targetInputDevice));

		int i = (left ? 1 : 0) + (right ? 1 : 0) + (bottom ? 1 : 0) + (top ? 1 : 0);
		if (i != 1) return; // Block if clicked together

		// Gettings vesc id
		byte vescId = LocalSettingsMemory.Singleton.CalibrateAxis.ChoosenAxis;
		if (vescId == byte.MaxValue) return;

		if (left)
			CalibrateController.SendConfirmAsync(vescId);

		if (right)
			CalibrateController.SendReturnToOriginAsync(vescId);

		if (top)
			CalibrateController.SendStopAsync(vescId);

		if (bottom)
			CalibrateController.SendCancelAsync(vescId);
	}


	public Dictionary<StringName, Godot.Collections.Array<InputEvent>> GetInputActions() =>
		IActionAwareController.FetchAllActionEvents(_usedActions);

	public string GetInputActionsAdditionalNote() =>
	"""
    CalibrateAxisController - for manula axis calibration, available only in EStop Mode
    Activation on holding the (PS) wheel button (the right one)
    
    - Velocity (left and right bumpers)
      Action: calibrate_rotate_left/right
      TO: set speed to choosen axis to rotate CCW or CW
    - Offset (left and right triggers)
      Action: calibrate_rotate_left_once/right_once
      TO: set rotation degree to the choosen axis as CCW or CW
    - Change Axis (left joystick: move left or right)
      calibrate_axis_next is getting next wheel in order FL, FR, BL, BR
      calibrate_axis_back is getting last wheel in the same order
      TO: changing operational axis
    - Actions (D-Pad)
      calibrate_action_top is Action Stop
      calibrate_action_bottom is Action Cancel
      calibrate_action_left is Confirm
      calibrate_action_right is Return to origin
      TO: sending actions without values
    """;
}
