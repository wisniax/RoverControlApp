using Godot;
using MQTTnet;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using static RoverControlApp.Core.MqttClasses;

public partial class ThreeDVisualizer : Node3D
{
	[Export] private Node3D _chassis = null!;

	[Export] private Node3D _suspensionFL = null!;
	[Export] private Node3D _suspensionFR = null!;
	[Export] private Node3D _suspensionRL = null!;
	[Export] private Node3D _suspensionRR = null!;

	[Export] private Node3D _wheelFL = null!;
	[Export] private Node3D _wheelFR = null!;
	[Export] private Node3D _wheelRL = null!;
	[Export] private Node3D _wheelRR = null!;

	[Export] private ThreeDBattery _battery0 = null!;
	[Export] private ThreeDBattery _battery1 = null!;
	[Export] private ThreeDBattery _battery2 = null!;

	[Export] private Node3D _hotswap0 = null!;
	[Export] private Node3D _hotswap1 = null!;
	[Export] private Node3D _hotswap2 = null!;

	[Export] private ThreeDManipulator _threeDManipulator = null!;
	[Export] private ThreeDSampler _threeDSampler = null!;

	[Export] private Camera3D _camera = null!;
	private Camera3D _targetCamera = null!;

	[Export] private Camera3D _cameraTargetFreemode = null!;
	[Export] private Camera3D _cameraTargetWheels = null!;
	[Export] private Camera3D _cameraTargetBattery = null!;
	[Export] private Camera3D _cameraTargetManipulator = null!;
	[Export] private Camera3D _cameraTargetSampler = null!;
	[Export] private Camera3D _cameraTargetBack = null!;
	[Export] private Camera3D _cameraTargetLeft = null!;
	[Export] private Camera3D _cameraTargetRight = null!;
	[Export] private Camera3D _cameraTargetFront = null!;

	public void ChangeCameraTarget(MqttClasses.CameraTarget3D target)
	{
		switch (target)
		{
			case MqttClasses.CameraTarget3D.Freemode:
				_targetCamera = _cameraTargetFreemode;
				break;
			case MqttClasses.CameraTarget3D.Wheels:
				_targetCamera = _cameraTargetWheels;
				break;
			case MqttClasses.CameraTarget3D.Manipulator:
				_targetCamera = _cameraTargetManipulator;
				break;
			case MqttClasses.CameraTarget3D.Sampler:
				_targetCamera = _cameraTargetSampler;
				break;
			case MqttClasses.CameraTarget3D.Battery:
				_targetCamera = _cameraTargetBattery;
				break;
			default:
				EventLogger.LogMessage("ThreeDVisualizer", EventLogger.LogLevel.Warning, $"Unhandled Camera Target: {target}");
				return;
		}
	}

	public override void _EnterTree()
	{
		MqttNode.Singleton.MessageReceivedAsync += VescInfoChanged;
		MqttNode.Singleton.MessageReceivedAsync += BatteryInfoChanged;

		_targetCamera = _cameraTargetFreemode;
	}

	public async Task VescInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicWheelFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicWheelFeedback)
			return;
		if (msg is null || msg.PayloadSegment.Count == 0)
			return;

		string payload = System.Text.Encoding.UTF8.GetString(msg.PayloadSegment.ToArray());
		var velData = JsonSerializer.Deserialize<MqttClasses.WheelFeedback>(payload);
		if (velData is null) return;

		int vescID = velData.VescId;

		switch (vescID)
		{
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.TurnMotors.FrontLeft): // Front Left turn
				CallDeferred(nameof(UpdateWheelRotation), _wheelFL, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.TurnMotors.FrontRight): // Front Right turn
				CallDeferred(nameof(UpdateWheelRotation), _wheelFR, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.TurnMotors.RearLeft): // Rear Left turn
				CallDeferred(nameof(UpdateWheelRotation), _wheelRL, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.TurnMotors.RearRight): // Rear Right turn
				CallDeferred(nameof(UpdateWheelRotation), _wheelRR, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis1): // Manipulator Axis 1
				_threeDManipulator.CallDeferred(nameof(ThreeDManipulator.UpdateManipulatorAxis), 0, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis2): // Manipulator Axis 2
				_threeDManipulator.CallDeferred(nameof(ThreeDManipulator.UpdateManipulatorAxis), 1, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis3): // Manipulator Axis 3
				_threeDManipulator.CallDeferred(nameof(ThreeDManipulator.UpdateManipulatorAxis), 2, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis4): // Manipulator Axis 4
				_threeDManipulator.CallDeferred(nameof(ThreeDManipulator.UpdateManipulatorAxis), 3, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis5): // Manipulator Axis 5
				_threeDManipulator.CallDeferred(nameof(ThreeDManipulator.UpdateManipulatorAxis), 4, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis6): // Manipulator Axis 6
				_threeDManipulator.CallDeferred(nameof(ThreeDManipulator.UpdateManipulatorAxis), 5, velData.PrecisePos);
				break;
			case var id when id == HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Gripper): // Manipulator Gripper
				_threeDManipulator.CallDeferred(nameof(ThreeDManipulator.UpdateManipulatorAxis), 6, velData.PrecisePos);
				break;
			default:
				EventLogger.LogMessage("ThreeDVisualizer", EventLogger.LogLevel.Warning, $"Unhandled VESC ID: {vescID}");
				break;
		}

		await Task.Yield();
	}

	public async Task BatteryInfoChanged(string subTopic, MqttApplicationMessage? msg)
	{
		if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicBatteryInfo) || subTopic != LocalSettings.Singleton.Mqtt.TopicBatteryInfo)
			return;
		if (msg is null || msg.PayloadSegment.Count == 0)
			return;
		string payload = System.Text.Encoding.UTF8.GetString(msg.PayloadSegment.ToArray());
		var batteryData = JsonSerializer.Deserialize<MqttClasses.BatteryInfo>(payload);
		if (batteryData is null) return;
		switch (batteryData.Slot)
		{
			case 0:
				_battery0.UpdateBatteryLevel(batteryData);
				break;
			case 1:
				_battery1.UpdateBatteryLevel(batteryData);
				break;
			case 2:
				_battery2.UpdateBatteryLevel(batteryData);
				break;
			default:
				EventLogger.LogMessage("ThreeDVisualizer", EventLogger.LogLevel.Warning, $"Unhandled Battery Slot: {batteryData.Slot}");
				break;
		}
		await Task.Yield();
	}

	int HexStringToInt(string hexString)
	{
		if (hexString.StartsWith("0x"))
		{
			hexString = hexString.Substring(2);
		}
		return Convert.ToInt32(hexString, 16);
	}

	void UpdateWheelRotation(Node3D wheelNode, double precisePos)
	{
		wheelNode.RotationDegrees = new Vector3(0, -(float)precisePos, 0);
	}

	public MqttClasses.CameraTarget3D ReportAvailableCameras()
	{
		MqttClasses.CameraTarget3D _availableCameras = MqttClasses.CameraTarget3D.Freemode;
		if (_cameraTargetWheels != null)
			_availableCameras |= MqttClasses.CameraTarget3D.Wheels;
		if (_cameraTargetBattery != null)
			_availableCameras |= MqttClasses.CameraTarget3D.Battery;
		if (_cameraTargetManipulator != null && _threeDManipulator.Visible) // TODO: Implement visibility check for manipulator
			_availableCameras |= MqttClasses.CameraTarget3D.Manipulator;
		if (_cameraTargetSampler != null && /*_threeDSampler.Visible*/ false) // TODO: Implement visibility check for sampler
			_availableCameras |= MqttClasses.CameraTarget3D.Sampler;

		return _availableCameras;
	}

	public void UpdateCameraInterpolation(double delta)
	{
		if (_camera == null || _targetCamera == null)
			return;

		float CameraSizeKp = 2.0f; // Proportional gain for camera size adjustment
		float CameraTransformKp = 3.0f; // Proportional gain for camera position adjustment

		_camera.Size += (_targetCamera.Size - _camera.Size) * CameraSizeKp * (float)delta;
		_camera.GlobalTransform = _camera.GlobalTransform.InterpolateWith(_targetCamera.GlobalTransform, CameraTransformKp * (float)delta);
	}

	public override void _Process(double delta)
	{
		UpdateCameraInterpolation(delta);
	}
}
