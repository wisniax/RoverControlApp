using Godot;
using RoverControlApp.Core.JSONConverters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(SamplerContainerConverter))]
public partial class SamplerContainer : RefCounted
{

	public SamplerContainer()
	{
		CustomName = "-";
		ClosedDegrees = 0f;
		OpenDegrees = 180f;
	}

	public SamplerContainer(string customName, float closedDegrees, float openDegrees)
	{
		CustomName = customName;
		ClosedDegrees = closedDegrees;
		OpenDegrees = openDegrees;
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string CustomName { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float ClosedDegrees { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float OpenDegrees { get; init; }
}