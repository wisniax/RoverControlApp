using Godot;

namespace RoverControlApp.MVVM.ViewModel;

public partial class WidgetDragControl : Control
{
	#region Fields
	private bool _isResized = false;
	private bool _showVisuals = true;
	private bool _processDrag = true;
	private bool _processResize = true;

	[ExportGroup(".internal", "_")]
	[Export]
	private Godot.Collections.Array<Control> _visualNodes = null!;

	[Export]
	private Control _resizeControl = null!;

	[Export]
	private Control _resizeControlFake = null!;

	#endregion Fields
	#region Events

	[Signal]
	public delegate void ResizeZoneEventHandler(InputEventMouseMotion eventMouseMotion, LayoutPreset layoutPreset);

	[Signal]
	public delegate void DragEventHandler(InputEventMouseMotion eventMouseMotion);

	#endregion Events
	#region Properties

	[Export]
	public bool ShowVisuals
	{
		get => _showVisuals;
		set
		{
			_showVisuals = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.ShowVisualsInternal, _showVisuals);
			}
		}
	}

	[Export]
	public bool ProcessDrag
	{
		get => _processDrag;
		set
		{
			_processDrag = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.AllowDrag, _processDrag);
			}
		}
	}

	[Export]
	public bool ProcessResize
	{
		get => _processResize;
		set
		{
			_processResize = value;
			if (IsInsideTree())
			{
				CallDeferred(MethodName.AllowResize, _processResize);
			}
		}
	}
	#endregion Properties
	#region Godot

	public override void _Ready()
	{
		ShowVisualsInternal(_showVisuals);
		AllowDrag(_processDrag);
		AllowResize(_processResize);
	}

	public override void _ExitTree()
	{
		_visualNodes.Clear();
		_visualNodes = null!;
	}

	#endregion Godot
	#region Methods

	private void OnResizeZoneGuiEvent(InputEvent inputEvent, LayoutPreset layoutPreset)
	{
		if (inputEvent is not InputEventMouseMotion eventMouseMotion || !ProcessResize)
		{
			return;
		}

		AcceptEvent();

		EmitSignal(SignalName.ResizeZone, eventMouseMotion, (int)layoutPreset);
	}

	private void OnDragGuiEvent(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseMotion eventMouseMotion || !ProcessDrag)
		{
			return;
		}

		AcceptEvent();

		EmitSignal(SignalName.Drag, eventMouseMotion);
	}

	private void ShowVisualsInternal(bool show)
	{
		foreach (var node in _visualNodes)
		{
			node.Visible = show;
		}
	}

	private void AllowDrag(bool allow)
	{
		MouseFilter = allow ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
		MouseDefaultCursorShape = allow ? CursorShape.Drag : CursorShape.Arrow;
	}

	private void AllowResize(bool allow)
	{
		_resizeControl.Visible = allow;
		_resizeControlFake.Visible = !allow;
	}

	#endregion Methods
}
