using System.ServiceModel;
using System.Threading.Tasks;
using Godot;
using Onvif.Core.Client;
using OnvifCameraControlTest;

namespace RoverControlApp;


public partial class ControlTest : Control
{
	private OnvifCameraThreadController camera = new();

	Label _camStatus;

	private TextureRect _imydz;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//camera = new OnvifCameraController("192.168.5.35", "admin", "admin");
		//GD.Print(camera.State == CommunicationState.Opened ? "Camera is connected" : "Camera is not connected");

		//KeyShow.OnUpArrowPressed += camera.MoveUp;
		//KeyShow.OnRightArrowPressed += camera.MoveRight;
		//KeyShow.OnDownArrowPressed += camera.MoveDown;
		//KeyShow.OnLeftArrowPressed += camera.MoveLeft;
		//KeyShow.OnAddKeyPressed += camera.ZoomIn;
		//KeyShow.OnSubtractKeyPressed += camera.ZoomOut;
		//KeyShow.OnKeyReleased += camera.MoveStop;
		//KeyShow.OnZoomKeyReleased += camera.ZoomStop;
		KeyShow.OnAbsoluteVectorChanged += camera.ChangeMoveVector;



		_camStatus = GetNode<Label>("CamStatus");
		camera.Start("192.168.5.35", "admin", "admin");

		_imydz = GetNode<TextureRect>("TextureRect");
	}

	double _deltaSum;
	double _deltaSumMax = 0;

	int _progress = 0;
	public override void _Process(double delta)
	{
		_deltaSum += delta;
		if (_deltaSum > _deltaSumMax)
		{
			_deltaSum = 0;
			switch (camera.State)
			{
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
			}

		}
	}
}
