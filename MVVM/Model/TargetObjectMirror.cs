using RoverControlApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RoverControlApp.MVVM.Model;

public class TargetObjectMirror(object parent, object original, string memberName, SettingsManagerVisibleAttribute attribute)
{
	private readonly object _parent = parent;
	private readonly object _original = original;
	private readonly string _memberName = memberName;
	private readonly Dictionary<string, object> _changes = [];
	private readonly bool _immutableSection = attribute.ImmutableSection;

	public Type HoldingType { get; init; } = original.GetType();

	private object Original => _parent is TargetObjectMirror parentObjectMirror ? parentObjectMirror.GetOriginalValue(_memberName)! : _original;

	public IEnumerable<string> GetChangedProperties()
	{
		return _changes.Keys.AsEnumerable();
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
		if (_changes.Remove(propertyName) && GetOriginalValue(propertyName)!.Equals(newValue))
			return true;

		_changes.Add(propertyName, newValue);
		return true;
	}

	public object? GetCloneValue(string propertyName)
	{
		var propertyInfo = HoldingType.GetProperty(propertyName);

		if (propertyInfo is null)
			return null;

		_changes.TryGetValue(propertyName, out object? value);
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
			_changes.Clear();
			return;
		}

		_changes.Remove(propertyName);
	}

	public void Apply()
	{
		if (!GetChangedProperties().Any())
			return;

		if (_immutableSection)
			Apply_UpdateWholeSection();
		else
			Apply_ChangesOnly();

		_changes.Clear();
	}

	private void Apply_ChangesOnly()
	{
		foreach ((string propertyName, object newValue) in _changes)
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

			if (_changes.TryGetValue(propertyInfo.Name, out object? newValue))
				newObjectValues.Add(propertyInfo.Name, newValue);
			else
				newObjectValues.Add(propertyInfo.Name, propertyInfo.GetValue(Original)!);
		}

		//if u see this, KEEP PARAMETERS IN SAME ORDER AS OF MEMBER DECLARATION, INSIDE CTOR. IT'S NICE THAT WAY YA KNOW?
		object theNewObject = Activator.CreateInstance(HoldingType, [..newObjectValues.Values])!;

		if (_parent is TargetObjectMirror parentObjectMirror)
		{
			parentObjectMirror.SetCloneValue(_memberName, theNewObject);
		}
		else
		{
			var parentOriginalProperty = _parent.GetType().GetProperty(_memberName)!;
			parentOriginalProperty.SetValue(_parent, theNewObject);
		}
	}
}
