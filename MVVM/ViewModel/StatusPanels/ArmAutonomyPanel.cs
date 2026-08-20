using Godot;
using Onvif.Core.Client.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using RoverControlApp.Core;
using MQTTnet.Client;

public struct ArmAutonomyTask
{
    public TaskType type;
    public string item;
    public bool skip_on_failure;

    public override readonly string ToString()
    {
        var item_str = item != "" ? $"/{item}" : "";
        var skip_str = skip_on_failure ? "?" : "";
        return type.ToString() + item_str + skip_str;
    }
}

public struct TaskEntry
{
    public ArmAutonomyTask task;
    public int entry_id;
}

public enum TaskType
{
    Rotary,
    Engine,
    Breaker,
    Bar,
    Home,
    Ready,
}

public partial class ArmAutonomyPanel : Control
{
    private readonly List<TaskEntry> _entries = [];
    [Export] private Node _panel = null!;
    [Export] private VBoxContainer _list = null!;
    [Export] private PackedScene _entryRowScene = null!;
    private int _entryIdCounter = 0;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetChildren();
        foreach (Node child in _panel.FindChildren("*", "Button", true))
        {
            if (child is PanelElement button)
            {
                button.Pressed += () => AddEntry(button);
            }
        }
    }

    private void AddEntry(PanelElement button)
    {
        ArmAutonomyTask task = new()
        {
            type = button._type,
            item = button._item,
            skip_on_failure = button._skip_on_failure,
        };


        bool task_exists = _entries.Any(e => e.task.Equals(task));
        if (task_exists)
            return;

        TaskEntry entry = new()
        {
            task = task,
            entry_id = _entryIdCounter++
        };

        var row = _entryRowScene.Instantiate<EntryRow>();
        row.SetTask(entry);
        row.RemovalRequested += entryId => RemoveEntry(entryId, row);

        _list.AddChild(row);
        _entries.Add(entry);
    }

    private void RemoveEntry(int entryId, EntryRow row)
    {
        int index = _entries.FindIndex(e => e.entry_id == entryId);
        if (index >= 0)
            _entries.RemoveAt(index);

        row.QueueFree();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
