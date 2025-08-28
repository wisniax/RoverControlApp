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
		Position0 = 0f;
		Position1 = 1f;
		Position2 = -1f;
		PreciseStep = 1f;
	}

	public SamplerContainer(string customName, float closedDegrees, float openDegrees)
	{
		CustomName = customName;
		ClosedDegrees = closedDegrees;
		OpenDegrees = openDegrees;
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string CustomName { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "-1;1;0.01;f;f")]
	public float Position0 { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "-1;1;0.01;f;f")]
	public float Position1 { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "-1;1;0.01;f;f")]
	public float Position2 { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0.01;1;0.01;f;f")]
	public float PreciseStep { get; init; }
}
