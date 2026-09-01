using Godot;
using RoverControlApp.Core;
using System;
using System.ComponentModel;
using static RoverControlApp.Core.MqttClasses;

public partial class PanelElement : Button
{
	[Export] public RoboticArmTaskType _type = RoboticArmTaskType.Rotary;
	[Export] public string _item = "";
	[Export] public bool _skip_on_failure = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RoboticArmTask task = new() { task_type = _type, item = _item, skip_on_failure = _skip_on_failure };
		TooltipText = task.ToString();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
