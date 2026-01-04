using Godot;
using System;

public partial class RoverVisualizer : Panel
{
	[Export] public Node3D RoverModelRoot = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	float rotationY = 0;
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
		rotationY += 0.03f;
		RoverModelRoot.GetNode<StaticBody3D>("StaticBody3D").Rotation = new Vector3(0, rotationY, 0);
	}
}
