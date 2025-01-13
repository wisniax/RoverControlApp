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
		_container0 = new();
		_container1 = new();
		_container2 = new();
	}

	public Sampler(SamplerContainer container0, SamplerContainer container1, SamplerContainer container2)
	{
		_container0 = container0;
		_container1 = container1;
		_container2 = container2;
	}

	public object Clone()
	{
		return new Sampler()
		{
			Container0 = _container0,
			Container1 = _container1,
			Container2 = _container2
		};
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public SamplerContainer Container0
	{
		get => _container0;
		set => EmitSignal_SectionChanged(ref _container0, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public SamplerContainer Container1
	{
		get => _container1;
		set => EmitSignal_SectionChanged(ref _container1, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public SamplerContainer Container2
	{
		get => _container2;
		set => EmitSignal_SectionChanged(ref _container2, value);
	}

	SamplerContainer _container0;
	SamplerContainer _container1;
	SamplerContainer _container2;
}