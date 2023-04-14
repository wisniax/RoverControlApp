using System;
using System.Collections.Generic;
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

	public static event EventHandler<Vector3> OnAbsoluteVectorChanged;

	private const float JoyPadDeadzone = 0.3f;

	private List<Key> _pressedKeys;
	private Vector2 _rightAnalogVector2;

	private TextureRect _up;
	private TextureRect _down;
	private TextureRect _right;
	private TextureRect _left;
	private TextureRect _zoomIn;
	private TextureRect _zoomOut;
	private Label _halt;
	public override void _Ready()
	{
		_pressedKeys = new List<Key>();
		_rightAnalogVector2 = Vector2.Zero;

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
		switch (@event)
		{
			case InputEventKey inputEventKey:
				HandleInputEventKey(inputEventKey);
				break;
			case InputEventJoypadMotion inputEventJoypadMotion:
				HandleInputEventAnalog(inputEventJoypadMotion);
				break;
			default:
				return;
		}
		RecalculateVector();
	}

	private void RecalculateVector()
	{
		Vector3 absoluteVector3 = Vector3.Zero;
		/*if (_pressedKeys.Count > 0) */
		ResolveVectorFromKeysPressed(ref absoluteVector3);
		OnAbsoluteVectorChanged?.Invoke(this, absoluteVector3);
	}

	private void ResolveVectorFromKeysPressed(ref Vector3 absoluteVector3)
	{
		foreach (var pressedKey in _pressedKeys)
		{
			switch (pressedKey)
			{
				case Key.W:
				case Key.Up:
					absoluteVector3 += Vector3.Up;
					break;

				case Key.D:
				case Key.Right:
					absoluteVector3 += Vector3.Right;
					break;

				case Key.S:
				case Key.Down:
					absoluteVector3 += Vector3.Down;
					break;

				case Key.A:
				case Key.Left:
					absoluteVector3 += Vector3.Left;
					break;

				case Key.H:
					break;

				case Key.KpAdd:
					absoluteVector3 += Vector3.Back;
					break;

				case Key.KpSubtract:
					absoluteVector3 += Vector3.Forward;
					break;

				default:
					break;
			}
		}

		absoluteVector3.X += _rightAnalogVector2.X;
		absoluteVector3.Y += _rightAnalogVector2.Y;
		absoluteVector3 = absoluteVector3.Clamp(new Vector3(-1f, -1f, -1f), new Vector3(1f, 1f, 1f));
	}

	private void HandleInputEventAnalog(InputEventJoypadMotion analogEvent)
	{
		Vector2 velocity = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		velocity = velocity.Clamp(new Vector2(-1f, -1f), new Vector2(1f, 1f));
		velocity.X = Mathf.IsEqualApprox(velocity.X, 0f, Mathf.Max(0.1f, JoyPadDeadzone)) ? 0 : velocity.X;
		velocity.Y = Mathf.IsEqualApprox(velocity.Y, 0f, Mathf.Max(0.1f, JoyPadDeadzone)) ? 0 : velocity.Y;
		_rightAnalogVector2 = velocity;
	}

	private void HandleInputEventKey(InputEventKey inputEventKey)
	{
		if (inputEventKey.IsEcho()) return;
		// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
		if (inputEventKey.Pressed) _pressedKeys.Add(inputEventKey.PhysicalKeycode);
		else _pressedKeys.RemoveAll((x) => x == inputEventKey.PhysicalKeycode);

		switch (inputEventKey.PhysicalKeycode)
		{
			case Key.W:
			case Key.Up:
				_up.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed) OnUpArrowPressed?.Invoke();
				else OnKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "UP_PRESSED" : "UP_RELEASED");
				break;

			case Key.D:
			case Key.Right:
				_right.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed) OnRightArrowPressed?.Invoke();
				else OnKeyReleased?.Invoke();

				GD.Print(inputEventKey.Pressed ? "RIGHT_PRESSED" : "RIGHT_RELEASED");
				break;

			case Key.S:
			case Key.Down:
				_down.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed) OnDownArrowPressed?.Invoke();
				else OnKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "DOWN_PRESSED" : "DOWN_RELEASED");
				break;

			case Key.A:
			case Key.Left:
				_left.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed) OnLeftArrowPressed?.Invoke();
				else OnKeyReleased?.Invoke();
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
				if (inputEventKey.Pressed) OnAddKeyPressed?.Invoke();
				else OnZoomKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "ADD_PRESSED" : "ADD_RELEASED");
				break;

			case Key.KpSubtract:
				_zoomOut.Visible = inputEventKey.Pressed;
				if (inputEventKey.Pressed) OnSubtractKeyPressed?.Invoke();
				else OnZoomKeyReleased?.Invoke();
				GD.Print(inputEventKey.Pressed ? "SUBTRACT_PRESSED" : "SUBTRACT_RELEASED");
				break;

			default:
				break;
		}
	}
}