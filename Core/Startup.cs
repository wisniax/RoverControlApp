using System.Linq;

using Godot;

using RoverControlApp.Core;
using RoverControlApp.Core.RoverControllerPresets;

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
		foreach (var eventName in actions)
		{
			var events = InputMap.ActionGetEvents(eventName);
			var deadzone = InputMap.ActionGetDeadzone(eventName);

			InputMap.AddAction(eventName + "_0", deadzone);
			InputMap.AddAction(eventName + "_1", deadzone);

			DualSeatEvent.GenerateStrings(eventName);

			foreach (var ev in events)
			{
				if (ev is InputEventKey kev)
				{
					var kev0 = kev.DeepCopy();
					kev0.Device = 0;
					InputMap.ActionAddEvent(eventName + "_0", kev0);

					var kev1 = kev.DeepCopy();
					kev1.Device = 1;
					InputMap.ActionAddEvent(eventName + "_1", kev1);
				}
				else if (ev is InputEventJoypadButton jbev)
				{
					var jbev0 = jbev.DeepCopy();
					jbev0.Device = 0;
					InputMap.ActionAddEvent(eventName + "_0", jbev0);

					var jbev1 = jbev.DeepCopy();
					jbev1.Device = 1;
					InputMap.ActionAddEvent(eventName + "_1", jbev1);
				}
				else if (ev is InputEventJoypadMotion jmev)
				{
					var jmev0 = jmev.DeepCopy();
					jmev0.Device = 0;
					InputMap.ActionAddEvent(eventName + "_0", jmev0);

					var jmev1 = jmev.DeepCopy();
					jmev1.Device = 1;
					InputMap.ActionAddEvent(eventName + "_1", jmev1);
				}
			}
		}

		QueueFree();
		EventLogger.LogMessage("Startup", EventLogger.LogLevel.Verbose, "Loading finished!");
	}

}
