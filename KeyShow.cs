using Godot;

namespace RoverControlApp;

public partial class KeyShow : Control
{
	//[Export()]
	//public float speed = 10.0f;
	public delegate void KeyPressedEventHandler();

	public static event KeyPressedEventHandler OnKeyReleased;

	public static event KeyPressedEventHandler OnUpArrowPressed;
	public static event KeyPressedEventHandler OnRightArrowPressed;
	public static event KeyPressedEventHandler OnDownArrowPressed;
	public static event KeyPressedEventHandler OnLeftArrowPressed;

	public static event KeyPressedEventHandler OnHKeyPressed;
	public static event KeyPressedEventHandler OnAddKeyPressed;
	public static event KeyPressedEventHandler OnSubtractKeyPressed;
	public static event KeyPressedEventHandler OnZoomKeyReleased;




	private TextureRect _up;
	private TextureRect _down;
	private TextureRect _right;
	private TextureRect _left;
	private TextureRect _zoomIn;
	private TextureRect _zoomOut;
	private Label _halt;
	public override void _Ready()
	{
		_up = GetNode<TextureRect>("up");
		_down = GetNode<TextureRect>("down");
		_right = GetNode<TextureRect>("right");
		_left = GetNode<TextureRect>("left");
		_zoomIn = GetNode<TextureRect>("zoomIn");
		_zoomOut = GetNode<TextureRect>("zoomOut");
		_halt = GetNode<Label>("halt");

		foreach (var child in GetChildren())
			if (child is Control tchild)
			{
				tchild.Visible = false;
			}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{



		//if (Input.IsKeyPressed(Key.R))
		//{
		//	Position = new Vector2(10f, 10f);
		//}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey inputEventKey) return;
		if (inputEventKey.IsEcho()) return;
		// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
		switch (inputEventKey.PhysicalKeycode)
		{
			case Key.W:
			case Key.Up:
				_up.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed)
					OnUpArrowPressed?.Invoke();
				else
					OnKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "UP_PRESSED" : "UP_RELEASED");
				break;

			case Key.D:
			case Key.Right:
				_right.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed)
					OnRightArrowPressed?.Invoke();
				else
					OnKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "RIGHT_PRESSED" : "RIGHT_RELEASED");
				break;

			case Key.S:
			case Key.Down:
				_down.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed)
					OnDownArrowPressed?.Invoke();
				else
					OnKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "DOWN_PRESSED" : "DOWN_RELEASED");
				break;

			case Key.A:
			case Key.Left:
				_left.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed)
					OnLeftArrowPressed?.Invoke();
				else
					OnKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "LEFT_PRESSED" : "LEFT_RELEASED");
				break;

			case Key.H:
				_halt.Visible = inputEventKey.Pressed;
				if (!inputEventKey.Pressed) break;
				OnHKeyPressed?.Invoke();
				GD.Print("H_PRESSED");
				break;

			case Key.KpAdd:
				_zoomIn.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed)
					OnAddKeyPressed?.Invoke();
				else
					OnZoomKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "ADD_PRESSED" : "ADD_RELEASED");
				break;

			case Key.KpSubtract:
				_zoomOut.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed)
					OnSubtractKeyPressed?.Invoke();
				else
					OnZoomKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "SUBTRACT_PRESSED" : "SUBTRACT_RELEASED");
				break;

			default:
				break;
		}


	}
}