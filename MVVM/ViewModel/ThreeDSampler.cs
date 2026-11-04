using Godot;
using System;

public partial class ThreeDSampler : Node3D
{
	[Export] private Node3D _platformMover = null!;
	[Export] private Node3D _drillMover = null!;

	private DateTime _lastUpdateTime = DateTime.MinValue;

	public void UpdateSamplerAxis(int axisNumber, double value)
	{
		// TODO: Implement the logic to update the sampler's axis based on the axisNumber and value.
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
