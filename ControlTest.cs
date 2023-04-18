using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Godot;
using Onvif.Core.Client;
using OnvifCameraControlTest;

namespace RoverControlApp;


public partial class ControlTest : Control
{
	private OnvifCameraThreadController _camera;
	public static LocalSettings Settings { get; set; }

	Label _camStatus;
	private TextureRect _imydz;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Settings = new LocalSettings();

		_camera = new OnvifCameraThreadController
		{
			InvertControl = Settings.Settings.CameraInverseAxis,
			MinSpanEveryCom = TimeSpan.FromSeconds(1 / Settings.Settings.PtzRequestFrequency)
		};

		_camera.Start(Settings.Settings.CameraPtzIp, Settings.Settings.CameraLogin, Settings.Settings.CameraPassword);
		KeyShow.JoyPadDeadzone = Settings.Settings.JoyPadDeadzone;
		KeyShow.OnAbsoluteVectorChanged += _camera.ChangeMoveVector;

		_camStatus = GetNode<Label>("CamStatus");
		_imydz = GetNode<TextureRect>("TextureRect");
	}

	double _deltaSum;
	double _deltaSumMax = 0;

	int _progress = 0;
	public override void _Process(double delta)
	{
		_deltaSum += delta;
		if (!(_deltaSum > _deltaSumMax)) return;
		_deltaSum = 0;
		switch (_camera.State)
		{
			case CommunicationState.Created:
			case CommunicationState.Opening:
				_deltaSumMax = 0.69;
				string dot;
				if (_progress == 0)
					dot = ".";
				else if (_progress == 1)
					dot = " .";
				else
				{
					dot = "  .";
					_progress = -1;
				}
				_progress++;
				_camStatus.Text = "Camera is connecting" + dot;
				break;

			case CommunicationState.Opened:
				_deltaSumMax = 1.0;
				_camStatus.Text = "Camera is connected!";
				if (_progress == 0)
					_camStatus.AddThemeColorOverride("font_color", Colors.Lime);
				else
				{
					_camStatus.AddThemeColorOverride("font_color", Colors.LimeGreen);
					_progress = -1;
				}
				_progress++;
				break;

			case CommunicationState.Faulted:
				_deltaSumMax = 0.3;
				_camStatus.Text = "Camera connection error :CCC";
				if (_progress == 0)
					_camStatus.AddThemeColorOverride("font_color", Colors.Red);
				else
				{
					_camStatus.AddThemeColorOverride("font_color", Colors.DarkRed);
					_progress = -1;
				}
				_progress++;
				break;

			case CommunicationState.Closed:
				_deltaSumMax = 60.0;
				_camStatus.Text = "Camera connection closed.";
				_camStatus.RemoveThemeColorOverride("font_color");
				break;
			case CommunicationState.Closing:
			default:
				break;
		}
	}
}
