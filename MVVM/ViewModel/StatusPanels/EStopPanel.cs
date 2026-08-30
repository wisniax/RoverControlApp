using Godot;
using System;

public partial class EStopPanel : Control
{
	[Export] Panel _hiddenStatePanel = null!;
	[Export] Panel _shownStatePanel = null!;

	public void PanelPressed()
	{
		if (_hiddenStatePanel.Visible)
		{
			_hiddenStatePanel.Visible = false;
			_shownStatePanel.Visible = true;
			this.CustomMinimumSize = _shownStatePanel.Size;
		}
		else
		{
			_hiddenStatePanel.Visible = true;
			_shownStatePanel.Visible = false;
			this.CustomMinimumSize = _hiddenStatePanel.Size;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		PanelPressed();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
