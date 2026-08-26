using Godot;
using System;
using System.Xml.Schema;

public partial class EntryRow : Control
{
    [Export] public Label _taskLabel = null!;
    [Export] public Button _removeButton = null!;
    [Export] public Panel _statusIcon = null!;

    private int _entryId = -1;

    public event Action<int>? RemovalRequested;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _removeButton.Visible = _entryId != -1;
        _removeButton.Pressed += () =>
        {
            if (_entryId != -1)
                RemovalRequested?.Invoke(_entryId);
        };
    }

    public void SetTask(TaskEntry entry)
    {
        _entryId = entry.entry_id;
        _removeButton.Visible = _entryId != -1;
        _taskLabel.Text = entry.task.ToString();
        var iconColor = entry.possibility switch
        {
            TaskCheckStatus.Unchecked => Colors.Yellow,
            TaskCheckStatus.Impossible => Colors.Red,
            TaskCheckStatus.Possible => Colors.Green,
            _ => Colors.Magenta,
        };
        var style = _statusIcon.GetThemeStylebox("panel").Duplicate() as StyleBoxFlat;
        if (style is not null)
        {
            style.BgColor = iconColor;
            _statusIcon.AddThemeStyleboxOverride("panel", style);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
