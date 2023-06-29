using Godot;
using System;

namespace RoverControlApp.MVVM.ViewModel;

public partial class FadeLabel : Label
{
	[Export]
	public float StayTimeSeconds { get; set; } = 5f;

	[Export]
	public float FadeTimeSeconds { get; set; } = 5f;

	Godot.Timer stayTimer;

	float fadeTimeElapsed = 0f;

	void StartFading()
	{
		SetProcess(true);
	}

	public override void _Ready()
	{
		stayTimer = GetNode<Timer>("FadeTimer");
		SetProcess(false);
		Modulate = Colors.White;
		stayTimer.WaitTime = StayTimeSeconds;
		stayTimer.Connect(Timer.SignalName.Timeout, new Callable(this, MethodName.StartFading));
		stayTimer.Start();
	}

	public override void _Process(double delta)
	{
		fadeTimeElapsed += (float) delta;
		Modulate = new Color(Colors.White, (FadeTimeSeconds - fadeTimeElapsed) / FadeTimeSeconds);
		if (fadeTimeElapsed > FadeTimeSeconds)
			QueueFree();
	}
}
