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
		_container3 = new();
		_container4 = new();
	}

	public Sampler(SamplerContainer container0, SamplerContainer container1, SamplerContainer container2, SamplerContainer container3, SamplerContainer container4)
	{
		_container0 = container0;
		_container1 = container1;
		_container2 = container2;
		_container3 = container3;
		_container4 = container4;
	}

	public object Clone()
	{
		return new Sampler()
		{
			Container0 = _container0,
			Container1 = _container1,
			Container2 = _container2,
			Container3 = _container3,
			Container4 = _container4,

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

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public SamplerContainer Container3
	{
		get => _container3;
		set => EmitSignal_SectionChanged(ref _container3, value);
	}

	[SettingsManagerVisible(cellMode: TreeItem.TreeCellMode.Custom, immutableSection: true)]
	public SamplerContainer Container4
	{
		get => _container4;
		set => EmitSignal_SectionChanged(ref _container4, value);
	}

	SamplerContainer _container0;
	SamplerContainer _container1;
	SamplerContainer _container2;
	SamplerContainer _container3;
	SamplerContainer _container4;
}
