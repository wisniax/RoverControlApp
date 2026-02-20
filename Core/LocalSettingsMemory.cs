﻿using Godot;
using System;
using System.Runtime.CompilerServices;

namespace RoverControlApp.Core;

/// <summary>
/// Master class for settings storage. Can be fetched by LocalSettings.Singleton<br/>
/// </summary>
public partial class LocalSettingsMemory : Node
{

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public static LocalSettingsMemory Singleton { get; private set; }

#pragma warning restore CS8618

	/// <summary>
	/// Signal stating that one of categories was overwritten (reference changed)
	/// </summary>
	[Signal]
	public delegate void CategoryChangedEventHandler(StringName category);

	/// <summary>
	/// Signal SubcategoryChanged propagated from one of categories. Prefer this over SettingBaseMemory.SubcategoryChanged
	/// </summary>
	[Signal]
	public delegate void PropagatedSubcategoryChangedEventHandler(StringName category, StringName subcategory, Variant oldValue, Variant newValue);

	/// <summary>
	/// Signal PropertyChanged propagated from one of categories. Prefer this over SettingBaseMemory.PropertyChanged
	/// </summary>
	[Signal]
	public delegate void PropagatedPropertyChangedEventHandler(StringName category, StringName property, Variant oldValue, Variant newValue);

	public LocalSettingsMemory()
	{
		_calibrateAxis = new();

		ForceDefaultSettings();
	}

	public override void _Ready()
	{
		//first ever call to _Ready will be on singletone instance.
		Singleton ??= this;
	}

	/// <summary>
	/// Reset settings to default state
	/// </summary>
	public void ForceDefaultSettings()
	{
		EventLogger.LogMessage(nameof(LocalSettingsMemory), EventLogger.LogLevel.Info, "Loading default settings memory data");

		CalibrateAxis = new();
	}

	private void EmitSignalCategoryChanged(string sectionName)
	{
		EmitSignal(SignalName.CategoryChanged, sectionName);
		EventLogger.LogMessageDebug(nameof(LocalSettingsMemory), EventLogger.LogLevel.Verbose, $"Section \"{sectionName}\" was overwritten");
	}

	private void PropagateSignal(StringName signal, StringName category, params Variant[] args)
	{
		Variant[] combined = new Variant[args.Length + 1];

		combined[0] = category;
		args.CopyTo(combined, 1);

		EventLogger.LogMessageDebug(nameof(LocalSettingsMemory), EventLogger.LogLevel.Verbose, $"Field \"{args[0].AsStringName()}\" from \"{combined[0]}\" was changed. Signal propagated to LocalSettingsMemory.");

		EmitSignal(signal, combined);
	}

	/// <summary>
	/// Fany pants lambda creator for propagator
	/// </summary>
	/// <param name="signal">SignalName.PropagatedPropertyChanged or SignalName.PropagatedSubcategoryChanged</param>
	/// <param name="category">name of category (when used in Property setter should be empty)</param>
	/// <returns></returns>
	private Action<StringName, Variant, Variant> CreatePropagator(StringName signal, [CallerMemberName] string category = "")
	{
		return (field, oldVal, newVal) => PropagateSignal(signal, category, field, oldVal, newVal);
	}


	public SettingsMemory.CalibrateAxis CalibrateAxis
	{
		get => _calibrateAxis;
		set
		{
			_calibrateAxis = value;

			_calibrateAxis.Connect(
				SettingsMemory.CalibrateAxis.SignalName.SubcategoryChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedSubcategoryChanged))
			);
			_calibrateAxis.Connect(
				SettingsMemory.CalibrateAxis.SignalName.PropertyChanged,
				Callable.From(CreatePropagator(SignalName.PropagatedPropertyChanged))
			);

			EmitSignalCategoryChanged(nameof(CalibrateAxis));
		}
	}

	SettingsMemory.CalibrateAxis _calibrateAxis;

}

