using System.Collections.Generic;

using Godot;

using ActionEventArray = Godot.Collections.Array<Godot.InputEvent>;
using ActionEventDict = System.Collections.Generic.Dictionary<string, Godot.Collections.Array<Godot.InputEvent>>;

namespace RoverControlApp.Core.RoverControllerPresets;

public interface IActionAwareController
{
    public static (string, ActionEventArray) FetchActionEvents(string actionName)
    {
        if (!InputMap.HasAction(actionName))
        {
            EventLogger.LogMessage(nameof(IActionAwareController), EventLogger.LogLevel.Error, $"Action '{actionName}' is undefined!");
        }
        return new(actionName, InputMap.ActionGetEvents(actionName));
    }

    public static ActionEventDict FetchAllActionEvents(IEnumerable<string> actions)
    {
        ActionEventDict all = [];

        foreach (var item in actions)
        {
            var (actionName, eventArray) = FetchActionEvents(item);
            if (eventArray.Count == 0)
            {
                EventLogger.LogMessage(nameof(IActionAwareController), EventLogger.LogLevel.Warning, $"Action '{actionName}' have no input mapped!");
            }
            all.Add(actionName, eventArray);
        }

        return all;
    }

    public ActionEventDict GetInputActions();

    public string GetInputActionsAdditionalNote() => string.Empty;
}