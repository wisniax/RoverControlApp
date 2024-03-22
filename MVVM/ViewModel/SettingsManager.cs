using Godot;
using RoverControlApp.MVVM.Model;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SettingsManager : Panel
{

	[Signal]
	public delegate void RequestedRestartEventHandler();

	[Export]
	private SettingsManagerTree smTree;

	[Export]
	private RichTextLabel statusBar;

	public override void _Ready()
	{
		smTree.Connect(SettingsManagerTree.SignalName.UpdateStatusBar, Callable.From<string>(OnUpdateStatusBar));
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

	public void OnRevertSettingsExpPressed()
	{
		smTree.RevertSettings();
		statusBar.Text = "[color=lightgreen]Settings reverted! (Experimental)[/color]";
	}

	public void OnApplySettingsExpPressed()
	{
		smTree.ApplySettings();
		statusBar.Text = "[color=lightgreen]Settings applied! (Experimental)[/color]";
	}

	public void OnSaveSettingsExpPressed()
	{
		smTree.ApplySettings();
		if(!LocalSettings.Singleton.SaveSettings())
		{
			statusBar.Text = "[color=orangered]Settings saving error! Check log for more information. (Experimental)[/color]";
			return;
		}
		statusBar.Text = "[color=lightgreen]Settings saved! (Experimental)[/color]";
	}

	public void OnLoadSettingsExpPressed()
	{
		if (!LocalSettings.Singleton.LoadSettings())
		{
			statusBar.Text = "[color=orangered]Settings loading error! Check log for more information. (Experimental)[/color]";
			return;
		}
		smTree.Reconstruct();
		statusBar.Text = "[color=lightgreen]Settings loaded! (Experimental)[/color]";
	}

	private void OnUpdateStatusBar(string text)
	{
		statusBar.Text = text;
	}

	public void OnVisibilityChange(bool onTrue)
	{
		if(onTrue)
			smTree.Reconstruct();
	}

	/// <summary>
	/// Node must be ready, else prepare for ObjectNullException
	/// </summary>
	public object Target
	{
		get => smTree.Target;
		set => smTree.Target = value;
	}
}
