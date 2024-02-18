using Godot;

namespace RoverControlApp.Core;

public class JoystickSettings
{
	public static readonly JoystickSettings DEFAULT = new()
	{
		NewFancyRoverController = false,
		Deadzone = 0.15f,
		VibrateOnModeChange = true
	};

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool NewFancyRoverController { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0f;1;0.01;f;f")]
	public float Deadzone { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool VibrateOnModeChange { get; set; }
}



