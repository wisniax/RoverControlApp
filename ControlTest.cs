using System.ServiceModel;
using Godot;
using OnvifCameraControlTest;

namespace RoverControlApp;


public partial class ControlTest : Control
{
	private OnvifCameraController camera;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		camera = new OnvifCameraController("placeholder", "admin", "admin");
		GD.Print(camera.State == CommunicationState.Opened ? "Camera is connected" : "Camera is not connected");

		KeyShow.OnUpArrowPressed += camera.MoveUp;
		KeyShow.OnRightArrowPressed += camera.MoveRight;
		KeyShow.OnDownArrowPressed += camera.MoveDown;
		KeyShow.OnLeftArrowPressed += camera.MoveLeft;
		KeyShow.OnHKeyPressed += camera.GotoHomePosition;
		KeyShow.OnAddKeyPressed += camera.ZoomIn;
		KeyShow.OnSubtractKeyPressed += camera.ZoomOut;
		KeyShow.OnKeyReleased += camera.MoveStop;

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}