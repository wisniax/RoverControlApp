using System.Linq;

using Godot;

using RoverControlApp.Core;

namespace RoverControlApp.MVVM.ViewModel;

public partial class Startup : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		DisplayServer.WindowSetMinSize(new Vector2I(800, 450));
		EventLogger.LogMessage("Startup", EventLogger.LogLevel.Verbose, "Loading MainView");
		var mainView_PS = ResourceLoader.Load<PackedScene>("res://MVVM/View/MainView.tscn");
		var mainView = mainView_PS.Instantiate();
		GetTree().Root.CallDeferred(MethodName.AddChild, mainView);

		//for dual seat
		var actions = InputMap.GetActions().SkipWhile((m) => m.ToString().StartsWith("ui"));
		foreach (var item in actions)
		{
			var events0 = InputMap.ActionGetEvents(item);
			var events1 = InputMap.ActionGetEvents(item);
			var deadzone = InputMap.ActionGetDeadzone(item);

			InputMap.AddAction(item + "_0", deadzone);
			InputMap.AddAction(item + "_1", deadzone);

			foreach (var ev in events0)
			{
				ev.Device = 0;
				InputMap.ActionAddEvent(item + "_0", ev);
			}

			foreach (var ev in events1)
			{
				ev.Device = 1;
				InputMap.ActionAddEvent(item + "_1", ev);
			}
		}

		QueueFree();
		EventLogger.LogMessage("Startup", EventLogger.LogLevel.Verbose, "Loading finished!");
	}

}
