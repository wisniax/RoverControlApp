using System;

using Godot;

using RoverControlApp.Core;

namespace RoverControlApp.MVVM.Model;

public partial class UIOverlay2 : Control
{
	private bool _animateAll = false;

	private int _controlMode = 0;

	private Godot.Collections.Array<UIOverlaySetting> _presets = [];

	private string _permanentText = "";

	[Export]
	public int ControlMode
	{
		get => _controlMode;
		set
		{
			var old = _controlMode;
			_controlMode = value;
			if (IsInsideTree())
				CallDeferred(MethodName.OnSetControlMode, old, _controlMode, false);
		}
	}

	[Export]
	public Godot.Collections.Array<UIOverlaySetting> Presets
	{
		get => _presets;
		set
		{
			_presets = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.GenerateAnimations);
				CallDeferred(MethodName.OnSetControlMode, _controlMode, _controlMode, true);
			}
		}
	}

	[Export]
	public string PermanentText
	{
		get => _permanentText;
		set
		{
			_permanentText = value;
			if (IsInsideTree() && _staticLabelNodePath is not null)
			{
				GetNode<Label>(_staticLabelNodePath).SetDeferred(Label.PropertyName.Text, _permanentText);
			}
		}
	}

	[Export]
	public long RapidChangeTimeMiliseconds { get; set; } = 1000L;

	[Export]
	public bool AnimateAll
	{
		get => _animateAll;
		set
		{
			_animateAll = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.GenerateAnimations);
				CallDeferred(MethodName.OnSetControlMode, _controlMode, _controlMode, true);
			}
		}
	}

	[ExportGroup(".internal", "_")]
	[Export]
	AnimationPlayer _animator = null!;

	[Export]
	NodePath _backgroundNodePath = null!;
	[Export]
	NodePath _animatedLabelNodePath = null!;

	[Export]
	NodePath? _staticLabelNodePath;


	string BackgroundColorAP => $"{_backgroundNodePath}:self_modulate";
	string FontColorAP => $"{_animatedLabelNodePath}:theme_override_colors/font_color";
	string TextAP => $"{_animatedLabelNodePath}:text";


	private long _lastChangeTimestamp = 0;

	public override void _Ready()
	{
		CallDeferred(MethodName.Regenerate);
		if(_staticLabelNodePath is not null)
			GetNode<Label>(_staticLabelNodePath).SetDeferred(Label.PropertyName.Text, _permanentText);
	}

	public void Regenerate()
	{
		GenerateAnimations();
		OnSetControlMode(_controlMode, _controlMode, true);
	}

	private bool IsValidControlMode(int num) => num < Presets.Count && num >= 0;

	private void CreateAnimation(int from, int to)
	{
		Animation anim = new()
		{
			Length = 1
		};

		var bgColorTrackIdx = anim.AddTrack(Animation.TrackType.Value);
		var fontColorTrackIdx = anim.AddTrack(Animation.TrackType.Value);
		var textTrackIdx = anim.AddTrack(Animation.TrackType.Value);

		anim.TrackSetPath(bgColorTrackIdx, BackgroundColorAP);
		anim.TrackSetInterpolationType(bgColorTrackIdx, Animation.InterpolationType.Linear);
		anim.TrackInsertKey(bgColorTrackIdx, 0.0, Presets[from].BackColor);
		anim.TrackInsertKey(bgColorTrackIdx, 1.0, Presets[to].BackColor);

		anim.TrackSetPath(fontColorTrackIdx, FontColorAP);
		anim.TrackSetInterpolationType(fontColorTrackIdx, Animation.InterpolationType.Linear);
		anim.TrackInsertKey(fontColorTrackIdx, 0.0, Presets[from].FontColor);
		anim.TrackInsertKey(fontColorTrackIdx, 1.0, Presets[to].FontColor);

		anim.TrackSetPath(textTrackIdx, TextAP);
		anim.TrackSetInterpolationType(textTrackIdx, Animation.InterpolationType.Linear);
		anim.TrackInsertKey(textTrackIdx, 0.0, Presets[from].Text);
		anim.TrackInsertKey(textTrackIdx, 0.5, "");
		anim.TrackInsertKey(textTrackIdx, 1.0, Presets[to].Text);

		_animator.GetAnimationLibrary("local").AddAnimation($"f{from}t{to}", anim);
	}

	private void CreateAnimation_Invalid()
	{
		Animation anim = new()
		{
			Length = 1
		};

		var bgColorTrackIdx = anim.AddTrack(Animation.TrackType.Value);
		var fontColorTrackIdx = anim.AddTrack(Animation.TrackType.Value);
		var textTrackIdx = anim.AddTrack(Animation.TrackType.Value);

		anim.TrackSetPath(bgColorTrackIdx, BackgroundColorAP);
		anim.TrackSetInterpolationType(bgColorTrackIdx, Animation.InterpolationType.Linear);
		anim.TrackInsertKey(bgColorTrackIdx, 0.0, Colors.DeepPink);
		anim.TrackInsertKey(bgColorTrackIdx, 1.0, Colors.DeepPink);

		anim.TrackSetPath(fontColorTrackIdx, FontColorAP);
		anim.TrackSetInterpolationType(fontColorTrackIdx, Animation.InterpolationType.Linear);
		anim.TrackInsertKey(fontColorTrackIdx, 0.0, Colors.White);
		anim.TrackInsertKey(fontColorTrackIdx, 1.0, Colors.White);

		anim.TrackSetPath(textTrackIdx, TextAP);
		anim.TrackSetInterpolationType(textTrackIdx, Animation.InterpolationType.Linear);
		anim.TrackInsertKey(textTrackIdx, 0.0, "#INVALID#");
		anim.TrackInsertKey(textTrackIdx, 1.0, "#INVALID#");

		_animator.GetAnimationLibrary("local").AddAnimation($"invalid", anim);
	}

	private void OnSetControlMode(int from, int to, bool forceRapid = false)
	{
		string animationToPlay = $"local/f{from}t{to}";

		if (!IsValidControlMode(to))
		{
			EventLogger.LogMessage($"{nameof(UIOverlay2)}/{Name}", EventLogger.LogLevel.Error, $"Invalid control mode '{to}' was used!");
			animationToPlay = "local/invalid";
			forceRapid = true;
		}
		else if (!IsValidControlMode(from) || !_animator.GetAnimationLibrary("local").HasAnimation(animationToPlay.Split('/')[1]))
		{
			EventLogger.LogMessage($"{nameof(UIOverlay2)}/{Name}", EventLogger.LogLevel.Warning, $"Animation '{animationToPlay}' was not found, skipping animation.");
			animationToPlay = $"local/f{to}t{to}";
			forceRapid = true;
		}

		if (_animator.IsPlaying() ||
						DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastChangeTimestamp < RapidChangeTimeMiliseconds ||
						forceRapid
					)
			{
				_animator.Play(animationToPlay);
				_animator.Seek(1);
			}
			else
				_animator.Play(animationToPlay);
		_lastChangeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}

	private void GenerateAnimations()
	{
		//clear
		if (_animator.HasAnimationLibrary("local"))
		{
			_animator.RemoveAnimationLibrary("local");
		}
		_animator.AddAnimationLibrary("local", new());
		var jellyfin = _animator.GetAnimationLibrary("local");

		CreateAnimation_Invalid();

		if (_animateAll)
		{
			for (int a = 0; a < Presets.Count; a++)
				for (int b = 0; b < Presets.Count; b++)
				{
					CreateAnimation(a, b);
				}
		}
		else
		{
			for (int a = 0; a < Presets.Count; a++)
			{
				var b = (a + 1) % Presets.Count;
				if(!jellyfin.HasAnimation($"f{a}t{a}"))
					CreateAnimation(a, a); //self to self
				if(!jellyfin.HasAnimation($"f{a}t{b}"))
					CreateAnimation(a, b); //from to
				if(!jellyfin.HasAnimation($"f{b}t{a}"))
					CreateAnimation(b, a); //to from
			}
		}
	}

}
