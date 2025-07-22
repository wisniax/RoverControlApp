using Godot;

namespace RoverControlApp.MVVM.ViewModel;

public partial class WidgetWindow : Window
{
	bool _resizeStarted = false;
	Rect2 _resizeInitialRect;
	Vector2 _mouseMoveStart;

	public void OnDragWindowBar(InputEvent @event)
	{
		if (@event is InputEventMouseMotion inputEventMouseMotion)
			OnDrag(inputEventMouseMotion);
	}

	public void OnDrag(InputEventMouseMotion eventMouseMotion)
	{
		if (!eventMouseMotion.ButtonMask.HasFlag(MouseButtonMask.Left))
		{
			_resizeStarted = false;
			return;
		}

		if (!_resizeStarted)
		{
			_resizeStarted = true;
			_mouseMoveStart = GetParent().GetViewport().GetMousePosition();
			_resizeInitialRect.Size = Size;
			_resizeInitialRect.Position = Position;
		}

		Vector2 mouseRelativeToStart = GetParent().GetViewport().GetMousePosition() - _mouseMoveStart;

		Position = (Vector2I)(_resizeInitialRect.Position + mouseRelativeToStart);
	}
}
