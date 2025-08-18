using Godot;

namespace RoverControlApp.MVVM.ViewModel;

public partial class WidgetManagerItem : PanelContainer
{
	private string _itemString = "";

	private bool _widgetStateWindow = false;
	private bool _widgetStateInvisible = false;

	[ExportGroup(".internal", "_")]
	[Export]
	private Label _itemLabel = null!;

	[Export]
	private Button _windowBtn = null!;

	[Export]
	private Button _visibleBtn = null!;

	[Export]
	private Button _trashBtn = null!;

	[Export]
	public string ItemString
	{
		get => _itemString;
		set
		{
			_itemString = value;
			if (IsInsideTree())
			{
				_itemLabel.Text = _itemString;
			}
		}
	}

	[Export]
	public bool WidgetStateWindow
	{
		get => _widgetStateWindow;
		set
		{
			_widgetStateWindow = value;
			if (IsInsideTree())
			{
				_windowBtn.CallDeferred(Button.MethodName.SetPressedNoSignal, _widgetStateWindow);
			}
		}
	}

	[Export]
	public bool WidgetStateInvisible
	{
		get => _widgetStateInvisible;
		set
		{
			_widgetStateInvisible = value;
			if (IsInsideTree())
			{
				_visibleBtn.CallDeferred(Button.MethodName.SetPressedNoSignal, _widgetStateInvisible);
			}
		}
	}

	[Signal]
	public delegate void WidgetOrderSwapEventHandler(WidgetManagerItem from, WidgetManagerItem to);

	[Signal]
	public delegate void WidgetWindowButtonEventHandler(bool windowMode);

	[Signal]
	public delegate void WidgetVisibleButtonEventHandler(bool windowMode);

	[Signal]
	public delegate void WidgetTrashButtonEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ItemString = _itemString;
		WidgetStateWindow = _widgetStateWindow;
		WidgetStateInvisible = _widgetStateInvisible;
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.Obj is WidgetManagerItem wmi)
		{
			return wmi != this;
		}
		return false;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (data.Obj is WidgetManagerItem wmi)
		{
			EmitSignal(SignalName.WidgetOrderSwap, this, wmi);
		}
	}

	private void OnWindowButton(bool toggled)
	{
		_widgetStateWindow = toggled;
		EmitSignal(SignalName.WidgetWindowButton, toggled);
	}

	private void OnVisibleButton(bool toggled)
	{
		_widgetStateInvisible = toggled;
		EmitSignal(SignalName.WidgetVisibleButton, toggled);
	}

	private void OnTrashButton() => EmitSignal(SignalName.WidgetWindowButton);

	public Variant GetDragData(Vector2 atPosition)
	{
		PanelContainer panel = new() { ThemeTypeVariation = "PanelContainerRcaSemiTransparent" };
		panel.AddChild(new Label { Text = "\xea52 " + _itemString });
		SetDragPreview(panel);
		return this;
	}
}
