using Godot;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.ViewModel;

public partial class UIOverlay : Control
{

	MqttClasses.ControlMode _controlMode = MqttClasses.ControlMode.EStop;

	private readonly Dictionary<MqttClasses.ControlMode, Setting> PRESET = new()
	{
		{ MqttClasses.ControlMode.EStop, new(Colors.DarkRed, Colors.Orange, "E-STOP") },
		{ MqttClasses.ControlMode.Rover, new(Colors.DarkGreen, Colors.LightGreen, "Driving") },
		{ MqttClasses.ControlMode.Manipulator, new(Colors.DarkOliveGreen, Colors.LightGreen, "Manipulator") },
		{ MqttClasses.ControlMode.Autonomy, new(Colors.DarkBlue, Colors.LightBlue, "Autonomy") }
	};

	AnimationPlayer roverModeAnimator;

	[Export]
	NodePath roverModeAnimatorPath;
	[Export]
	NodePath roverModeBgPath;
	[Export]
	NodePath roverModeLabelPath;

	private long lastChangeTimestamp = 0;

	public override void _Ready()
	{
		roverModeAnimator = GetNode<AnimationPlayer>(roverModeAnimatorPath);
		ControlMode = MqttClasses.ControlMode.EStop;
	}

	public Task ControlModeChangedSubscriber(MqttClasses.ControlMode newMode)
	{
		ControlMode = newMode;
		return Task.CompletedTask;
	}

	public void SetupAnimSwap(MqttClasses.ControlMode from, MqttClasses.ControlMode to)
	{
		int track, key;
		var anim = roverModeAnimator.GetAnimation("UIOverlay/swap");

		//from
		track = anim.FindTrack($"{roverModeBgPath}:color", Animation.TrackType.Value);
		key = anim.TrackFindKey(track, 0.0);
		anim.TrackSetKeyValue(track, key, PRESET[from].BackColor);

		track = anim.FindTrack($"{roverModeLabelPath}:theme_override_colors/font_color", Animation.TrackType.Value);
		key = anim.TrackFindKey(track, 1.0);
		anim.TrackSetKeyValue(track, key, PRESET[from].FontColor);

		track = anim.FindTrack($"{roverModeLabelPath}:text", Animation.TrackType.Value);
		key = anim.TrackFindKey(track, 0.0);
		anim.TrackSetKeyValue(track, key, PRESET[from].Text);

		//to
		track = anim.FindTrack($"{roverModeBgPath}:color", Animation.TrackType.Value);
		key = anim.TrackFindKey(track, 1.0);
		anim.TrackSetKeyValue(track, key, PRESET[to].BackColor);

		track = anim.FindTrack($"{roverModeLabelPath}:theme_override_colors/font_color", Animation.TrackType.Value);
		key = anim.TrackFindKey(track, 1.0);
		anim.TrackSetKeyValue(track, key, PRESET[to].FontColor);

		track = anim.FindTrack($"{roverModeLabelPath}:text", Animation.TrackType.Value);
		key = anim.TrackFindKey(track, 1.0);
		anim.TrackSetKeyValue(track, key, PRESET[to].Text);	
	}

	public MqttClasses.ControlMode ControlMode
	{
		get => _controlMode;
		set
		{
			SetupAnimSwap(_controlMode, value);
			if (roverModeAnimator.IsPlaying() || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastChangeTimestamp < 1000)
			{
				roverModeAnimator.Play("UIOverlay/swap");
				roverModeAnimator.Seek(1);
			}
			else
				roverModeAnimator.Play("UIOverlay/swap");
			lastChangeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			_controlMode = value;
		}
	}
		
	private struct Setting
	{

		public Setting(Color backColor, Color fontColor, string text)
		{
			BackColor = backColor;
			FontColor = fontColor;
			Text = text;
	}

		public Color BackColor { get; set; }

		public Color FontColor { get; set; }

		public string Text { get; set; }
	}
}
