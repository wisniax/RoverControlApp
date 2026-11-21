using Godot;
using System;
using RoverControlApp.Core;
using RoverControlApp.Core.Settings;
using RoverControlApp.MVVM.Model;

public partial class ThreeDModelView : Panel
{
	[Export] private ThreeDVisualizer _threeDVisualizer = null!;
	[Export] private Button _buttonForward = null!;
	[Export] private Button _buttonBackward = null!;
	[Export] private Label _labelCurrentCamera = null!;
	[Export] private Label[] _driveLabels = null!;
	[Export] private Label[] _turnLabels = null!;
	[Export] private Label[] _imuLabels = null!;
	[Export] private Label[] _manipulatorLabels = null!;
	[Export] private Label[] _samplerLabels = null!;
	

	private MqttClasses.CameraTarget3D _currentCameraType = MqttClasses.CameraTarget3D.Freemode;
	private MqttClasses.CameraTarget3D AvailableCameras
	{
		get => _threeDVisualizer.ReportAvailableCameras();	
	} 

	public override void _Ready()
	{
		ChangeLabels();
	}

	private void ChangeLabels()
	{
		_driveLabels[0].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.DriveMotors.FrontLeft).ToString("X2");
		_driveLabels[1].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.DriveMotors.FrontRight).ToString("X2");
		_driveLabels[2].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.DriveMotors.RearRight).ToString("X2");
		_driveLabels[3].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.DriveMotors.RearLeft).ToString("X2");

		_turnLabels[0].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.TurnMotors.FrontLeft).ToString("X2");
		_turnLabels[1].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.TurnMotors.FrontRight).ToString("X2");
		_turnLabels[2].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.TurnMotors.RearRight).ToString("X2");
		_turnLabels[3].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.TurnMotors.RearLeft).ToString("X2");

		_imuLabels[0].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.ImuBoards.FrontLeft).ToString("X2");
		_imuLabels[1].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.ImuBoards.FrontRight).ToString("X2");
		_imuLabels[2].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.ImuBoards.RearRight).ToString("X2");
		_imuLabels[3].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.ImuBoards.RearLeft).ToString("X2");
		_imuLabels[4].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.ImuBoards.Body).ToString("X2");
		_manipulatorLabels[0].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis1).ToString("X2");
		_manipulatorLabels[1].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis2).ToString("X2");
		_manipulatorLabels[2].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis3).ToString("X2");
		_manipulatorLabels[3].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis4).ToString("X2");
		_manipulatorLabels[4].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis5).ToString("X2");
		_manipulatorLabels[5].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Axis6).ToString("X2");
		_manipulatorLabels[6].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Manipulator.Gripper).ToString("X2");

		_samplerLabels[0].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Sampler.Platform).ToString("X2");
		_samplerLabels[1].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Sampler.DrillMovement).ToString("X2");
		_samplerLabels[2].Text = _threeDVisualizer.HexStringToInt(LocalSettings.Singleton.CanIds.Sampler.Drill).ToString("X2");
	}

	string GetCanHexStringFromSettings(string canIdString)
	{
		if (canIdString == null || canIdString == "")
		{
			return "-";
		}
		return _threeDVisualizer.HexStringToInt(canIdString).ToString("X2");
	}

	private void _OnForwardButtonPressed()
	{
		do
		{
			_currentCameraType = _currentCameraType switch
			{
				MqttClasses.CameraTarget3D.Freemode => MqttClasses.CameraTarget3D.Right,
				MqttClasses.CameraTarget3D.Right => MqttClasses.CameraTarget3D.Battery,
				MqttClasses.CameraTarget3D.Battery => MqttClasses.CameraTarget3D.Sampler,
				MqttClasses.CameraTarget3D.Sampler => MqttClasses.CameraTarget3D.Back,
				MqttClasses.CameraTarget3D.Back => MqttClasses.CameraTarget3D.Left,
				MqttClasses.CameraTarget3D.Left => MqttClasses.CameraTarget3D.Manipulator,
				MqttClasses.CameraTarget3D.Manipulator => MqttClasses.CameraTarget3D.Wheels,
				MqttClasses.CameraTarget3D.Wheels => MqttClasses.CameraTarget3D.Freemode,
				_ => throw new ArgumentOutOfRangeException()
			};
		} while (!AvailableCameras.HasFlag(_currentCameraType));

		CallDeferred(nameof(UpdateCameraTargetLabel));
		_threeDVisualizer.ChangeCameraTarget(_currentCameraType);
	}

	private void _OnBackwardButtonPressed()
	{
		do
		{
			_currentCameraType = _currentCameraType switch
			{
				MqttClasses.CameraTarget3D.Freemode => MqttClasses.CameraTarget3D.Wheels,
				MqttClasses.CameraTarget3D.Wheels => MqttClasses.CameraTarget3D.Manipulator,
				MqttClasses.CameraTarget3D.Manipulator => MqttClasses.CameraTarget3D.Left,
				MqttClasses.CameraTarget3D.Left => MqttClasses.CameraTarget3D.Back,
				MqttClasses.CameraTarget3D.Back => MqttClasses.CameraTarget3D.Sampler,
				MqttClasses.CameraTarget3D.Sampler => MqttClasses.CameraTarget3D.Battery,
				MqttClasses.CameraTarget3D.Battery => MqttClasses.CameraTarget3D.Right,
				MqttClasses.CameraTarget3D.Right => MqttClasses.CameraTarget3D.Freemode,
				_ => throw new ArgumentOutOfRangeException()
			};
		} while (!AvailableCameras.HasFlag(_currentCameraType));

		CallDeferred(nameof(UpdateCameraTargetLabel));
		_threeDVisualizer.ChangeCameraTarget(_currentCameraType);
	}

	void UpdateCameraTargetLabel()
	{
		_labelCurrentCamera.Text = _currentCameraType.ToString();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
