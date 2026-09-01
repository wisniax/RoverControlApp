using Godot;

/// <summary>
/// Base class for collapsible status panels that toggle between a collapsed
/// ("hidden") and expanded ("shown") state, keeping their minimum size in sync
/// so a parent <see cref="Container"/> stacks them without overlap.
/// </summary>
public abstract partial class StatusPanel : Control
{
	[Export] private Panel _hiddenStatePanel = null!;
	[Export] private Panel _shownStatePanel = null!;

	public override void _EnterTree()
	{
		SyncMinimumSize();
	}

	/// <summary>Wired to the collapsed/expanded toggle buttons in the scene.</summary>
	public void PanelPressed()
	{
		bool show = !_shownStatePanel.Visible;
		_hiddenStatePanel.Visible = !show;
		_shownStatePanel.Visible = show;
		SyncMinimumSize();
	}

	private void SyncMinimumSize()
	{
		CustomMinimumSize = _shownStatePanel.Visible
			? _shownStatePanel.Size
			: _hiddenStatePanel.Size;
	}
}
