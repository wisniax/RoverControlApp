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
		Position1 = 90f;
		Position2 = 180f;
	}

	public SamplerContainer(string customName, float position0, float position1, float position2)
	{
		CustomName = customName;
		Position0 = position0;
		Position1 = position1;
		Position2 = position2;
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String)]
	public string CustomName { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float Position0 { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float Position1 { get; init; }

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float Position2 { get; init; }
}
