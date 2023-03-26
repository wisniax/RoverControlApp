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

	public override void _Input(InputEvent passeEvent)
	{
		if (passeEvent is not InputEventKey eventKey) return;
		if (!eventKey.Pressed) return;

		/*
		switch (eventKey.Keycode)
		{
			case (int)Key.Up:
				camera.MoveUp();
				break;
			case (int)KeyList.Down:
				camera.MoveDown();
				break;
			case (int)KeyList.Left:
				camera.MoveLeft();
				break;
			case (int)KeyList.Right:
				camera.MoveRight();
				break;
			case (int)KeyList.PageUp:
				camera.MoveUp();
				break;
			case (int)KeyList.PageDown:
				camera.MoveDown();
				break;
			case (int)KeyList.Home:
				camera.MoveHome();
				break;
			case (int)KeyList.End:
				camera.MoveStop();
				break;
		}
		*/
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}