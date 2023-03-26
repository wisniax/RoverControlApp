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
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}