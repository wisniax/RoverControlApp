using Godot;
using System;

public partial class EntryRow : Control
{
    [Export] public required Label _taskLabel;
    [Export] public required Button _removeButton;

    private int _entryId;

    public event Action<int>? RemovalRequested;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _removeButton.Pressed += () => RemovalRequested?.Invoke(_entryId);
    }

    public void SetTask(TaskEntry entry)
    {
        _entryId = entry.entry_id;
        _taskLabel.Text = entry.task.ToString();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
