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
		if (Part1 == null || Part2 == null)
			return;

		AimAt(Part1, Part2);
		AimAt(Part2, Part1);
	}

	void AimAt(Node3D subject, Node3D target)
	{
		Vector3 normal = subject.GlobalBasis.Y;
		subject.LookAt(target.GlobalPosition, normal);
	}
}
