using Godot;
using System;

public partial class sampler_menu : Control
{
	private DateTime startTime;
	private Label runningTimeLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Find the Summary node
		Control summaryNode = GetNode<Control>("Summary");
		// Find the RunningTime label in the Summary node
		runningTimeLabel = summaryNode.GetNode<Label>("RunningTime");

		// Set the start time to the current time
		startTime = DateTime.Now;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Calculate the time since the program started running
		TimeSpan elapsedTime = DateTime.Now - startTime;

		// Set the label text to the current time
		runningTimeLabel.Text = elapsedTime.ToString(@"hh\:mm\:ss");
	}

	public void RunSelfCheck()
	{
		GD.Print("Running self check...");
	}

	public void LaunchControlPad()
	{
		GD.Print("Launching control pad...");
	}

	public void RunAnalysis()
	{
		GD.Print("Running analysis...");
	}
}
