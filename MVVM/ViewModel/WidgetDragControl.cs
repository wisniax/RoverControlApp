using Godot;
using System;
using System.Linq;

public partial class WidgetDragControl : Control
{
	private bool _isResized = false;

	[Signal]
	public delegate void ResizeZoneEventHandler(InputEventMouseMotion eventMouseMotion, LayoutPreset layoutPreset);

	[Signal]
	public delegate void DragEventHandler(InputEventMouseMotion eventMouseMotion);


	private void OnResizeZoneGuiEvent(InputEvent inputEvent, LayoutPreset layoutPreset)
	{
		if (inputEvent is not InputEventMouseMotion eventMouseMotion)
		{
			return;
		}

		AcceptEvent();

		EmitSignal(SignalName.ResizeZone, eventMouseMotion, (int)layoutPreset);
	}

	private void OnDragGuiEvent(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseMotion eventMouseMotion)
		{
			return;
		}

		AcceptEvent();

		EmitSignal(SignalName.Drag, eventMouseMotion);
	}
}
