using Godot;
using System;

public partial class SpinningText : Panel
{
	private float _angularSpeed = Mathf.Pi;
	private Label _label;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_label = GetNode<Label>("SpinningLabel");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_label.Rotation += _angularSpeed * (float)delta;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			switch (keyEvent.Keycode)
			{
				case Key.R:
					_label.Modulate = Colors.Red;
					break;
				case Key.G:
					_label.Modulate = Colors.Green;
					break;
				case Key.B:
					_label.Modulate = Colors.Blue;
					break;
			}
		}
	}
}
