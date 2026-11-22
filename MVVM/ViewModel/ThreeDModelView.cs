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
	private DateTime[] _driveTimers = Array.Empty<DateTime>();

	[Export] private Label[] _turnLabels = null!;
	private DateTime[] _turnTimers = Array.Empty<DateTime>();

	[Export] private Label[] _imuLabels = null!;
	private DateTime[] _imuTimers = Array.Empty<DateTime>();

	[Export] private Label[] _manipulatorLabels = null!;
	private DateTime[] _manipulatorTimers = Array.Empty<DateTime>();

	[Export] private Label[] _samplerLabels = null!;
	private DateTime[] _samplerTimers = Array.Empty<DateTime>();


	private MqttClasses.CameraTarget3D _currentCameraType = MqttClasses.CameraTarget3D.Freemode;
	private MqttClasses.CameraTarget3D AvailableCameras
	{
		get => _threeDVisualizer.ReportAvailableCameras();	
	} 

	public override void _Ready()
	{
		UpdateLabels();
		//LocalSettings.Singleton.PropagatedPropertyChanged += OnSettingsPropertyChanged;
		LocalSettings.Singleton.Connect(LocalSettings.SignalName.PropagatedSubcategoryChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsSubcategoryChanged));

	}

	public override void _ExitTree()
	{
		//LocalSettings.Singleton.PropagatedPropertyChanged -= OnSettingsPropertyChanged;
		LocalSettings.Singleton.Disconnect(LocalSettings.SignalName.PropagatedSubcategoryChanged, Callable.From<StringName, StringName, Variant, Variant>(OnSettingsSubcategoryChanged));
	}

	private void OnSettingsSubcategoryChanged(StringName category, StringName subcategory, Variant oldValue, Variant newValue)
	{
		if (category != nameof(LocalSettings.CanIds)) return;

		UpdateLabels();
	}

	private void UpdateLabels()
	{
		_driveLabels[0].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.DriveMotors.FrontLeft);
		_driveLabels[1].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.DriveMotors.FrontRight);
		_driveLabels[2].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.DriveMotors.RearRight);
		_driveLabels[3].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.DriveMotors.RearLeft);

		_turnLabels[0].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.TurnMotors.FrontLeft);
		_turnLabels[1].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.TurnMotors.FrontRight);
		_turnLabels[2].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.TurnMotors.RearRight);
		_turnLabels[3].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.TurnMotors.RearLeft);

		_imuLabels[0].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.ImuBoards.FrontLeft);
		_imuLabels[1].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.ImuBoards.FrontRight);
		_imuLabels[2].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.ImuBoards.RearRight);
		_imuLabels[3].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.ImuBoards.RearLeft);
		_imuLabels[4].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.ImuBoards.Body);
		_manipulatorLabels[0].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Manipulator.Axis1);
		_manipulatorLabels[1].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Manipulator.Axis2);
		_manipulatorLabels[2].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Manipulator.Axis3);
		_manipulatorLabels[3].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Manipulator.Axis4);
		_manipulatorLabels[4].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Manipulator.Axis5);
		_manipulatorLabels[5].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Manipulator.Axis6);
		_manipulatorLabels[6].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Manipulator.Gripper);

		_samplerLabels[0].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Sampler.Platform);
		_samplerLabels[1].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Sampler.DrillMovement);
		_samplerLabels[2].Text = GetCanHexStringFromSettings(LocalSettings.Singleton.CanIds.Sampler.Drill);
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
