using Godot;
using System;

public partial class Point : Control
{
	[Export] Label label = null!;
	[Export] CollisionShape2D shape = null!;

	public void SetColor(Color color)
	{
		shape.DebugColor = color;
	}

	public void SetString(string text)
	{
		label.Text = text;
	}

	public void SetNumber(int number)
	{
		label.Text = number.ToString();
	}
}
