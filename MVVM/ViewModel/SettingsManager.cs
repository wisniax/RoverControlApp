using Godot;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SettingsManager : Panel
{

	[Signal]
	public delegate void RequestedRestartEventHandler();

	private SettingsManagerTree? smTree;
	private RichTextLabel? statusBar;

	public override void _Ready()
	{
		smTree = GetNode<SettingsManagerTree>(SMTreeNodePath);
		smTree.StatusBar = statusBar = GetNode<RichTextLabel>(StatusBarNodePath);
	}
	public void OnForceDefaultSettingsPressed()
	{
		MainViewModel.Settings?.ForceDefaultSettings();
		smTree.Reconstruct();
		statusBar.Text = "[color=lightgreen]Default settings loaded![/color]";
	}
	public void OnLoadSettingsPressed()
	{
		MainViewModel.Settings?.LoadSettings();
		smTree.Reconstruct();
		statusBar.Text = "[color=lightgreen]Settings loaded![/color]";
	}

	public void OnSaveSettingsPressed()
	{
		MainViewModel.Settings?.SaveSettings();
		statusBar.Text = "[color=lightgreen]Settings saved![/color]";
		EmitSignal(SignalName.RequestedRestart, null);
	}

	public void Redraw(bool onTrue)
	{
		if(onTrue)
			smTree.Reconstruct();
	}


	[Export]
	public NodePath SMTreeNodePath { get; set; }
	[Export]
	public NodePath StatusBarNodePath { get; set; }

	/// <summary>
	/// Node must be ready, else prepare for ObjectNullException
	/// </summary>
	public object Target
	{
		get => smTree.Target;
		set => smTree.Target = value;
	}
}
