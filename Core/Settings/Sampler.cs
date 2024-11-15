using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;
using RoverControlApp.Core.RoverControllerPresets;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(SamplerConverter))]
public partial class Sampler : SettingBase, ICloneable
{

	public Sampler()
	{
		_containerDegreesClosed0 = 0f;
		_containerDegreesOpened0 = 180f;
		_containerDegreesClosed1 = 0f;
		_containerDegreesOpened1 = 180f;
		_containerDegreesClosed2 = 0f;
		_containerDegreesOpened2 = 180f;
	}

	public Sampler(int roverDriveController, float deadzone, bool vibrateOnModeChange, float containerDegreesClosed0, float containerDegreesOpened0, float containerDegreesClosed1, float containerDegreesOpened1, float containerDegreesClosed2, float containerDegreesOpened2)
	{
		_roverDriveController = roverDriveController;
		_deadzone = deadzone;
		_vibrateOnModeChange = vibrateOnModeChange;
		ContainerDegreesClosed0 = containerDegreesClosed0;
		ContainerDegreesOpened0 = containerDegreesOpened0;
		ContainerDegreesClosed1 = containerDegreesClosed1;
		ContainerDegreesOpened1 = containerDegreesOpened1;
		ContainerDegreesClosed2 = containerDegreesClosed2;
		ContainerDegreesOpened2 = containerDegreesOpened2;
	}

	public object Clone()
	{
		return new Sampler()
		{
			RoverDriveController = _roverDriveController,
			Deadzone = _deadzone,
			VibrateOnModeChange = _vibrateOnModeChange,
			ContainerDegreesClosed0 = _containerDegreesClosed0,
			ContainerDegreesOpened0 = _containerDegreesOpened0,
			ContainerDegreesClosed1 = _containerDegreesClosed1,
			ContainerDegreesOpened1 = _containerDegreesOpened1,
			ContainerDegreesClosed2 = _containerDegreesClosed2,
			ContainerDegreesOpened2 = _containerDegreesOpened2
		};
	}

	[SettingsManagerVisible(
		cellMode: TreeItem.TreeCellMode.Range,
		formatData: "0;3;1;f;i",
		customTooltip: "0 - GoodOldGamesLikeController\n" +
		               "1 - EricSOnController\n" +
		               "2 - ForzaLikeController\n" +
		               "3 - DirectDriveController (Default)"
	)]
	public int RoverDriveController
	{
		get => _roverDriveController;
		set => EmitSignal_SettingChanged(ref _roverDriveController, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;1;0.01;f;f")]
	public float Deadzone
	{
		get => _deadzone;
		set => EmitSignal_SettingChanged(ref _deadzone, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Check)]
	public bool VibrateOnModeChange
	{
		get => _vibrateOnModeChange;
		set => EmitSignal_SettingChanged(ref _vibrateOnModeChange, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float ContainerDegreesClosed0
	{
		get => _containerDegreesClosed0;
		set => EmitSignal_SettingChanged(ref _containerDegreesClosed0, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float ContainerDegreesOpened0
	{
		get => _containerDegreesOpened0;
		set => EmitSignal_SettingChanged(ref _containerDegreesOpened0, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float ContainerDegreesClosed1
	{
		get => _containerDegreesClosed1;
		set => EmitSignal_SettingChanged(ref _containerDegreesClosed1, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float ContainerDegreesOpened1
	{
		get => _containerDegreesOpened1;
		set => EmitSignal_SettingChanged(ref _containerDegreesOpened1, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float ContainerDegreesClosed2
	{
		get => _containerDegreesClosed2;
		set => EmitSignal_SettingChanged(ref _containerDegreesClosed2, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Range, formatData: "0;180;0.1;f;f")]
	public float ContainerDegreesOpened2
	{
		get => _containerDegreesOpened2;
		set => EmitSignal_SettingChanged(ref _containerDegreesOpened2, value);
	}

	int _roverDriveController;
	float _deadzone;
	bool _vibrateOnModeChange;
	float _containerDegreesClosed0;
	float _containerDegreesOpened0;
	float _containerDegreesClosed1;
	float _containerDegreesOpened1;
	float _containerDegreesClosed2;
	float _containerDegreesOpened2;
}