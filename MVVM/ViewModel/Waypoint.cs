using Godot;
using RoverControlApp.Core;
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

	public override void _EnterTree()
	{
		deleteButton.Pressed += OnDeletePressed;
	}

	void ShowOnScreen()
	{
		numberLabel.Text = _number.ToString();
		xaxisEdit.Text = Math.Round(_position.X, 2).ToString();
		yaxisEdit.Text = Math.Round(_position.Y, 2).ToString();
		deadzoneEdit.Text = _deadzone.ToString();	
	}

	void OnDeletePressed()
	{
		if (GetParent().GetParent().GetParent() is MissionPlanner missionPlanner)
		{
			missionPlanner.RemoveWaypoint(this);
		}
		else
		{
			GD.PrintErr("Failed to find parent MissionPlanner.");
		}
	}
}
