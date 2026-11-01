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

	private MqttClasses.CameraTarget3D _currentCameraType = MqttClasses.CameraTarget3D.Freemode;
	private MqttClasses.CameraTarget3D AvailableCameras
	{
		get => _threeDVisualizer.ReportAvailableCameras();	
	} 

	private void _OnForwardButtonPressed()
	{
		do
		{
			_currentCameraType = _currentCameraType switch
			{
				MqttClasses.CameraTarget3D.Freemode => MqttClasses.CameraTarget3D.Wheels,
				MqttClasses.CameraTarget3D.Wheels => MqttClasses.CameraTarget3D.Manipulator,
				MqttClasses.CameraTarget3D.Manipulator => MqttClasses.CameraTarget3D.Sampler,
				MqttClasses.CameraTarget3D.Sampler => MqttClasses.CameraTarget3D.Battery,
				MqttClasses.CameraTarget3D.Battery => MqttClasses.CameraTarget3D.Freemode,
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
				MqttClasses.CameraTarget3D.Freemode => MqttClasses.CameraTarget3D.Battery,
				MqttClasses.CameraTarget3D.Wheels => MqttClasses.CameraTarget3D.Freemode,
				MqttClasses.CameraTarget3D.Manipulator => MqttClasses.CameraTarget3D.Wheels,
				MqttClasses.CameraTarget3D.Sampler => MqttClasses.CameraTarget3D.Manipulator,
				MqttClasses.CameraTarget3D.Battery => MqttClasses.CameraTarget3D.Sampler,
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
