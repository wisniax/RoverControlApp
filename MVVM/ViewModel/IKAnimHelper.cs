using Godot;
using System;

[Tool]
public partial class IKAnimHelper : Node3D
{
	[Export] Node3D Part1 = null!;

	[Export] Node3D Part2 = null!;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Part1.LookAt(Part2.GlobalPosition, Vector3.Up);
		Part2.LookAt(Part1.GlobalPosition, Vector3.Up);
	}
}
