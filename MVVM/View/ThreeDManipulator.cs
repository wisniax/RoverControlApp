using Godot;
using System;

public partial class ThreeDManipulator : Node3D
{
	[Export] private Node3D _axis1 = null!;
	[Export] private Node3D _axis2 = null!;
	[Export] private Node3D _axis3 = null!;
	[Export] private Node3D _axis4 = null!;
	[Export] private Node3D _axis5 = null!;
	[Export] private Node3D _axis6 = null!;
	[Export] private Node3D _gripper = null!;

	public void UpdateManipulatorAxis(int axisNumber, double value)
	{
		switch (axisNumber)
		{
			case 0:
				_axis1.RotationDegrees = new Vector3(0, 0, (float)value);
				break;
			case 1:
				_axis2.RotationDegrees = new Vector3((float)value, 0, 0);
				break;
			case 2:
				_axis3.RotationDegrees = new Vector3((float)value, 0, 0);
				break;
			case 3:
				_axis4.RotationDegrees = new Vector3(0, (float)value, 0);
				break;
			case 4:
				_axis5.RotationDegrees = new Vector3((float)value, 0, 0);
				break;
			case 5:
				_axis6.RotationDegrees = new Vector3(0, 0, (float)value));
				break;
			case 6:
				//_gripper.RotationDegrees = new Vector3(0, 0, (float)value);
				break;
			default:
				GD.PrintErr($"Invalid axis number: {axisNumber}");
				break;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
