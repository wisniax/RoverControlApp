using Godot;
using System;

public partial class Point : Control
{
	[Export] Label label = null!;
	[Export] Sprite2D sprite = null!;

	public void SetColor(Color color)
	{
		sprite.Modulate = color;
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
