using Godot;
using System;
using System.Linq;

public partial class WidgetDragControl : Control
{

	[Signal]
	public delegate void ResizeZoneEventHandler(InputEvent inputEvent, LayoutPreset layoutPreset);

	[Signal]
	public delegate void DragEventHandler(InputEvent inputEvent);

	private void OnResizeZoneGuiEvent(InputEvent inputEvent, LayoutPreset layoutPreset)
	{
		EmitSignal(SignalName.ResizeZone, inputEvent, (int)layoutPreset);
	}

	private void OnDragGuiEvent(InputEvent inputEvent)
	{
		EmitSignal(SignalName.Drag, inputEvent);
	}

	private void SetResizeZoneVisibilityOffOnMouseLeave()
	{
		if (new Godot.Rect2(Vector2.Zero, Size).HasPoint(GetLocalMousePosition()))
			return;

		SetResizeZoneVisiblility(false);
	}

	public void SetResizeZoneVisiblility(bool state)
	{
		foreach (Control child in GetChildren().Cast<Control>())
		{
			child.Visible = state;
		}
	}

}
