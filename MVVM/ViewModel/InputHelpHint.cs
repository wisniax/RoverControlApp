using System.Collections.Generic;
using System.Linq;
using System.Text;

using Godot;

using RoverControlApp.Core;

using HintInit = System.Collections.Generic.KeyValuePair<string, Godot.Collections.Array<Godot.InputEvent>>;

namespace RoverControlApp.MVVM.ViewModel;

[Tool]
public partial class InputHelpHint : Control
{
	#region Enums

	public enum HintVisibility
	{
		None = 0,
		Kb = 1,
		Joy = 2,
	}

	#endregion Enums


	#region Fields

	[ExportGroup(".internal", "_")]

	[Export]
	private HBoxContainer _kbHelp = null!;

	[Export]
	private HBoxContainer _joyHelp = null!;

	[Export]
	private Label _kbHelpEventLabel = null!;

	[Export]
	private Label _joyHelpEventLabel = null!;

	[Export]
	private Label _kbHelpActionLabel = null!;

	[Export]
	private Label _joyHelpActionLabel = null!;

	private HintInit _hintInformation = new("<ACTION_INVALID>", []);
	private HintVisibility _hintType = HintVisibility.Kb;
	private bool _showEmpty = false;

	private List<string> _kbHints = [];
	private List<string> _joyHints = [];

	#endregion Fields

	#region Properties

	public HintInit HintInformation
	{
		get => _hintInformation;
		set
		{
			_hintInformation = value;
			if (IsInsideTree())
				CallDeferred(MethodName.SetupHint);
		}
	}

	[Export]
	public HintVisibility HintType
	{
		get => _hintType;
		set
		{
			_hintType = value;
			if (IsInsideTree())
				CallDeferred(MethodName.ChangeVisible);
		}
	}

	[Export]
	public bool ShowEmpty
	{
		get => _showEmpty;
		set
		{
			_showEmpty = value;
			if (IsInsideTree())
				CallDeferred(MethodName.ChangeVisible);
		}
	}

	[Export]
	public Color ActionActiveColor = Colors.LimeGreen;

	[Export]
	public Color ActionStaticColor = Colors.White;

	#endregion Properties

	#region Godot

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetupHint();
		ChangeVisible();
	}

	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			float actionStr = Input.GetActionStrength(_hintInformation.Key, exactMatch: true);

			Color newColor = ActionStaticColor.Lerp(
				ActionActiveColor,
				actionStr
			);

			_kbHelpActionLabel.AddThemeColorOverride(
				"font_color",
				newColor
			);

			_joyHelpActionLabel.AddThemeColorOverride(
				"font_color",
				newColor
			);
		}
	}

	public override Vector2 _GetMinimumSize()
	{
		return HintType switch
		{
			HintVisibility.Kb when _kbHelp.Visible => _kbHelp.GetCombinedMinimumSize(),
			HintVisibility.Joy when _joyHelp.Visible => _joyHelp.GetCombinedMinimumSize(),
			_ => Vector2.Zero,
		};
	}

	#endregion Godot

	#region Methods

	private string GetJoyAxisString(in InputEventJoypadMotion inputJoyAxis)
	{
		StringBuilder eventStrBuilder = new();
		eventStrBuilder.Append("JAx_");

		switch (inputJoyAxis.Axis)
		{
			case JoyAxis.LeftX:
			case JoyAxis.LeftY:
				eventStrBuilder.Append($"JoyLeft");
				break;
			case JoyAxis.RightX:
			case JoyAxis.RightY:
				eventStrBuilder.Append($"JoyRight");
				break;
			case JoyAxis.TriggerLeft:
			case JoyAxis.TriggerRight:
				eventStrBuilder.Append(inputJoyAxis.Axis);
				break;
		}

		switch (inputJoyAxis.Axis)
		{
			case JoyAxis.RightX when inputJoyAxis.AxisValue < 0:
			case JoyAxis.LeftX when inputJoyAxis.AxisValue < 0:
				eventStrBuilder.Append($"_ToLeft");
				break;
			case JoyAxis.RightX when inputJoyAxis.AxisValue > 0:
			case JoyAxis.LeftX when inputJoyAxis.AxisValue > 0:
				eventStrBuilder.Append($"_ToRight");
				break;
			case JoyAxis.RightY when inputJoyAxis.AxisValue < 0:
			case JoyAxis.LeftY when inputJoyAxis.AxisValue < 0:
			case JoyAxis.TriggerLeft when inputJoyAxis.AxisValue > 0:
			case JoyAxis.TriggerRight when inputJoyAxis.AxisValue > 0:
				eventStrBuilder.Append($"_ToDown");
				break;
			case JoyAxis.RightY when inputJoyAxis.AxisValue > 0:
			case JoyAxis.LeftY when inputJoyAxis.AxisValue > 0:
			case JoyAxis.TriggerLeft when inputJoyAxis.AxisValue < 0:
			case JoyAxis.TriggerRight when inputJoyAxis.AxisValue < 0:
				eventStrBuilder.Append($"_ToUp");
				break;
		}
		return eventStrBuilder.ToString();
	}

	private void SetupHint()
	{
		_kbHints.Clear();
		_joyHints.Clear();
		foreach (var input in _hintInformation.Value.Where((i) => i is InputEventKey))
		{
			if (input is InputEventKey inputKbKey)
			{
				if (inputKbKey.Keycode != Key.None)
					_kbHints.Add($"KBt_{OS.GetKeycodeString(inputKbKey.Keycode).Replace(' ', '_')}");
				if (inputKbKey.KeyLabel != Key.None)
					_kbHints.Add($"KBt_{OS.GetKeycodeString(inputKbKey.KeyLabel).Replace(' ', '_')}");
				if (inputKbKey.PhysicalKeycode != Key.None)
					_kbHints.Add($"KBt_{OS.GetKeycodeString(inputKbKey.PhysicalKeycode).Replace(' ', '_')}");
			}
		}

		foreach (var input in _hintInformation.Value.Where((i) => i is InputEventJoypadButton or InputEventJoypadMotion))
		{
			if (input is InputEventJoypadButton inputJoyKey && inputJoyKey.ButtonIndex != JoyButton.Invalid)
				_joyHints.Add($"JBt_{inputJoyKey.ButtonIndex}");
			if (input is InputEventJoypadMotion inputJoyAxis && inputJoyAxis.Axis != JoyAxis.Invalid)
				_joyHints.Add(GetJoyAxisString(inputJoyAxis));
		}

		_kbHelpActionLabel.Text = _hintInformation.Key;
		_joyHelpActionLabel.Text = _hintInformation.Key;

		EventLogger.LogMessageDebug(nameof(InputHelpHint) + $"/{_hintInformation.Key}", EventLogger.LogLevel.Verbose, $"K:{_kbHints.Count} J:{_joyHints.Count}");

		CycleHints(0);
	}

	private void ChangeVisible()
	{
		switch (HintType)
		{
			case HintVisibility.Kb when _kbHints.Count > 0 || _showEmpty:
				_kbHelp.Visible = true;
				_joyHelp.Visible = false;
				Visible = true;
				break;
			case HintVisibility.Joy when _joyHints.Count > 0 || _showEmpty:
				_kbHelp.Visible = false;
				_joyHelp.Visible = true;
				Visible = true;
				break;
			default:
				_kbHelp.Visible = false;
				_joyHelp.Visible = false;
				Visible = false;
				break;
		}

		UpdateMinimumSize();
	}

	public void CycleHints(uint cycle)
	{
		if (_kbHints.Count > 0)
			_kbHelpEventLabel.Text = _kbHints[(int)(cycle % (uint)_kbHints.Count)];
		else
			_kbHelpEventLabel.Text = "<KBt_INVALID>";

		if (_joyHints.Count > 0)
			_joyHelpEventLabel.Text = _joyHints[(int)(cycle % (uint)_joyHints.Count)];
		else
			_joyHelpEventLabel.Text = "<JBt_INVALID>";

		UpdateMinimumSize();
	}

	#endregion Methods
}
