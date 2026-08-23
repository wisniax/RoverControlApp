using Godot;
using Onvif.Core.Client.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using RoverControlApp.Core;
using MQTTnet.Client;
using RoverControlApp.MVVM.Model;
using MQTTnet;
using System.Text.Json;
using System.IO;
using RoverControlApp.Core.Settings;

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
    public TaskCheckStatus possibility;
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

public enum TaskCheckStatus
{
    Unchecked,
    Impossible,
    Possible,
}

public partial class ArmAutonomyPanel : Control
{
    private readonly List<TaskEntry> _entries = [];
    private readonly Dictionary<int, EntryRow> _rowsByEntryId = [];
    private int mission_id = 0;
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
            entry_id = _entryIdCounter++,
            possibility = TaskCheckStatus.Unchecked,
        };

        var row = _entryRowScene.Instantiate<EntryRow>();
        row.SetTask(entry);
        row.RemovalRequested += entryId => RemoveEntry(entryId);

        _list.AddChild(row);
        _rowsByEntryId.Add(entry.entry_id, row);
        _entries.Add(entry);
        mission_id++;
        EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Verbose, "Mission ID is now " + mission_id);
    }

    private void RemoveEntry(int entryId)
    {
        int index = _entries.FindIndex(e => e.entry_id == entryId);
        if (index >= 0)
            _entries.RemoveAt(index);

        if (_rowsByEntryId.Remove(entryId, out EntryRow? row))
            row.QueueFree();

        mission_id++;
        EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Verbose, "Mission ID is now " + mission_id);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    public override void _EnterTree()
    {
        MqttNode.Singleton.MessageReceivedAsync += OnArmAutonomyCheckResult;
    }

    public override void _ExitTree()
    {
        MqttNode.Singleton.MessageReceivedAsync -= OnArmAutonomyCheckResult;
    }

    public void UpdateCheckResult(MqttClasses.ArmAutonomyCheckResult result)
    {
        if (result.mission_id != mission_id)
        {
            // Maybe log warning
            return;
        }
        if (result.possibility.Count() != _entries.Count())
        {
            // definitely log warning or error
            return;
        }
        for (int i = 0; i < result.possibility.Count(); i++)
        {
            var entry = _entries[i];
            entry.possibility = result.possibility[i] ? TaskCheckStatus.Possible : TaskCheckStatus.Impossible;
            _entries[i] = entry;
            if (_rowsByEntryId.TryGetValue(entry.entry_id, out EntryRow? row))
                row.SetTask(entry);
        }
    }

    public Task OnArmAutonomyCheckResult(string subTopic, MqttApplicationMessage? msg)
    {
        if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicArmAutonomyCheckResult) || subTopic != LocalSettings.Singleton.Mqtt.TopicArmAutonomyCheckResult)
            return Task.CompletedTask;
        if (msg is null || msg.PayloadSegment.Count == 0)
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Error, "Empty payload");
            return Task.CompletedTask;
        }

        try
        {
            var result = JsonSerializer.Deserialize<MqttClasses.ArmAutonomyCheckResult>(msg.ConvertPayloadToString());
            if (result is null)
                throw new InvalidDataException("Invalid ArmAutonomyCheckResult payload.");
            Callable.From(() => UpdateCheckResult(result)).CallDeferred();
        }
        catch (Exception e)
        {
            EventLogger.LogMessage("ArmAutonomyCheckResult", EventLogger.LogLevel.Error, $"Something is wrong with json/deserialization: {e.Message}");
        }
        return Task.CompletedTask;
    }
}
