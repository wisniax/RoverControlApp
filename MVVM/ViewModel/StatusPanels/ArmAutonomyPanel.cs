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
using static RoverControlApp.Core.MqttClasses;

public struct TaskEntry
{
    public RoboticArmTask task;
    public int entry_id;
    public TaskCheckStatus possibility;
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
    private int _entryIdCounter = 0;
    [Export] private Node _panel = null!;
    [Export] private VBoxContainer _list = null!;
    [Export] private PackedScene _entryRowScene = null!;

    [Export] private Label _lastCheckResultAgeLabel = null!;

    private ulong? _lastCheckResultAtMs;
    private ulong _lastDisplayedAgeSeconds = ulong.MaxValue; // Use to avoid rendering each frame
    private bool _lastCheckPanelFound = false;

    [Export] private Button _startButton = null!;

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
        _startButton.Pressed += StartMission;
    }

    private void AddEntry(PanelElement button)
    {
        RoboticArmTask task = new()
        {
            task_type = button._type,
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

        OnMissionChange();
    }

    private void RemoveEntry(int entryId)
    {
        int index = _entries.FindIndex(e => e.entry_id == entryId);
        if (index >= 0)
            _entries.RemoveAt(index);

        if (_rowsByEntryId.Remove(entryId, out EntryRow? row))
            row.QueueFree();

        OnMissionChange();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (_lastCheckResultAtMs is not ulong receivedAt)
            return;

        ulong checkAgeSeconds = (Godot.Time.GetTicksMsec() - receivedAt) / 1000;

        if (checkAgeSeconds != _lastDisplayedAgeSeconds)
        {
            _lastDisplayedAgeSeconds = checkAgeSeconds;
            _lastCheckResultAgeLabel.Text = $"Last check: {checkAgeSeconds}s ago; Panel: " + (_lastCheckPanelFound ? "found" : "NOT found");
        }
    }
    public override void _EnterTree()
    {
        MqttNode.Singleton.MessageReceivedAsync += OnArmAutonomyCheckResult;
    }

    public override void _ExitTree()
    {
        MqttNode.Singleton.MessageReceivedAsync -= OnArmAutonomyCheckResult;
    }

    public void UpdateCheckResult(ArmAutonomyCheckResult result)
    {
        _lastCheckPanelFound = result.panel_found;
        _lastCheckResultAtMs = Godot.Time.GetTicksMsec();
        _lastDisplayedAgeSeconds = 0;
        _lastCheckResultAgeLabel.Text = "Last check: 0s ago; Panel: " + (_lastCheckPanelFound ? "found" : "NOT found");

        if (result.mission_id != mission_id)
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Warning, "Mission ID mismatch");
            // return;
        }
        if (result.possibility.Length != _entries.Count)
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Error, "Mission task count mismatch");
            return;
        }
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Info, "Yep0");
        for (int i = 0; i < result.possibility.Count(); i++)
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Info, "Yep");
            var entry = _entries[i];
            entry.possibility = result.possibility[i] ? TaskCheckStatus.Possible : TaskCheckStatus.Impossible;
            _entries[i] = entry;
            if (_rowsByEntryId.TryGetValue(entry.entry_id, out EntryRow? row))
                row.SetTask(entry);
        }
    }

    public Task OnArmAutonomyCheckResult(string subTopic, MqttApplicationMessage? msg)
    {
        EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Info, "Got the message");
        if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicArmAutonomyCheckResult) || subTopic != LocalSettings.Singleton.Mqtt.TopicArmAutonomyCheckResult)
            return Task.CompletedTask;
        if (msg is null || msg.PayloadSegment.Count == 0)
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Error, "Empty payload");
            return Task.CompletedTask;
        }

        try
        {
            var result = JsonSerializer.Deserialize<ArmAutonomyCheckResult>(msg.ConvertPayloadToString());
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

    private void OnMissionChange()
    {
        mission_id++;
        EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Verbose, "Mission ID is now " + mission_id);
        _lastCheckResultAtMs = null;
        _lastCheckResultAgeLabel.Text = $"Last check: N/A ago";
        RoboticArmAutonomy msg = new()
        {
            action = RoboticArmAutonomyActionType.Check,
            tasks = _entries.Select(e => e.task).ToArray(),
            mission_id = mission_id,
        };

        // Consider using async?
        MqttNode.Singleton.EnqueueMessage(LocalSettings.Singleton.Mqtt.TopicArmAutonomy,
            JsonSerializer.Serialize(msg));
    }

    private void StartMission()
    {
        RoboticArmAutonomy msg = new()
        {
            action = RoboticArmAutonomyActionType.Run,
            tasks = _entries.Select(e => e.task).ToArray(),
            mission_id = mission_id,
        };

        MqttNode.Singleton.EnqueueMessage(LocalSettings.Singleton.Mqtt.TopicArmAutonomy,
            JsonSerializer.Serialize(msg));
    }
}
