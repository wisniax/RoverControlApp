using Godot;
using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoverControlApp.MVVM.Model;

public abstract partial class UIOverlay : Control
{

	int _controlMode;

	public abstract Dictionary<int, Setting> Presets { get; } 

	[Export]
	AnimationPlayer Animator = null!;

	[Export]
	NodePath BackgroundNodePath = null!;
	[Export]
	NodePath LabelNodePath = null!;

	string BackgroundColorAP => $"{BackgroundNodePath}:color";
	string FontColorAP => $"{LabelNodePath}:theme_override_colors/font_color";
	string TextAP => $"{LabelNodePath}:text";


	private long lastChangeTimestamp = 0;

	public override void _Ready()
	{
		//create local animation
		Animation anim = new();

		anim.Length = 1;

		var bgColorTrackIdx = anim.AddTrack(Animation.TrackType.Value);
		var fontColorTrackIdx = anim.AddTrack(Animation.TrackType.Value);
		var textTrackIdx = anim.AddTrack(Animation.TrackType.Value);

		anim.TrackSetPath(bgColorTrackIdx, BackgroundColorAP);
		anim.TrackSetInterpolationType(bgColorTrackIdx, Animation.InterpolationType.Linear);
		anim.TrackInsertKey(bgColorTrackIdx, 0.0, Colors.Gray);
		anim.TrackInsertKey(bgColorTrackIdx, 1.0, Colors.LightGray);

		anim.TrackSetPath(fontColorTrackIdx, FontColorAP);
		anim.TrackSetInterpolationType(fontColorTrackIdx, Animation.InterpolationType.Linear);
		anim.TrackInsertKey(fontColorTrackIdx, 0.0, Colors.White);
		anim.TrackInsertKey(fontColorTrackIdx, 1.0, Colors.White);	
		
		anim.TrackSetPath(textTrackIdx, TextAP);
		anim.TrackSetInterpolationType(textTrackIdx, Animation.InterpolationType.Linear);
		anim.TrackInsertKey(textTrackIdx, 0.0, "Val A");
		anim.TrackInsertKey(textTrackIdx, 0.4, "Val");
		anim.TrackInsertKey(textTrackIdx, 0.6, "Val");
		anim.TrackInsertKey(textTrackIdx, 1.0, "Val B");

		AnimationLibrary animLib = new();
		animLib.AddAnimation("swap", anim);
		Animator.AddAnimationLibrary("local", animLib);

		ControlMode = 0;
	}

	private void SetupAnimSwap(int from, int to)
	{
		int track, key;
		var anim = Animator.GetAnimation("local/swap");

		//from
		track = anim.FindTrack(BackgroundColorAP, Animation.TrackType.Value);
		key = anim.TrackFindKey(track, 0.0);
		anim.TrackSetKeyValue(track, key, Presets[from].BackColor);
		key = anim.TrackFindKey(track, 1.0);
		anim.TrackSetKeyValue(track, key, Presets[to].BackColor);

		track = anim.FindTrack(FontColorAP, Animation.TrackType.Value);
		key = anim.TrackFindKey(track, 0.0);
		anim.TrackSetKeyValue(track, key, Presets[from].FontColor);
		key = anim.TrackFindKey(track, 1.0);
		anim.TrackSetKeyValue(track, key, Presets[to].FontColor);

		track = anim.FindTrack(TextAP, Animation.TrackType.Value);
		key = anim.TrackFindKey(track, 0.0);
		anim.TrackSetKeyValue(track, key, Presets[from].Text);
		key = anim.TrackFindKey(track, 0.4);
		anim.TrackSetKeyValue(track, key, Presets[from].PermanentText);
		key = anim.TrackFindKey(track, 0.6);
		anim.TrackSetKeyValue(track, key, Presets[to].PermanentText);
		key = anim.TrackFindKey(track, 1.0);
		anim.TrackSetKeyValue(track, key, Presets[to].Text);

	}

	public int ControlMode
	{
		get => _controlMode;
		set
		{
			CallDeferred(MethodName.OnSetControlMode, _controlMode, value);
			_controlMode = value;
		}
	}

	private void OnSetControlMode(int old, int @new)
	{
		SetupAnimSwap(old, @new);
		if (Animator.IsPlaying() || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastChangeTimestamp < 1000)
		{
			Animator.Play("local/swap");
			Animator.Seek(1);
		}
		else
			Animator.Play("local/swap");
		lastChangeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}
		
	public struct Setting
	{

		public Setting(Color backColor, Color fontColor, string text, string permanentText = "")
		{
			BackColor = backColor;
			FontColor = fontColor;
			Text = text;
			PermanentText = permanentText;
	}

		public Color BackColor { get; set; }

		public Color FontColor { get; set; }

		public string Text { get; set; }
		public string PermanentText { get; set; }
	}
}
