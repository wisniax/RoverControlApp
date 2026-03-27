using Godot;

namespace RoverControlApp.Core.RoverControllerPresets;

public static class RcaInEvName
{
	public static readonly StringName CameraMoveLeft = new("camera_move_left");
	public static readonly StringName CameraMoveRight = new("camera_move_right");
	public static readonly StringName CameraMoveDown = new("camera_move_down");
	public static readonly StringName CameraMoveUp = new("camera_move_up");
	public static readonly StringName CameraZoomOut = new("camera_zoom_out");
	public static readonly StringName CameraZoomIn = new("camera_zoom_in");
	public static readonly StringName CameraFocusOut = new("camera_focus_out");
	public static readonly StringName CameraFocusIn = new("camera_focus_in");

	public static readonly StringName ControlModeEstop = new("controlmode_estop");
	public static readonly StringName ControlModeChange = new("controlmode_change");
	public static readonly StringName ControlModeDrive = new("controlmode_drive");
	public static readonly StringName ControlModeManipulator = new("controlmode_manipulator");
	public static readonly StringName ControlModeSampler = new("controlmode_sampler");
	public static readonly StringName ControlModeAutonomy = new("controlmode_autonomy");

	public static readonly StringName ManipulatorSpeedBackward = new("manipulator_speed_backward");
	public static readonly StringName ManipulatorSpeedForward = new("manipulator_speed_forward");
	public static readonly StringName ManipulatorAxis1 = new("manipulator_axis_1");
	public static readonly StringName ManipulatorAxis2 = new("manipulator_axis_2");
	public static readonly StringName ManipulatorAxis3 = new("manipulator_axis_3");
	public static readonly StringName ManipulatorAxis4 = new("manipulator_axis_4");
	public static readonly StringName ManipulatorAxis5 = new("manipulator_axis_5");
	public static readonly StringName ManipulatorAxis6 = new("manipulator_axis_6");
	public static readonly StringName ManipulatorMultiAxis1Backward = new("manipulator_multi_axis_1_backward");
	public static readonly StringName ManipulatorMultiAxis2Backward = new("manipulator_multi_axis_2_backward");
	public static readonly StringName ManipulatorMultiAxis3Backward = new("manipulator_multi_axis_3_backward");
	public static readonly StringName ManipulatorMultiAxis4Backward = new("manipulator_multi_axis_4_backward");
	public static readonly StringName ManipulatorMultiAxis5Backward = new("manipulator_multi_axis_5_backward");
	public static readonly StringName ManipulatorMultiAxis6Backward = new("manipulator_multi_axis_6_backward");
	public static readonly StringName ManipulatorMultiGripperBackward = new("manipulator_multi_gripper_backward");
	public static readonly StringName ManipulatorMultiAxis1Forward = new("manipulator_multi_axis_1_forward");
	public static readonly StringName ManipulatorMultiAxis2Forward = new("manipulator_multi_axis_2_forward");
	public static readonly StringName ManipulatorMultiAxis3Forward = new("manipulator_multi_axis_3_forward");
	public static readonly StringName ManipulatorMultiAxis4Forward = new("manipulator_multi_axis_4_forward");
	public static readonly StringName ManipulatorMultiAxis5Forward = new("manipulator_multi_axis_5_forward");
	public static readonly StringName ManipulatorMultiAxis6Forward = new("manipulator_multi_axis_6_forward");
	public static readonly StringName ManipulatorMultiGripperForward = new("manipulator_multi_gripper_forward");
	public static readonly StringName ManipulatorMultiChangeAxes = new("manipulator_multi_change_axes");

	public static readonly StringName SamplerMoveDown = new("sampler_move_down");
	public static readonly StringName SamplerMoveUp = new("sampler_move_up");
	public static readonly StringName SamplerDrillDown = new("sampler_drill_down");
	public static readonly StringName SamplerDrillUp = new("sampler_drill_up");
	public static readonly StringName SamplerDrillMovement = new("sampler_drill_movement");
	public static readonly StringName SamplerPlatformMovement = new("sampler_platform_movement");
	public static readonly StringName SamplerDrillEnable = new("sampler_drill_enable");
	public static readonly StringName SamplerDrillingAltmode = new("sampler_drilling_altmode");
	public static readonly StringName SamplerContainer0 = new("sampler_container_0");
	public static readonly StringName SamplerContainer1 = new("sampler_container_1");
	public static readonly StringName SamplerContainer2 = new("sampler_container_2");
	public static readonly StringName SamplerContainer3 = new("sampler_container_3");
	public static readonly StringName SamplerContainer4 = new("sampler_container_4");
	public static readonly StringName SamplerContainerPreciseUp = new("sampler_container_precise_up");
	public static readonly StringName SamplerContainerPreciseDown = new("sampler_container_precise_down");

	public static readonly StringName CrabMode = new("crab_mode");
	public static readonly StringName SpinnerMode = new("spinner_mode");
	public static readonly StringName EbrakeMode = new("ebrake_mode");
	public static readonly StringName AckermannMode = new("ackermann_mode");
	public static readonly StringName RoverMoveBackward = new("rover_move_backward");
	public static readonly StringName RoverMoveForward = new("rover_move_forward");
	public static readonly StringName RoverMoveRight = new("rover_move_right");
	public static readonly StringName RoverMoveLeft = new("rover_move_left");
	public static readonly StringName RoverMoveDown = new("rover_move_down");
	public static readonly StringName RoverMoveUp = new("rover_move_up");
	public static readonly StringName RoverRotateRight = new("rover_move_up");
	public static readonly StringName RoverRotateLeft = new("rover_move_up");

	public static readonly StringName CalibrateMode = new("calibrate_mode");
	public static readonly StringName CalibrateRotateLeft = new("calibrate_rotate_left");
	public static readonly StringName CalibrateRotateLeftOnce = new("calibrate_rotate_left_once");
	public static readonly StringName CalibrateRotateRight = new("calibrate_rotate_right");
	public static readonly StringName CalibrateRotateRightOnce = new("calibrate_rotate_right_once");
	public static readonly StringName CalibrateAxisNext = new("calibrate_axis_next");
	public static readonly StringName CalibrateAxisBack = new("calibrate_axis_back");

	public static readonly StringName CalibrateActionTop = new("calibrate_action_top");
	public static readonly StringName CalibrateActionBottom = new("calibrate_action_bottom");
	public static readonly StringName CalibrateActionLeft = new("calibrate_action_left");
	public static readonly StringName CalibrateActionRight = new("calibrate_action_right");
}
