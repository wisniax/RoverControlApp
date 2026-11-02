using Godot;
using System;

public partial class IKAnimHelper : Node3D
{
	[Export] Node3D Part1 = null!;
	[Export] Node3D Part2 = null!;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}
}
