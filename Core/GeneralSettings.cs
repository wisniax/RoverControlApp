using Godot;

namespace RoverControlApp.Core;

public class GeneralSettings
{
	public static readonly GeneralSettings DEFAULT = new()
	{
		VerboseDebug = false,
		MissionControlPosition = "20;30",
		MissionControlSize = "480;360",
		BackCaptureLength = 15000
	};

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool VerboseDebug { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"-?[0-9]+;-?[0-9]+")]
	public string MissionControlPosition { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: @"-?[0-9]+;-?[0-9]+")]
	public string MissionControlSize { get; set; }
	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;60000;100;f;l", customTooltip: "How long is history [ms]")]
	public long BackCaptureLength { get; set; }
}

