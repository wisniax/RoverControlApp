using Godot;
using System;

public partial class IKAnimHelper : Node3D
{
	[Export] Node3D Part1 = null!;
	[Export] Node3D Part2 = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}
}
