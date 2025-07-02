using System;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

[Tool]
public partial class UIOverlay2 : PanelContainer
{
	public enum AnimationAlert
	{
		Off,
		AlertSoft_Slow,
		AlertSoft_Normal,
		AlertSoft_Fast,
		AlertHard_Slow,
		AlertHard_Normal,
		AlertHard_Fast,
	}

	private bool _animateAll = false;
	private bool _noAnimation = false;
	private bool _noBackground = false;

	private AnimationAlert _alertMode = AnimationAlert.Off;

	private int _controlMode = 0;

	private Godot.Collections.Array<UIOverlaySetting> _presets = [];

	private string _permanentText = "Permanent:";
	private string _variableLabelPrefixExText = "";
	private string _variableLabelSurfixExText = "";

	private int _fontSize = 20;
	private int _fontPrefixSizeEx = 20;
	private int _fontSurfixSizeEx = 20;
	private int _fontPrefixSize = 20;
	private int _fontSurfixSize = 20;

	[ExportGroup(".internal", "_")]
	[Export]
	AnimationPlayer _animator = null!;

	[Export]
	AnimationPlayer _animatorAlert = null!;

	[Export]
	NodePath _backgroundNodePath = null!;
	[Export]
	NodePath _animatedLabelNodePath = null!;

	[Export]
	HBoxContainer _variableTextRegion = null!;

	[Export]
	Label? _staticLabel;

	[Export]
	Label? _variableLabel;

	[Export]
	Label? _variableLabelPrefix;

	[Export]
	Label? _variableLabelSurfix;

	[Export]
	Label? _variableLabelPrefixEx;

	[Export]
	Label? _variableLabelSurfixEx;

	[Export]
	StyleBox _normalStyle = null!;

	[Export]
	StyleBox _noBgStyle = null!;


	[ExportGroup("Main Settings")]
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

	[ExportGroup("Main Settings")]
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

	[ExportGroup("Main Settings")]
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

	[ExportGroup("Main Settings")]
	[Export]
	public bool NoAnimation
	{
		get => _noAnimation;
		set
		{
			_noAnimation = value;
		}
	}

	[ExportGroup("Main Settings")]
	[Export]
	public AnimationAlert AlertMode
	{
		get => _alertMode;
		set
		{
			_alertMode = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.AlertModeInternal, (int)_alertMode);
			}
		}
	}

	[ExportGroup("Main Settings")]
	[Export]
	public bool NoBackground
	{
		get => _noBackground;
		set
		{
			_noBackground = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.AddThemeStyleboxOverride, "panel", _noBackground ? _noBgStyle : _normalStyle);
				_variableTextRegion.SetDeferred(PropertyName.SizeFlagsHorizontal, _noBackground ? (long)SizeFlags.ShrinkBegin : (long)SizeFlags.ShrinkCenter | (long)SizeFlags.Expand);
			}
		}
	}

	[ExportGroup("Main Settings")]
	[Export]
	public string PermanentText
	{
		get => _permanentText;
		set
		{
			_permanentText = value;
			if (IsInsideTree() && _staticLabel is not null)
			{
				_staticLabel.SetDeferred(Label.PropertyName.Text, _permanentText);
			}
		}
	}

	[ExportGroup("Main Settings")]
	[Export]
	public int FontSize
	{
		get => _fontSize;
		set
		{
			_fontSize = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.UpdateFontSize);
			}
		}
	}

	[ExportGroup("Main Settings")]
	[Export]
	public int FontPrefixSize
	{
		get => _fontPrefixSize;
		set
		{
			_fontPrefixSize = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.UpdateFontSize);
			}
		}
	}

	[ExportGroup("Main Settings")]
	[Export]
	public int FontSurfixSize
	{
		get => _fontSurfixSize;
		set
		{
			_fontSurfixSize = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.UpdateFontSize);
			}
		}
	}

	[ExportGroup("Main Settings")]
	[Export]
	public long RapidChangeTimeMiliseconds { get; set; } = 1000L;

	[ExportGroup("Main Settings")]
	[Export]
	public bool SkipWhenUpdatedButNotChanged { get; set; } = true;


	[ExportGroup("VariableText-External")]
	[Export]
	public string VariableTextPrefixEx
	{
		get => _variableLabelPrefixExText;
		set
		{
			_variableLabelPrefixExText = value;
			if (IsInsideTree() && _variableLabelPrefixEx is not null)
			{
				_variableLabelPrefixEx.SetDeferred(Label.PropertyName.Text, _variableLabelPrefixExText);
			}
		}
	}

	[ExportGroup("VariableText-External")]
	[Export]
	public string VariableTextSurfixEx
	{
		get => _variableLabelSurfixExText;
		set
		{
			_variableLabelSurfixExText = value;
			if (IsInsideTree() && _variableLabelSurfixEx is not null)
			{
				_variableLabelSurfixEx.SetDeferred(Label.PropertyName.Text, _variableLabelSurfixExText);
			}
		}
	}

	[ExportGroup("VariableText-External")]
	[Export]
	public int FontPrefixSizeEx
	{
		get => _fontPrefixSizeEx;
		set
		{
			_fontPrefixSizeEx = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.UpdateFontSize);
			}
		}
	}

	[ExportGroup("VariableText-External")]
	[Export]
	public int FontSurfixSizeEx
	{
		get => _fontSurfixSizeEx;
		set
		{
			_fontSurfixSizeEx = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.UpdateFontSize);
			}
		}
	}

	string BackgroundColorAP => $"{_backgroundNodePath}:self_modulate";
	string FontColorAP => $"{_animatedLabelNodePath}:theme_override_colors/font_color";
	string FontColorPrefixAP => $"{GetPathTo(_variableLabelPrefix)}:theme_override_colors/font_color";
	string FontColorSurfixAP => $"{GetPathTo(_variableLabelSurfix)}:theme_override_colors/font_color";
	string FontColorPrefixExAP => $"{GetPathTo(_variableLabelPrefixEx)}:theme_override_colors/font_color";
	string FontColorSurfixExAP => $"{GetPathTo(_variableLabelSurfixEx)}:theme_override_colors/font_color";
	string TextAP => $"{_animatedLabelNodePath}:text";
	string TextPrefixAP => $"{GetPathTo(_variableLabelPrefix)}:text";
	string TextSurfixAP => $"{GetPathTo(_variableLabelSurfix)}:text";




	private long _lastChangeTimestamp = 0;

	public override void _Ready()
	{
		CallDeferred(MethodName.Regenerate);
		UpdateFontSize();
		PermanentText = _permanentText; // force update
		VariableTextPrefixEx = _variableLabelPrefixExText; // force update
		VariableTextSurfixEx = _variableLabelSurfixExText; // force update
		NoBackground = _noBackground;
		AlertMode = _alertMode;
	}

	public void Regenerate()
	{
		GenerateAnimations();
		OnSetControlMode(_controlMode, _controlMode, true);
	}

	private bool IsValidControlMode(int num) => num < Presets.Count && num >= 0;

	private void CreateAnimationTrack_Color(Animation animation, string propertyPath, in Color from, in Color to)
	{
		var colorTrack = animation.AddTrack(Animation.TrackType.Value);
		animation.TrackSetPath(colorTrack, propertyPath);
		animation.TrackSetInterpolationType(colorTrack, Animation.InterpolationType.Linear);
		animation.TrackInsertKey(colorTrack, 0.0, from);
		animation.TrackInsertKey(colorTrack, 1.0, to);
	}

	private void CreateAnimationTrack_ColorConst(Animation animation, string propertyPath, in Color from, in Color to)
	{
		var colorTrack = animation.AddTrack(Animation.TrackType.Value);
		animation.TrackSetPath(colorTrack, propertyPath);
		animation.TrackSetInterpolationType(colorTrack, Animation.InterpolationType.Nearest);
		animation.TrackInsertKey(colorTrack, 0.0, from);
		animation.TrackInsertKey(colorTrack, 0.1, to);
	}

	private void CreateAnimationTrack_TextA(Animation animation, string propertyPath, in string from, in string to)
	{
		var textTrack = animation.AddTrack(Animation.TrackType.Value);
		animation.TrackSetPath(textTrack, propertyPath);
		animation.TrackSetInterpolationType(textTrack, Animation.InterpolationType.Linear);
		animation.TrackInsertKey(textTrack, 0.0, from);
		animation.TrackInsertKey(textTrack, 0.1, from);
		if (from != to)
		{
			animation.TrackInsertKey(textTrack, 0.45, "");
			animation.TrackInsertKey(textTrack, 0.55, "");
		}
		animation.TrackInsertKey(textTrack, 0.9, to);
	}

	private void CreateAnimationTrack_TextB(Animation animation, string propertyPath, in string from, in string to)
	{
		var textTrack = animation.AddTrack(Animation.TrackType.Value);
		animation.TrackSetPath(textTrack, propertyPath);
		animation.TrackSetInterpolationType(textTrack, Animation.InterpolationType.Linear);
		animation.TrackInsertKey(textTrack, 0.0, from);
		if (from != to)
		{
			animation.TrackInsertKey(textTrack, 0.3, "");
			animation.TrackInsertKey(textTrack, 0.7, "");
		}
		animation.TrackInsertKey(textTrack, 1.0, to);
	}

	private void CreateAnimationTrack_TextConst(Animation animation, string propertyPath, in string from, in string to)
	{
		var textTrack = animation.AddTrack(Animation.TrackType.Value);
		animation.TrackSetPath(textTrack, propertyPath);
		animation.TrackSetInterpolationType(textTrack, Animation.InterpolationType.Nearest);
		animation.TrackInsertKey(textTrack, 0.0, from);
		animation.TrackInsertKey(textTrack, 0.1, to);
	}

	private void CreateAnimation(int from, int to)
	{
		Animation anim = new()
		{
			Length = 1
		};

		Color BackColorF = Presets[from].UseFontAsBackColor ? Presets[from].FontColor : Presets[from].BackColor;
		Color BackColorT = Presets[to].UseFontAsBackColor ? Presets[to].FontColor : Presets[to].BackColor;

		CreateAnimationTrack_Color(anim, BackgroundColorAP, BackColorF, BackColorT);
		CreateAnimationTrack_Color(anim, FontColorAP, Presets[from].FontColor, Presets[to].FontColor);
		CreateAnimationTrack_TextA(anim, TextAP, Presets[from].Text, Presets[to].Text);

		if (_variableLabelPrefix is not null)
		{
			CreateAnimationTrack_Color(anim, FontColorPrefixAP, Presets[from].FontColor, Presets[to].FontColor);
			CreateAnimationTrack_TextB(anim, TextPrefixAP, Presets[from].TextPrefix, Presets[to].TextPrefix);
		}

		if (_variableLabelSurfix is not null)
		{
			CreateAnimationTrack_Color(anim, FontColorSurfixAP, Presets[from].FontColor, Presets[to].FontColor);
			CreateAnimationTrack_TextB(anim, TextSurfixAP, Presets[from].TextSurfix, Presets[to].TextSurfix);
		}

		if (_variableLabelPrefixEx is not null)
		{
			CreateAnimationTrack_Color(anim, FontColorPrefixExAP, Presets[from].FontColor, Presets[to].FontColor);
		}

		if (_variableLabelSurfixEx is not null)
		{
			CreateAnimationTrack_Color(anim, FontColorSurfixExAP, Presets[from].FontColor, Presets[to].FontColor);
		}

		_animator.GetAnimationLibrary("local").AddAnimation($"f{from}t{to}", anim);
	}

	private void CreateAnimation_Invalid()
	{
		Animation anim = new()
		{
			Length = 1
		};

		Color colorBg = Colors.DeepPink;
		Color colorFont = Colors.PaleVioletRed;

		CreateAnimationTrack_ColorConst(anim, BackgroundColorAP, colorBg, colorBg);
		CreateAnimationTrack_ColorConst(anim, FontColorAP, colorFont, colorFont);
		CreateAnimationTrack_TextConst(anim, TextAP, "#INVALID#", "#INVALID#");

		if (_variableLabelPrefix is not null)
		{
			CreateAnimationTrack_ColorConst(anim, FontColorPrefixAP, colorFont, colorFont);
			CreateAnimationTrack_TextConst(anim, TextPrefixAP, "", "");
		}

		if (_variableLabelSurfix is not null)
		{
			CreateAnimationTrack_ColorConst(anim, FontColorSurfixAP, colorFont, colorFont);
			CreateAnimationTrack_TextConst(anim, TextSurfixAP, "", "");
		}

		if (_variableLabelPrefixEx is not null)
		{
			CreateAnimationTrack_ColorConst(anim, FontColorPrefixExAP, colorFont, colorFont);
		}

		if (_variableLabelSurfixEx is not null)
		{
			CreateAnimationTrack_ColorConst(anim, FontColorSurfixExAP, colorFont, colorFont);
		}

		_animator.GetAnimationLibrary("local").AddAnimation($"invalid", anim);
	}

	private void OnSetControlMode(int from, int to, bool forceRapid = false)
	{
		if (Engine.IsEditorHint())
			return;

		if (to == from && SkipWhenUpdatedButNotChanged && !forceRapid)
			return;

		string animationToPlay = $"local/f{from}t{to}";

		if (NoAnimation)
		{
			forceRapid = true;
			animationToPlay = $"local/f{to}t{to}";
		}


		if (!IsValidControlMode(to))
		{
			EventLogger.LogMessage($"{nameof(UIOverlay2)}/{Name}", EventLogger.LogLevel.Error, $"Invalid control mode '{to}' was used!");
			animationToPlay = "local/invalid";
			forceRapid = true;
		}
		else if (!IsValidControlMode(from) || (!forceRapid && !_animator.GetAnimationLibrary("local").HasAnimation(animationToPlay.Split('/')[1]) ))
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
		if (Engine.IsEditorHint())
			return;
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
				if (!jellyfin.HasAnimation($"f{a}t{a}"))
					CreateAnimation(a, a); //self to self
				if (!jellyfin.HasAnimation($"f{a}t{b}"))
					CreateAnimation(a, b); //from to
				if (!jellyfin.HasAnimation($"f{b}t{a}"))
					CreateAnimation(b, a); //to from
			}
		}
	}

	private void UpdateFontSize()
	{
		_staticLabel?.AddThemeFontSizeOverride("font_size", _fontSize);
		_variableLabel?.AddThemeFontSizeOverride("font_size", _fontSize);
		_variableLabelPrefixEx?.AddThemeFontSizeOverride("font_size", _fontPrefixSizeEx);
		_variableLabelSurfixEx?.AddThemeFontSizeOverride("font_size", _fontSurfixSizeEx);
		_variableLabelPrefix?.AddThemeFontSizeOverride("font_size", _fontPrefixSize);
		_variableLabelSurfix?.AddThemeFontSizeOverride("font_size", _fontSurfixSize);
	}

	private void AlertModeInternal(int alertMode)
	{
		if (Engine.IsEditorHint())
			return;

		switch ((AnimationAlert)alertMode)
		{
			case AnimationAlert.AlertSoft_Slow:
				_animatorAlert.Play("alert_soft_1");
				break;
			case AnimationAlert.AlertSoft_Normal:
				_animatorAlert.Play("alert_soft_2");
				break;
			case AnimationAlert.AlertSoft_Fast:
				_animatorAlert.Play("alert_soft_3");
				break;
			case AnimationAlert.AlertHard_Slow:
				_animatorAlert.Play("alert_hard_1");
				break;
			case AnimationAlert.AlertHard_Normal:
				_animatorAlert.Play("alert_hard_2");
				break;
			case AnimationAlert.AlertHard_Fast:
				_animatorAlert.Play("alert_hard_3");
				break;
			default:
				_animatorAlert.Stop();
				break;
		}
	}

}
