using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RoverControlApp.MVVM.Model;

public class TargetObjectMirror
{
	public TargetObjectMirror(object parent, object original, string memberName, SettingsManagerVisibleAttribute attribute)
	{
		HoldingType = original.GetType();

		immutableSection = attribute.ImmutableSection;

		this.parent = parent;
		this.original = original;
		this.memberName = memberName;

		changes = new();
	}

	public IEnumerable<string> GetChangedProperties()
	{
		return changes.Keys.AsEnumerable();
	}

	public bool VadilateValue(string propertyName, object value)
	{
		var propertyInfo = HoldingType.GetProperty(propertyName);

		if (propertyInfo is null)
			return false;

		var settingsManagerAttribute = propertyInfo.GetCustomAttribute<SettingsManagerVisibleAttribute>()!;

		return settingsManagerAttribute.ValidateValue(value);
	}

	public bool SetCloneValue(string propertyName, object newValue)
	{
		var propertyInfo = HoldingType.GetProperty(propertyName);

		if (propertyInfo is null)
			return false;

		var settingsManagerAttribute = propertyInfo.GetCustomAttribute<SettingsManagerVisibleAttribute>()!;

		if (!settingsManagerAttribute.ValidateValue(newValue))
			return false;

		//if new == original then it is reverted, not modified anymore.
		if (changes.Remove(propertyName) && GetOriginalValue(propertyName)!.Equals(newValue))
			return true;

		changes.Add(propertyName, newValue);
		return true;
	}

	public object? GetCloneValue(string propertyName)
	{
		var propertyInfo = HoldingType.GetProperty(propertyName);

		if (propertyInfo is null)
			return null;

		changes.TryGetValue(propertyName, out object? value);
		if (value is not null)
			return value;
		else
			return GetOriginalValue(propertyName);
	}

	public object? GetOriginalValue(string propertyName)
	{
		var propertyInfo = HoldingType.GetProperty(propertyName);

		if (propertyInfo is null)
			return null;

		return propertyInfo.GetValue(Original);
	}

	public void Revert(string? propertyName = null)
	{
		if (string.IsNullOrEmpty(propertyName))
		{
			changes.Clear();
			return;
		}

		changes.Remove(propertyName);
	}

	public void Apply()
	{
		if (!GetChangedProperties().Any())
			return;

		if (immutableSection)
			Apply_UpdateWholeSection();
		else
			Apply_ChangesOnly();

		changes.Clear();
	}

	private void Apply_ChangesOnly()
	{
		foreach ((string propertyName, object newValue) in changes)
		{
			var propertyInfo = HoldingType.GetProperty(propertyName)!;
			propertyInfo.SetValue(Original, newValue);
		}
	}

	private void Apply_UpdateWholeSection()
	{
		Dictionary<string, object> newObjectValues = new();
		foreach(var propertyInfo in HoldingType.GetProperties())
		{
			//skip this, dont care
			if (propertyInfo.Name.Equals("NativeInstance"))
				continue;

			if (changes.TryGetValue(propertyInfo.Name, out object? newValue))
				newObjectValues.Add(propertyInfo.Name, newValue);
			else
				newObjectValues.Add(propertyInfo.Name, propertyInfo.GetValue(Original)!);
		}

		//if u see this, KEEP PARAMETERS IN SAME ORDER AS OF MEMBER DECLARATION, INSIDE CTOR. IT'S NICE THAT WAY YA KNOW?
		object theNewObject = Activator.CreateInstance(HoldingType, [..newObjectValues.Values])!;

		if (parent is TargetObjectMirror parentObjectMirror)
		{
			parentObjectMirror.SetCloneValue(memberName, theNewObject);
		}
		else
		{
			var parentOriginalProperty = parent.GetType().GetProperty(memberName)!;
			parentOriginalProperty.SetValue(parent, theNewObject);
		}
	}

	public Type HoldingType { get; init; }

	private object Original => parent is TargetObjectMirror parentObjectMirror ? parentObjectMirror.GetOriginalValue(memberName)! : original;

	private readonly object parent;
	private readonly object original;
	private readonly string memberName;

	private readonly Dictionary<string, object> changes;

	private readonly bool immutableSection;
}
