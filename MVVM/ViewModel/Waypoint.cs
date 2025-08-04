using Godot;
using System;

public partial class Waypoint : Panel
{
	[Export] Label		numberLabel = null!;
	[Export] TextEdit	xaxisEdit = null!;
	[Export] TextEdit	yaxisEdit = null!;
	[Export] TextEdit	deadzoneEdit = null!;
	[Export] CheckBox	waitCheckBox = null!;
	[Export] Button		deleteButton = null!;

	public int Number
	{
		get => _number;
		set
		{
			_number = value;
			ShowOnScreen();
		}
	}

	public Vector2 Coordinates
	{
		get => _position;
		set
		{
			_position = value;
			ShowOnScreen();
		}
	}

	public int Deadzone
	{
		get => _deadzone;
		set
		{
			_deadzone = value;
			ShowOnScreen();
		}
	}

	private int _number;
	private Vector2 _position;
	private int _deadzone;

	public Waypoint()
	{
		_number = 0;
		_position = Vector2.Zero;
		_deadzone = 1;
		//ShowOnScreen();
	}

	public Waypoint(int number, Vector2 position, int deadzone)
	{
		_number = number;
		_position = position;
		_deadzone = deadzone;
		ShowOnScreen();
	}

	void ShowOnScreen()
	{
		numberLabel.Text = _number.ToString();
		xaxisEdit.Text = _position.X.ToString();
		yaxisEdit.Text = _position.Y.ToString();
		deadzoneEdit.Text = _deadzone.ToString();
	}
}
