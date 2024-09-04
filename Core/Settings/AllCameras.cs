using Godot;
using RoverControlApp.Core.JSONConverters;
using System;
using System.Text.Json.Serialization;

namespace RoverControlApp.Core.Settings;

[JsonConverter(typeof(AllCamerasConverter))]
public partial class AllCameras : SettingBase, ICloneable
{

	public AllCameras()
	{
		_camera0 = new(0);
		_camera1 = new(1);
		_camera2 = new(2);
		_camera3 = new(3);
		_camera4 = new(4);
		_camera5 = new(5);
	}

	public AllCameras(Camera camera0, Camera camera1, Camera camera2, Camera camera3, Camera camera4, Camera camera5)
	{
		_camera0 = camera0;
		_camera1 = camera1;
		_camera2 = camera2;
		_camera3 = camera3;
		_camera4 = camera4;
		_camera5 = camera5;
	}

	public object Clone()
	{
		return new AllCameras()
		{
			Camera0 = _camera0,
			Camera1 = _camera1,
			Camera2 = _camera2,
			Camera3 = _camera3,
			Camera4 = _camera4,
			Camera5 = _camera5
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public Camera Camera0
	{
		get => _camera0;
		set => EmitSignal_SectionChanged(ref _camera0, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public Camera Camera1
	{
		get => _camera1;
		set => EmitSignal_SectionChanged(ref _camera1, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public Camera Camera2
	{
		get => _camera2;
		set => EmitSignal_SectionChanged(ref _camera2, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public Camera Camera3
	{
		get => _camera3;
		set => EmitSignal_SectionChanged(ref _camera3, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public Camera Camera4
	{
		get => _camera4;
		set => EmitSignal_SectionChanged(ref _camera4, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public Camera Camera5
	{
		get => _camera5;
		set => EmitSignal_SectionChanged(ref _camera5, value);
	}

	Camera _camera0;
	Camera _camera1;
	Camera _camera2;
	Camera _camera3;
	Camera _camera4;
	Camera _camera5;
}


