using System;

using Godot;

using static RoverControlApp.Core.MqttClasses;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IRoverManipulatorController : IActionAwareController
{
	/// <summary>
	/// Probes Godot.Input and returns ManipulatorControl
	/// </summary>
	public ManipulatorControl CalculateMoveVector(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, in ManipulatorControl lastState);

	/// <summary>
	/// Processes input
	/// </summary>
	/// <returns>True when input causes state change</returns>
	public bool HandleInput(in InputEvent inputEvent, DualSeatEvent.InputDevice targetInputDevice, ManipulatorControl lastState, out ManipulatorControl newState)
	{
		newState = CalculateMoveVector(inputEvent, targetInputDevice, lastState);
		bool changed = IsMoveVectorChanged(newState, lastState);
		
		newState.Timestamp = changed ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : lastState.Timestamp;
		return changed;
	}

	/// <summary>
	/// Compares two ManipulatorControl states and determines if change is big enough, to be considered
	/// </summary>
	/// <returns>true if changed</returns>
	public bool IsMoveVectorChanged(in ManipulatorControl currentState, in ManipulatorControl lastState)
	{
		if (currentState.ActionType != lastState.ActionType) return true;

		if (!Mathf.IsEqualApprox(currentState.Gripper, lastState.Gripper, 0.001f)) return true;

		switch (currentState.ActionType)
		{
			case ActionType.ForwardKin:
				return !Mathf.IsEqualApprox(currentState.ForwardKin.Axis1, lastState.ForwardKin.Axis1, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.ForwardKin.Axis2, lastState.ForwardKin.Axis2, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.ForwardKin.Axis3, lastState.ForwardKin.Axis3, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.ForwardKin.Axis4, lastState.ForwardKin.Axis4, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.ForwardKin.Axis5, lastState.ForwardKin.Axis5, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.ForwardKin.Axis6, lastState.ForwardKin.Axis6, 0.001f);
				break;
			case ActionType.InvKinJoystick:
				return !Mathf.IsEqualApprox(currentState.InvJoystick.LinearSpeed.X, lastState.InvJoystick.LinearSpeed.X, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvJoystick.LinearSpeed.Y, lastState.InvJoystick.LinearSpeed.Y, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvJoystick.LinearSpeed.Z, lastState.InvJoystick.LinearSpeed.Z, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvJoystick.RotationSpeed.X, lastState.InvJoystick.RotationSpeed.X, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvJoystick.RotationSpeed.Y, lastState.InvJoystick.RotationSpeed.Y, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvJoystick.RotationSpeed.Z, lastState.InvJoystick.RotationSpeed.Z, 0.001f);
				break;
			case ActionType.InvKinPosition:
				return !Mathf.IsEqualApprox(currentState.InvPosition.Position.X, lastState.InvPosition.Position.X, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvPosition.Position.Y, lastState.InvPosition.Position.Y, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvPosition.Position.Z, lastState.InvPosition.Position.Z, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvPosition.Rotation.X, lastState.InvPosition.Rotation.X, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvPosition.Rotation.Y, lastState.InvPosition.Rotation.Y, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvPosition.Rotation.Z, lastState.InvPosition.Rotation.Z, 0.001f) ||
					   !Mathf.IsEqualApprox(currentState.InvPosition.Rotation.W, lastState.InvPosition.Rotation.W, 0.001f);
				break;
			case ActionType.InvKinOffset:
				// No mode for it
				break;
			case ActionType.GoToReference:
				// No mode for it
				break;
		}

		return false;
	}



	/// <summary>
	/// Returns currently controlled axes
	/// </summary>
	public string[] GetControlledAxes();
}
