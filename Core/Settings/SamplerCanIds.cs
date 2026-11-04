using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(SamplerCanIdsConverter))]
public partial class SamplerCanIds : SettingBase, ICloneable
{
	// Defaults from wisniax/raptor_ws, RosCanConstants.cpp (af8ae757fb2e852035066041e54d7361ffc77f6e).
	public SamplerCanIds()
	{
		_platform = "0x80";
		_drillMovement = "0x81";
		_drill = "0x82";
		_containerA = "0x83";
		_containerB = "0x84";
		_vacuumSuction = "0x85";
		_vacuumA = "0x86";
		_vacuumB = "0x87";
	}

	public SamplerCanIds(string platform, string drillMovement, string drill, string containerA, string containerB, string vacuumSuction, string vacuumA, string vacuumB)
	{
		_platform = platform;
		_drillMovement = drillMovement;
		_drill = drill;
		_containerA = containerA;
		_containerB = containerB;
		_vacuumSuction = vacuumSuction;
		_vacuumA = vacuumA;
		_vacuumB = vacuumB;
	}

	public object Clone()
	{
		return new SamplerCanIds(_platform, _drillMovement, _drill, _containerA, _containerB, _vacuumSuction, _vacuumA, _vacuumB);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string Platform
	{
		get => _platform;
		set => EmitSignal_SettingChanged(ref _platform, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string DrillMovement
	{
		get => _drillMovement;
		set => EmitSignal_SettingChanged(ref _drillMovement, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string Drill
	{
		get => _drill;
		set => EmitSignal_SettingChanged(ref _drill, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string ContainerA
	{
		get => _containerA;
		set => EmitSignal_SettingChanged(ref _containerA, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string ContainerB
	{
		get => _containerB;
		set => EmitSignal_SettingChanged(ref _containerB, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string VacuumSuction
	{
		get => _vacuumSuction;
		set => EmitSignal_SettingChanged(ref _vacuumSuction, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string VacuumA
	{
		get => _vacuumA;
		set => EmitSignal_SettingChanged(ref _vacuumA, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.String, formatData: CanIds.IdPattern)]
	public string VacuumB
	{
		get => _vacuumB;
		set => EmitSignal_SettingChanged(ref _vacuumB, value);
	}

	private string _platform;
	private string _drillMovement;
	private string _drill;
	private string _containerA;
	private string _containerB;
	private string _vacuumSuction;
	private string _vacuumA;
	private string _vacuumB;
}
