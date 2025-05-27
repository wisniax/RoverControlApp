using System.Collections.Generic;
using System.Linq;

using Godot;

using RoverControlApp.Core.RoverControllerPresets;

using HintInit = System.Collections.Generic.KeyValuePair<string, Godot.Collections.Array<Godot.InputEvent>>;
using InputArray = Godot.Collections.Array<Godot.InputEvent>;

public partial class InputHelpMaster : PanelContainer
{
	#region Fields

	[ExportGroup(".internal", "_")]
	[Export]
	private PackedScene _inputHelpHintScene = null!;

	[Export]
	private Timer _cycleTimer = null!;

	[Export]
	private Control _kindergarden = null!;

	[Export]
	private Label _additionalNotesHeadLabel = null!;

	[Export]
	private Label _additionalNotesValueLabel = null!;

	[Export]
	private HBoxContainer _headerBox = null!;

	[Export]
	private ScrollContainer _headerScroll = null!;

	[Export]
	private ScrollContainer _kinderScroll = null!;

	private bool _cycleTimerEnabled = false;

	private uint _hintCycle = 0;

	private List<InputHelpHint> _inputHelpHints = [];

	private InputHelpHint.HintVisibility _hintType;
	private bool _reactiveHints;

	private IActionAwareController[] _actionAwareControllers = [];
	private Dictionary<string, InputArray> _actionsCache = [];
	private Color _reactiveHintStateActive;
	private Color _reactiveHintStateStatic;

	private bool _showEmpty;

	private bool _showAdditionalNotes;

	private string _additionalNotes = "";

	#endregion Fields

	#region Properties

	[Export]
	public InputHelpHint.HintVisibility HintType
	{
		get => _hintType;
		set
		{
			_hintType = value;
			if (IsInsideTree())
				CallDeferred(MethodName.UpdateHintVisibility);
		}
	}

	/// <summary>
	/// Hints will glow upon action input.
	/// </summary>
	[Export]
	public bool ReactiveHints
	{
		get => _reactiveHints;
		set
		{
			_reactiveHints = value;
			if (IsInsideTree())
				CallDeferred(MethodName.GenerateHints, _reactiveHints);
		}
	}

	public Color ReactiveHintStateActive
	{
		get => _reactiveHintStateActive;
		set
		{
			_reactiveHintStateActive = value;
			if (IsInsideTree())
				CallDeferred(MethodName.ApplyColors);
		}
	}
	public Color ReactiveHintStateStatic
	{
		get => _reactiveHintStateStatic;
		set
		{
			_reactiveHintStateStatic = value;
			if (IsInsideTree())
				CallDeferred(MethodName.ApplyColors);
		}
	}

	public IActionAwareController[] ActionAwareControllers
	{
		get => _actionAwareControllers;
		set
		{
			if (_actionAwareControllers.SequenceEqual(value))
				return;
			_actionAwareControllers = value;
			ExtractActionsFromControllers();
			if (IsInsideTree())
				CallDeferred(MethodName.GenerateHints, _reactiveHints);
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
				CallDeferred(MethodName.UpdateHintVisibility);
		}
	}

	[Export]
	public bool CycleHints
	{
		get => _cycleTimerEnabled;
		set
		{
			_cycleTimerEnabled = value;
			if (!IsInsideTree())
				return;
			if (value)
					_cycleTimer?.CallDeferred(Timer.MethodName.Start, 0.0);
				else
					_cycleTimer?.CallDeferred(Timer.MethodName.Stop);
		}
	}
	
	[Export]
	public bool ShowAdditionalNotes
	{
		get => _showAdditionalNotes;
		set
		{
			_showAdditionalNotes = value;
			if (IsInsideTree())
					CallDeferred(MethodName.UpdateAdditionalNotesVisibility);
		}
	}

	#endregion Properties

	#region Godot

	public override void _Ready()
	{
		OnKindergardenResize();
		if (_cycleTimerEnabled)
			_cycleTimer.Start();
	}

    public override void _Process(double delta)
    {
		_headerScroll.ScrollHorizontal = _kinderScroll.ScrollHorizontal;
    }

    public override void _GuiInput(InputEvent @event)
	{
		if (@event.IsActionPressed("input_help_cycle_hint_up", allowEcho: false, exactMatch: true))
		{
			_hintCycle++;
			CallDeferred(MethodName.UpdateHintCycle);
		}
		if (@event.IsActionPressed("input_help_cycle_hint_down", allowEcho: false, exactMatch: true))
		{
			_hintCycle--;
			CallDeferred(MethodName.UpdateHintCycle);
		}
	}
	
	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (Input.IsActionJustPressed("input_help_cycle_hint_stop", exactMatch: true) && _cycleTimerEnabled)
		{
			_cycleTimer.CallDeferred(Timer.MethodName.Stop);
		}
		else if (@Input.IsActionJustReleased("input_help_cycle_hint_stop", exactMatch: true) && _cycleTimerEnabled)
		{
			_cycleTimer.CallDeferred(Timer.MethodName.Start, 0.0);
		}
	}


#endregion Godot

#region Methods

private void OnCycleTimer()
	{
		_hintCycle++;
		UpdateHintCycle();
	}

	private void UpdateHintCycle()
	{
		foreach (var kiddo in _inputHelpHints)
			kiddo.CycleHints(_hintCycle);
	}

	private void ExtractActionsFromControllers()
	{
		_actionsCache.Clear();
		_additionalNotes = "";
		foreach (var actionAwareController in _actionAwareControllers)
		{
			foreach (var action in actionAwareController.GetInputActions())
			{
				if (_actionsCache.ContainsKey(action.Key))
					continue;
				_actionsCache.Add(action.Key, action.Value);
			}
			string additionalNote = actionAwareController.GetInputActionsAdditionalNote();
			if (!string.IsNullOrWhiteSpace(additionalNote))
				_additionalNotes += $"{additionalNote}\n";
		}
		UpdateAdditionalNotesVisibility();
	}

	private void UpdateHintVisibility()
	{
		foreach (var kiddo in _inputHelpHints)
		{
			kiddo.HintType = _hintType;
			kiddo.ShowEmpty = _showEmpty;
		}
	}

	private void UpdateAdditionalNotesVisibility()
	{
		_additionalNotesHeadLabel.Visible = _showAdditionalNotes;
		_additionalNotesValueLabel.Visible = _showAdditionalNotes;
		_additionalNotesValueLabel.Text = string.IsNullOrWhiteSpace(_additionalNotes) ? "None." : _additionalNotes;
	}

	private void ApplyColors()
	{
		if (!_reactiveHints)
		{
			GenerateHints(_reactiveHints);
			return;
		}

		foreach (var kiddo in _inputHelpHints)
		{
			kiddo.ActionActiveColor = ReactiveHintStateActive;
			kiddo.ActionStaticColor = ReactiveHintStateStatic;
		}
	}

	private void Hint_New(string actionName, InputArray inputArray, bool allowProcessing)
	{
		var newHint = _inputHelpHintScene.Instantiate<InputHelpHint>();
		newHint.HintInformation = new HintInit(actionName, inputArray);
		_kindergarden.AddChild(newHint, @internal: InternalMode.Back);
		newHint.ProcessMode = allowProcessing ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
		newHint.HintType = _hintType;
		newHint.ShowEmpty = _showEmpty;
		_inputHelpHints.Add(newHint);
	}

	private void GenerateHints(bool allowProcessing = true)
	{
		foreach (var kiddo in _inputHelpHints)
		{
			_kindergarden.RemoveChild(kiddo);
			kiddo.QueueFree();
		}

		_inputHelpHints.Clear();

		foreach (var action in _actionsCache)
			Hint_New(action.Key, action.Value, allowProcessing);

		UpdateAdditionalNotesVisibility();
	}

	private void OnKindergardenResize()
	{
		_headerBox.CustomMinimumSize = _kindergarden.Size with { Y = 0 };
	}

    public void GenerateHints() => GenerateHints(_reactiveHints);

    #endregion Methods
}
