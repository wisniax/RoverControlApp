using Godot;
using RoverControlApp.Core;
using RoverControlApp.Core;
using RoverControlApp.MVVM.Model;
using System.Threading.Tasks;
using System;

namespace RoverControlApp.MVVM.ViewModel;

public partial class SettingsManager : Panel
{
	public event Func<Task>? OnSettingsApplied;
	

	[Export]
	private SettingsManagerTree smTree = null!;

	[Export]
	private RichTextLabel statusBar = null!;

	public override void _Ready()
	{
		smTree.Connect(SettingsManagerTree.SignalName.UpdateStatusBar, Callable.From<string>(OnUpdateStatusBar));
	}

	public void OnLoadSettingsPressed()
	{
		if (!LocalSettings.Singleton.LoadSettings())
		{
			statusBar.Text = "[color=orangered]Settings loading error! Check log for more information.[/color]";
			return;
		}
		smTree.Reconstruct();
		statusBar.Text = "[color=lightgreen]Settings loaded![/color]";
	}

	public void OnSaveSettingsPressed()
	{
		smTree.ApplySettings();
		if (!LocalSettings.Singleton.SaveSettings())
		{
			statusBar.Text = "[color=orangered]Settings saving error! Check log for more information.[/color]";
			return;
		}
		statusBar.Text = "[color=lightgreen]Settings saved![/color]";
	}

	public void OnForceDefaultSettingsPressed()
	{
		LocalSettings.Singleton.ForceDefaultSettings();
		smTree.Reconstruct();
		statusBar.Text = "[color=lightgreen]Default settings loaded![/color]";
	}

	public void OnRevertSettingsPressed()
	{
		smTree.RevertSettings();
		statusBar.Text = "[color=lightgreen]Settings reverted![/color]";
	}

	public void OnApplySettingsPressed()
	{
		smTree.ApplySettings();
		statusBar.Text = "[color=lightgreen]Settings applied![/color]";
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
