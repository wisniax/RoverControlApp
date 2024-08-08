using Godot;
using System;

namespace RoverControlApp.MVVM.ViewModel;

public partial class TouchScreenControlsOverlay : Control
{
	//TODO change to TouchScreenButton
	void OnButtonStateChange(int keyCode, bool state)
	{
		InputEventAction actionEvent = new();
		actionEvent.Pressed = state;

		switch ((Key)keyCode)
		{
			case Key.Tab:
				actionEvent.Action = "ControlModeChange";
				break;
			default:
				return;
		}

		if (state)
			Input.ActionPress(actionEvent.Action);
		else
			Input.ActionRelease(actionEvent.Action);
		Input.ParseInputEvent(actionEvent);
	}
}
