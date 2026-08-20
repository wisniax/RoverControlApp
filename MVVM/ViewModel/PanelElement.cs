using Godot;
using System;
using System.ComponentModel;

public partial class PanelElement : Button
{
	[Export] public TaskType _type = TaskType.Rotary;
	[Export] public string _item = "";
	[Export] public bool _skip_on_failure = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ArmAutonomyTask task = new() { type = _type, item = _item, skip_on_failure = _skip_on_failure };
		TooltipText = task.ToString();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
