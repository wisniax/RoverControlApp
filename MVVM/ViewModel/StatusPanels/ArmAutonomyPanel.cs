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

    [Export] private VBoxContainer _feedbackList = null!;
    private readonly List<EntryRow> _feedbackRows = [];

    [Export] private Button _checkButton = null!;
    [Export] private Button _stopButton = null!;
    [Export] private Label _feedbackStatusLabel = null!;
    [Export] private Button _clearButton = null!;

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
        _checkButton.Pressed += SendCheck;
        _stopButton.Pressed += SendStop;
        _clearButton.Pressed += ClearMission;
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

    private void ClearMission()
    {
        _entries.Clear();

        foreach (var row in _rowsByEntryId.Values)
        {
            row.QueueFree();
        }
        _rowsByEntryId.Clear();

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
        MqttNode.Singleton.MessageReceivedAsync += OnRoboticArmCheckResult;
        MqttNode.Singleton.MessageReceivedAsync += OnRoboticArmMissionFeedback;
    }

    public override void _ExitTree()
    {
        MqttNode.Singleton.MessageReceivedAsync -= OnRoboticArmCheckResult;
        MqttNode.Singleton.MessageReceivedAsync -= OnRoboticArmMissionFeedback;
    }

    public void UpdateCheckResult(RoboticArmCheckResult result)
    {
        _lastCheckPanelFound = result.panel_found;
        _lastCheckResultAtMs = Godot.Time.GetTicksMsec();
        _lastDisplayedAgeSeconds = 0;
        _lastCheckResultAgeLabel.Text = "Last check: 0s ago; Panel: " + (_lastCheckPanelFound ? "found" : "NOT found");

        if (result.mission_id != mission_id.ToString())
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Warning, "Mission ID mismatch");
            // return;
        }
        if (result.possibility.Length != _entries.Count)
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Error, "Mission task count mismatch");
            return;
        }
        for (int i = 0; i < result.possibility.Length; i++)
        {
            var entry = _entries[i];
            entry.possibility = result.possibility[i] ? TaskCheckStatus.Possible : TaskCheckStatus.Impossible;
            _entries[i] = entry;
            if (_rowsByEntryId.TryGetValue(entry.entry_id, out EntryRow? row))
                row.SetTask(entry);
        }
    }

    public Task OnRoboticArmCheckResult(string subTopic, MqttApplicationMessage? msg)
    {
        if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicRoboticArmCheckResult) || subTopic != LocalSettings.Singleton.Mqtt.TopicRoboticArmCheckResult)
            return Task.CompletedTask;
        if (msg is null || msg.PayloadSegment.Count == 0)
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Error, "Empty payload");
            return Task.CompletedTask;
        }

        try
        {
            var result = JsonSerializer.Deserialize<RoboticArmCheckResult>(msg.ConvertPayloadToString());
            if (result is null)
                throw new InvalidDataException("Invalid RoboticArmCheckResult payload.");
            Callable.From(() => UpdateCheckResult(result)).CallDeferred();
        }
        catch (Exception e)
        {
            EventLogger.LogMessage("RoboticArmCheckResult", EventLogger.LogLevel.Error, $"Something is wrong with json/deserialization: {e.Message}");
        }
        return Task.CompletedTask;
    }

    private void OnMissionChange()
    {
        mission_id++;
        EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Verbose, "Mission ID is now " + mission_id);
        _lastCheckResultAtMs = null;
        _lastCheckResultAgeLabel.Text = $"Last check: N/A ago";
        SendCheck();
    }

    private void SendCheck()
    {
        RoboticArmAutonomy msg = new()
        {
            action = RoboticArmAutonomyActionType.Check,
            tasks = _entries.Select(e => e.task).ToArray(),
            mission_id = mission_id.ToString(),
        };

        // Consider using async?
        MqttNode.Singleton.EnqueueMessage(LocalSettings.Singleton.Mqtt.TopicRoboticArmAutonomy,
            JsonSerializer.Serialize(msg));
    }

    private void SendStop()
    {
        RoboticArmAutonomy msg = new()
        {
            action = RoboticArmAutonomyActionType.Stop,
        };

        // Consider using async?
        MqttNode.Singleton.EnqueueMessage(LocalSettings.Singleton.Mqtt.TopicRoboticArmAutonomy,
            JsonSerializer.Serialize(msg));
    }

    private void StartMission()
    {
        RoboticArmAutonomy msg = new()
        {
            action = RoboticArmAutonomyActionType.Run,
            tasks = _entries.Select(e => e.task).ToArray(),
            mission_id = mission_id.ToString(),
        };

        MqttNode.Singleton.EnqueueMessage(LocalSettings.Singleton.Mqtt.TopicRoboticArmAutonomy,
            JsonSerializer.Serialize(msg));
    }

    private void SetRowListLength(VBoxContainer list_element, List<EntryRow> row_list, int length)
    {
        if (length < 0)
            length = 0;

        while (row_list.Count > length)
        {
            // if (row_list.RemoveAt(row_list.Count - 1, out EntryRow? row))
            var row = row_list[^1];
            row_list.RemoveAt(row_list.Count - 1);
            row.QueueFree();
        }
        while (row_list.Count < length)
        {
            var row = _entryRowScene.Instantiate<EntryRow>();
            row.RemovalRequested += entryId => RemoveEntry(entryId);

            list_element.AddChild(row);
            row_list.Add(row);
        }
    }

    private void SetRowListTasks(VBoxContainer list_element, List<EntryRow> row_list, RoboticArmTask[] tasks, bool[] completed)
    {
        if (completed.Length != tasks.Length)
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Warning, "Invalid feedback - completed count does not match tasks count");
            return;
        }
        SetRowListLength(list_element, row_list, tasks.Length);
        for (int i = 0; i < tasks.Length; i++)
        {
            TaskCheckStatus status = completed.Length > i
                ? (completed[i] ? TaskCheckStatus.Possible : TaskCheckStatus.Impossible)
                : TaskCheckStatus.Unchecked;
            TaskEntry entry = new()
            {
                entry_id = -1,
                possibility = status,
                task = tasks[i]
            };
            row_list[i].SetTask(entry);
        }
    }

    public Task OnRoboticArmMissionFeedback(string subTopic, MqttApplicationMessage? msg)
    {
        if (string.IsNullOrEmpty(LocalSettings.Singleton.Mqtt.TopicRoboticArmMissionFeedback) || subTopic != LocalSettings.Singleton.Mqtt.TopicRoboticArmMissionFeedback)
            return Task.CompletedTask;
        if (msg is null || msg.PayloadSegment.Count == 0)
        {
            EventLogger.LogMessage("ArmAutonomyPanel", EventLogger.LogLevel.Error, "Empty payload (feedback)");
            return Task.CompletedTask;
        }

        try
        {
            var feedback = JsonSerializer.Deserialize<RoboticArmMissionFeedback>(msg.ConvertPayloadToString());
            if (feedback is null)
                throw new InvalidDataException("Invalid RoboticArmMissionFeedback payload.");
            Callable.From(() => HandleFeedback(feedback)).CallDeferred();
        }
        catch (Exception e)
        {
            EventLogger.LogMessage("RoboticArmMissionFeedback", EventLogger.LogLevel.Error, $"Something is wrong with json/deserialization: {e.Message}");
        }
        return Task.CompletedTask;
    }

    private void HandleFeedback(RoboticArmMissionFeedback feedback)
    {
        SetRowListTasks(_feedbackList, _feedbackRows, feedback.tasks, feedback.completed_tasks);
        EventLogger.LogMessage("RoboticArmMissionFeedback", EventLogger.LogLevel.Info, "Got feedback, status " + feedback.status);
        _feedbackStatusLabel.Text = $"Status: {feedback.status}";
    }
}
